using System.Text.Json;
namespace MathPet.Core;

public static class StateSerialization
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    public static string Serialize(PlayerState state) => JsonSerializer.Serialize(state,Options);
    public static PlayerState Deserialize(string text)
    {
        var state = JsonSerializer.Deserialize<PlayerState>(text,Options) ?? throw new InvalidDataException("存档为空。");
        using var document=JsonDocument.Parse(text);
        var root=document.RootElement;
        var version=root.TryGetProperty("Version",out var v)?v.GetInt32():1;
        if(version is not (1 or 2))throw new InvalidDataException("此存档版本暂不支持，请保留原文件。");
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
            pet.Experience=Math.Max(growth,PetProgress.RequiredExperience(mature?13:young?6:1));
            if(state.HasRibbon){state.OwnedWearables.Add("bow");if(state.WearingRibbon)pet.Headwear="bow";}
            if(state.UsingRoom)state.Room="spring";
            state.Version=2;
        }
        if(state.Pets is null||state.Pets.Count!=3||state.Pets.Any(p=>p is null)||state.Pets.Select(p=>p.Species).Order().SequenceEqual(new[]{"cat","fox","rabbit"})==false
            ||!state.Pets.Any(p=>p.Species==state.ActiveSpecies)||state.OwnedWearables is null
            ||state.Room is not ("study" or "spring" or "stars"))throw new InvalidDataException("宠物存档不完整。");
        foreach(var pet in state.Pets)
        {
            if(string.IsNullOrWhiteSpace(pet.Name)||pet.Name.Length>12||pet.Experience<0||pet.Experience>PetProgress.MaxExperience
                ||pet.Fullness is <30 or >100||pet.Mood is <40 or >100)throw new InvalidDataException("宠物数据不合法。");
            foreach(var pair in new[]{("head",pet.Headwear),("body",pet.Outfit),("accessory",pet.Accessory)})
                if(pair.Item2!=""&&(!state.OwnedWearables.Contains(pair.Item2)||!GameEngine.Wardrobe.Any(w=>w.Id==pair.Item2&&w.Slot==pair.Item1)))throw new InvalidDataException("穿搭数据不合法。");
        }
        if(state.OwnedWearables.Any(id=>!GameEngine.Wardrobe.Any(w=>w.Id==id)))throw new InvalidDataException("衣饰数据不合法。");
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
        return state;
    }
}
