using System.Text.Json;
namespace MathPet.Core;

public static class StateSerialization
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    public static string Serialize(PlayerState state) => JsonSerializer.Serialize(state,Options);
    public static PlayerState DeserializeBackup(string text)
    {
        using var document = JsonDocument.Parse(text);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("Coins", out _)
            || (!root.TryGetProperty("Pets", out _) && !root.TryGetProperty("PetName", out _)))
            throw new InvalidDataException("文件不是完整的数学宠物岛存档。");
        return Deserialize(text);
    }
    public static PlayerState Deserialize(string text)
    {
        var state = JsonSerializer.Deserialize<PlayerState>(text,Options) ?? throw new InvalidDataException("存档为空。");
        using var document=JsonDocument.Parse(text);
        var root=document.RootElement;
        var version=root.TryGetProperty("Version",out var v)?v.GetInt32():1;
        if(version is not (1 or 2 or 3 or 4))throw new InvalidDataException("此存档版本暂不支持，请保留原文件。");
        if(version==1)
        {
            var pet=state.Pets[0];
            if(root.TryGetProperty("PetName",out var name))pet.Name=name.GetString()??"";
            if(root.TryGetProperty("Fullness",out var fullness))pet.Fullness=fullness.GetInt32();
            if(root.TryGetProperty("Mood",out var mood))pet.Mood=mood.GetInt32();
            var growth=root.TryGetProperty("Growth",out var g)?g.GetInt32():20;
            if(growth is <0 or >100)throw new InvalidDataException("旧成长数据不合法。");
            var mature=growth>=100&&state.Stars is not null&&state.Stars.Count>0&&(state.ReviewedMistakes>0||state.CompletedPractices>=2);
            var young=growth>=50&&state.CompletedPractices>0;
            pet.Experience=Math.Max(growth,PetProgress.LegacyRequiredExperience(mature?13:young?6:1));
            if(state.HasRibbon){state.OwnedWearables.Add("bow");if(state.WearingRibbon)pet.Headwear="bow";}
            if(state.UsingRoom)state.Room="spring";
            state.Version=2;
        }
        var requiredSpecies=new[]{"cat","fox","rabbit"};
        if(state.Pets is null||state.Pets.Any(p=>p is null)||state.Pets.Count<3||state.Pets.Count>6
            ||state.Pets.Select(p=>p.Species).Distinct().Count()!=state.Pets.Count
            ||requiredSpecies.Any(species=>!state.Pets.Any(p=>p.Species==species))
            ||state.Pets.Any(p=>!PetCatalog.Pets.Any(kind=>kind.Species==p.Species))
            ||version<4&&state.Pets.Count!=3
            ||!state.Pets.Any(p=>p.Species==state.ActiveSpecies)||state.OwnedWearables is null
            ||!RoomCatalog.Themes.Any(r=>r.Id==state.Room)
            ||version<4&&state.Room is not ("study" or "spring" or "stars"))throw new InvalidDataException("宠物存档不完整。");
        var allowedWearables = version < 3 ? GameEngine.Wardrobe.Concat(GameEngine.RetiredWearables).ToArray() : GameEngine.Wardrobe;
        foreach(var pet in state.Pets)
        {
            if(string.IsNullOrWhiteSpace(pet.Name)||pet.Name.Length>12||pet.Experience<0||pet.Experience>(version<4?1425:PetProgress.MaxExperience)
                ||pet.Fullness is <30 or >100||pet.Mood is <40 or >100)throw new InvalidDataException("宠物数据不合法。");
            foreach(var pair in new[]{("head",pet.Headwear),("body",pet.Outfit),("accessory",pet.Accessory)})
                if(pair.Item2!=""&&(!state.OwnedWearables.Contains(pair.Item2)||!allowedWearables.Any(w=>w.Id==pair.Item2&&w.Slot==pair.Item1)))throw new InvalidDataException("穿搭数据不合法。");
        }
        if(state.OwnedWearables.Any(id=>!allowedWearables.Any(w=>w.Id==id)))throw new InvalidDataException("衣饰数据不合法。");
        if (state.Grade is < 1 or > 6 || !Enum.IsDefined(state.Semester)
            || state.Coins < 0 || state.Food < 0 || state.Growth is < 0 or > 100
            || state.Fullness is < 30 or > 100 || state.Mood is < 40 or > 100
            || state.CompletedPractices < 0 || state.ReviewedMistakes < 0
            || state.Stars is null || state.Progress is null || state.Mistakes is null
            || state.ViewedTopics is null || state.DailyBonuses is null)
            throw new InvalidDataException("存档数据不完整或不合法。");
        if (state.Stars.Any(p=>p.Value is < 1 or > 3)
            || state.Progress.Any(p=>p.Value is null || p.Value.AttemptCount < 0 || p.Value.CorrectCount < 0 || p.Value.CorrectCount > p.Value.AttemptCount || p.Value.IndependentCorrectCount < 0 || p.Value.IndependentCorrectCount > p.Value.CorrectCount)
            || state.Mistakes.Any(m=>m is null || m.WrongCount < 0 || string.IsNullOrWhiteSpace(m.QuestionId)))
            throw new InvalidDataException("学习记录不合法。");
        if (string.IsNullOrWhiteSpace(state.PetName) || state.PetName.Length > 12) throw new InvalidDataException("宠物名字不合法。");
        if (version < 3)
        {
            // Refund each purchased retired item once, regardless of how many pets wore it.
            var refund = GameEngine.RetiredWearables.Where(w => state.OwnedWearables.Contains(w.Id)).Sum(w => w.Price);
            if ((long)state.Coins + refund > int.MaxValue) throw new InvalidDataException("积分数据超出范围。");
            state.Coins += refund;
            state.OwnedWearables.ExceptWith(GameEngine.RetiredWearables.Select(w => w.Id));
            foreach (var pet in state.Pets)
            {
                pet.Outfit = "";
                if (pet.Accessory == "backpack") pet.Accessory = "";
            }
            state.OwnedRooms = ["study"];
            if (state.HasRoom || state.Room != "study") state.OwnedRooms.UnionWith(["spring", "stars"]);
            state.OwnedRoomDecorations = [];
            state.PlacedRoomDecorations = [];
            state.Version = 3;
        }
        if (state.OwnedRooms is null || state.OwnedRoomDecorations is null || state.PlacedRoomDecorations is null
            || !state.OwnedRooms.Contains("study") || !state.OwnedRooms.Contains(state.Room)
            || state.OwnedRooms.Any(id => !RoomCatalog.Themes.Any(r => r.Id == id))
            || state.OwnedRoomDecorations.Any(id => !RoomCatalog.Decorations.Any(d => d.Id == id))
            || state.PlacedRoomDecorations.Any(id => !state.OwnedRoomDecorations.Contains(id)))
            throw new InvalidDataException("房间装饰数据不合法。");
        if(version<4)
        {
            foreach(var pet in state.Pets)
            {
                var oldLevel=Enumerable.Range(1,20).Last(l=>pet.Experience>=PetProgress.LegacyRequiredExperience(l));
                if(oldLevel==20)pet.Experience=PetProgress.MaxExperience;
                else
                {
                    var oldStart=PetProgress.LegacyRequiredExperience(oldLevel);
                    var oldSpan=PetProgress.LegacyRequiredExperience(oldLevel+1)-oldStart;
                    var newStart=PetProgress.RequiredExperience(oldLevel);
                    var newSpan=PetProgress.RequiredExperience(oldLevel+1)-newStart;
                    pet.Experience=newStart+(int)((long)(pet.Experience-oldStart)*newSpan/oldSpan);
                }
                pet.SelectedFood="apple";pet.SelectedToy="";pet.CareDate=null;
                pet.FoodGrowthCount=0;pet.IndependentAnswersToday=0;pet.ToyTrainingClaimed=false;
            }
            state.FoodInventory=new();state.OwnedToys=new();state.Version=4;
        }
        if(state.FoodInventory is null||state.OwnedToys is null
            ||state.FoodInventory.Any(p=>p.Key=="apple"||p.Value<0||!PetCatalog.Foods.Any(f=>f.Id==p.Key))
            ||state.OwnedToys.Any(id=>id=="yarn"||!PetCatalog.Toys.Any(t=>t.Id==id)))
            throw new InvalidDataException("培养库存数据不合法。");
        foreach(var pet in state.Pets)
        {
            if(!PetCatalog.Foods.Any(f=>f.Id==pet.SelectedFood)
                ||pet.SelectedToy!=""&&(!PetCatalog.Toys.Any(t=>t.Id==pet.SelectedToy)
                    ||pet.SelectedToy=="yarn"&&!state.HasToy||pet.SelectedToy!="yarn"&&!state.OwnedToys.Contains(pet.SelectedToy))
                ||pet.FoodGrowthCount is <0 or >3||pet.IndependentAnswersToday is <0 or >3
                ||pet.CareDate is null&&(pet.FoodGrowthCount!=0||pet.IndependentAnswersToday!=0||pet.ToyTrainingClaimed)
                ||pet.ToyTrainingClaimed&&pet.IndependentAnswersToday<3)
                throw new InvalidDataException("宠物培养数据不合法。");
        }
        if (state.DailyAnsweredCount is < 0 or > 6 || state.LearningDate is null && state.DailyAnsweredCount != 0)
            throw new InvalidDataException("每日学习进度不合法。");
        // A content correction may retire a deduplicated parameter variant.
        // Keep its mistake reviewable using the same topic and template.
        foreach(var mistake in state.Mistakes.Where(m=>Curriculum.FindQuestion(m.QuestionId) is null))
        {
            var separator=mistake.QuestionId.LastIndexOf('-');
            if(separator<0)continue;
            var topicId=mistake.QuestionId[..separator];var suffix=mistake.QuestionId[(separator+1)..];
            var topic=Curriculum.Topics.FirstOrDefault(t=>t.Id==topicId&&t.Order<1000);
            if(topic is null||suffix.Length<2||suffix[0] is not ('q' or 'r')||!int.TryParse(suffix[1..],out var index)||index is <1 or >40)continue;
            var template=$"{topicId}-t{(index-1)%5+1}";
            var replacement=(suffix[0]=='q'?topic.Questions:topic.ReviewQuestions).FirstOrDefault(q=>q.TemplateId==template);
            if(replacement is not null)mistake.QuestionId=replacement.Id;
        }
        state.Mistakes=state.Mistakes.GroupBy(m=>m.QuestionId).Select(g=>new Mistake {
            QuestionId=g.Key,LastAnswer=g.OrderByDescending(m=>m.LastAttempt).First().LastAnswer,
            LastAttempt=g.Max(m=>m.LastAttempt),WrongCount=(int)Math.Min(int.MaxValue,g.Sum(m=>(long)m.WrongCount))
        }).ToList();
        return state;
    }
}
