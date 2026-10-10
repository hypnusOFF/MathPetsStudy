namespace MathPet.Core;

public sealed class GameEngine(PlayerState state, TimeProvider? clock = null)
{
    public PlayerState State { get; } = state;
    public TimeProvider Clock { get; } = clock ?? TimeProvider.System;
    public static readonly decimal[] GradeMultipliers = [1m, 1.3m, 1.6m, 2m, 2.5m, 3m];
    public static IReadOnlyList<ShopItem> Shop { get; } =
    [
        new(ItemKind.Food,"苹果小点心","🍎",20,"饱腹 +20，经验 +2"),
        new(ItemKind.Toy,"彩色毛线球","🧶",50,"永久玩具，玩耍心情 +15"),
        new(ItemKind.Ribbon,"紫色蝴蝶结","🎀",80,"可穿戴装饰，不影响答题"),
        new(ItemKind.Room,"春日小屋","🌷",100,"可切换的家园装饰")
    ];
    public QuizSession Start(StudyMode mode, int grade, string? mistakeId = null, string? topicId = null)
    {
        var topic = topicId is null ? Curriculum.ForGrade(grade) : Curriculum.FindTopic(topicId);
        if (topic.Grade != grade) throw new ArgumentException("知识点与年级不匹配。");
        var questions = topic.Questions.ToList();
        string? reviewOriginal = null;
        if (mode == StudyMode.Review)
        {
            var original = Curriculum.FindQuestion(mistakeId ?? "") ?? throw new ArgumentException("错题不存在。");
            topic = Curriculum.FindTopic(original.TopicId);
            if (!State.Mistakes.Any(m => m.QuestionId == original.Id)) throw new ArgumentException("该题已不在错题列表。");
            var index = topic.Questions.ToList().FindIndex(q => q.Id == original.Id);
            if (index < 0) index = topic.ReviewQuestions.ToList().FindIndex(q => q.Id == original.Id);
            if (index < 0) throw new ArgumentException("相似题不存在。");
            // A failed review stays paired with the original. Reviewing a variant
            // uses the original problem rather than offering the identical item.
            questions = [topic.Questions.Any(q => q.Id == original.Id)
                ? topic.ReviewQuestions[index] : topic.Questions[index]];
            reviewOriginal = original.Id;
        }
        else if (topicId is not null)
        {
            // Balance templates and rotate their variants using recorded attempts.
            // Stable IDs make old mistakes and reduced repeat rewards reproducible.
            var groups = questions.GroupBy(q => q.TemplateId).ToArray();
            questions = groups.Select(group => group.OrderBy(q => State.Progress.GetValueOrDefault(q.Id)?.AttemptCount ?? 0)
                .ThenBy(q => q.Id).First()).Take(6).ToList();
            if (groups.Length == 1) questions = topic.Questions.OrderBy(q => State.Progress.GetValueOrDefault(q.Id)?.AttemptCount ?? 0)
                .ThenBy(q => q.Id).Take(6).ToList();
        }
        return new QuizSession(this, mode, topic, questions, reviewOriginal);
    }
    public QuizSession StartDaily(int grade, Semester semester)
    {
        var topics = Curriculum.ForSemester(grade, semester).Where(t => t.Order < 1000).ToArray();
        var selected = topics.OrderBy(t => t.Questions.Sum(q => State.Progress.GetValueOrDefault(q.Id)?.AttemptCount ?? 0))
            .ThenBy(t => t.Order).Take(6).ToArray();
        var questions = selected.Select((t, i) => t.Questions
            .OrderBy(q => State.Progress.GetValueOrDefault(q.Id)?.AttemptCount ?? 0)
            .ThenBy(q => q.TemplateId == t.Questions[i % 5].TemplateId ? 0 : 1)
            .ThenBy(q => q.Id).First()).ToList();
        // Include one saved misconception from this semester without repeating a
        // question already in the daily selection.
        var original = State.Mistakes.Select(m => Curriculum.FindQuestion(m.QuestionId))
            .FirstOrDefault(q => q is not null && topics.Any(t => t.Id == q.TopicId));
        if (original is not null && !questions.Any(q => q.Id == original.Id)) questions[^1] = original;
        return new QuizSession(this, StudyMode.Daily, selected[0], questions, null);
    }
    public static IReadOnlyList<Wearable> Wardrobe { get; } = [
        new("bow","紫色蝴蝶结","head",80),new("hat","薄荷帽","head",100),new("star-clip","星星发夹","head",90),
        new("scarf","珊瑚围巾","body",110),new("overalls","薄荷背带衣","body",160),new("cape","淡紫披风","body",180),
        new("backpack","小书包","accessory",140),new("bell","铃铛项链","accessory",90),new("badge","星星徽章","accessory",120)];
    public void SelectPet(string species)
    {
        if (!State.Pets.Any(p=>p.Species==species)) throw new ArgumentException("宠物不存在。");
        State.ActiveSpecies=species;
    }
    public (bool Success,string Message) BuyWearable(string id)
    {
        var item=Wardrobe.SingleOrDefault(w=>w.Id==id) ?? throw new ArgumentException("衣饰不存在。");
        if(State.OwnedWearables.Contains(id)) return(false,"已经拥有这件衣饰。");
        if(State.Coins<item.Price)return(false,"积分还不够。");
        State.Coins-=item.Price;State.OwnedWearables.Add(id);return(true,"已购买"+item.Name);
    }
    public void Equip(string slot,string id)
    {
        if(id!="" && (!State.OwnedWearables.Contains(id)||!Wardrobe.Any(w=>w.Id==id&&w.Slot==slot)))throw new ArgumentException("衣饰未拥有或槽位不匹配。");
        switch(slot){case "head":State.ActivePet.Headwear=id;break;case "body":State.ActivePet.Outfit=id;break;case "accessory":State.ActivePet.Accessory=id;State.ActivePet.HideStageDecoration=true;break;default:throw new ArgumentException("槽位不存在。");}
    }
    public QuizSession StartComprehensive(int grade,Semester semester,string? unit=null)
    {
        var topics=Curriculum.ForSemester(grade,semester).Where(t=>(t.Order<1000||t.Order>=2000)&&(unit is null||t.UnitName==unit)).ToArray();
        if(topics.Length==0)throw new ArgumentException("没有可用的综合关内容。");
        var chosen=topics.OrderBy(t=>t.Order).Select(t=>t.Questions.OrderBy(q=>State.Progress.GetValueOrDefault(q.Id)?.AttemptCount??0).ThenBy(q=>q.Id).First()).ToList();
        if(chosen.Count<3)chosen=topics.SelectMany(t=>t.Questions).GroupBy(q=>q.TemplateId).Select(g=>g.First()).Take(3).ToList();
        if(chosen.Count>12)chosen=Enumerable.Range(0,12).Select(i=>chosen[i*chosen.Count/12]).ToList();
        var key=$"comprehensive:{grade}:{semester}:{unit??"semester"}";
        return new QuizSession(this,unit is null?StudyMode.SemesterChallenge:StudyMode.UnitChallenge,topics[0],chosen,null,key,unit is null?"学期综合关":unit+"单元关");
    }
    public Mastery GetMastery(Topic topic)
    {
        if (State.Mistakes.Any(m => Curriculum.FindQuestion(m.QuestionId)?.TopicId == topic.Id)) return Mastery.NeedsReview;
        if (topic.Questions.GroupBy(q => q.TemplateId == "legacy" ? q.Id : q.TemplateId)
            .All(group => group.Sum(q => State.Progress.GetValueOrDefault(q.Id)?.IndependentCorrectCount ?? 0) >= 2)
            && State.Stars.ContainsKey(topic.Id)) return Mastery.Mastered;
        return State.ViewedTopics.Contains(topic.Id) || topic.Questions.Any(q => State.Progress.ContainsKey(q.Id))
            ? Mastery.Learning : Mastery.NotStarted;
    }
    public int Score(Question q, bool usedHint, bool timeBonus)
    {
        var repeated = State.Progress.TryGetValue(q.Id, out var p) && p.CorrectCount > 0;
        var multiplier = (usedHint ? .6m : 1m) * (repeated ? .3m : 1m);
        var points = Math.Max(1, (int)Math.Round(10 * GradeMultipliers[q.Grade-1] * q.Difficulty * multiplier, MidpointRounding.AwayFromZero));
        // Repeats never earn the speed bonus, so brushing the same question stays low reward.
        return points + (timeBonus && !usedHint && !repeated ? 2 : 0);
    }
    public bool Owns(ItemKind kind) => kind switch
    {
        ItemKind.Food => State.Food > 0, ItemKind.Toy => State.HasToy,
        ItemKind.Ribbon => State.HasRibbon || State.OwnedWearables.Contains("bow"), ItemKind.Room => State.HasRoom, _ => false
    };
    public (bool Success, string Message) Buy(ItemKind kind)
    {
        var item = Shop.Single(i => i.Kind == kind);
        if (kind != ItemKind.Food && Owns(kind)) return (false,"已经拥有这件物品。");
        if (State.Coins < item.Price) return (false,"积分还不够，先去学一点数学吧。");
        State.Coins -= item.Price;
        switch (kind)
        {
            case ItemKind.Food: State.Food++; break;
            case ItemKind.Toy: State.HasToy = true; break;
            case ItemKind.Ribbon: State.HasRibbon = true; State.OwnedWearables.Add("bow"); break;
            case ItemKind.Room: State.HasRoom = true; break;
        }
        return (true,$"已购买{item.Name}，放入背包。");
    }
    public string Feed()
    {
        if (State.Food == 0) return "点心用完了，去商店看看吧。";
        if (State.Fullness == 100) return $"{State.PetName}已经吃饱了，点心先存着吧。";
        State.Food--; State.Fullness = Math.Min(100, State.Fullness + 20);
        AddGrowth(2);
        return $"{State.PetName}吃得很开心！饱腹增加，经验 +2。";
    }
    public string Play()
    {
        State.Mood = Math.Min(100, State.Mood + (State.HasToy ? 15 : 8));
        return State.HasToy ? "一起玩毛线球，心情增加！" : "一起玩捉迷藏，心情增加！";
    }
    public string Care()
    {
        State.Mood = Math.Min(100, State.Mood + 5);
        return $"{State.PetName}蹭了蹭你的手，心情增加！";
    }
    public string PetStage => State.ActivePet.Stage switch { "mature"=>"最终进化", "young"=>"初次进化", _=>"幼年期" };
    public void ApplyAbsence()
    {
        var now = Clock.GetUtcNow();
        var days = Math.Clamp((int)(now - State.LastSeen).TotalDays, 0, 30);
        foreach(var pet in State.Pets){pet.Fullness=Math.Max(30,pet.Fullness-days*5);pet.Mood=Math.Max(40,pet.Mood-days*3);}
        State.LastSeen = now;
    }
    internal int AddGrowth(int amount,string? species=null)
    {
        var pet=State.Pets.Single(p=>p.Species==(species??State.ActiveSpecies));
        var before=pet.Experience;pet.Experience=Math.Min(PetProgress.MaxExperience,pet.Experience+Math.Max(0,amount));
        return pet.Experience-before;
    }

}

public sealed class QuizSession
{
    private readonly GameEngine engine;
    private readonly string? reviewOriginal;
    private readonly DateOnly localDate;
    private DateTimeOffset questionStarted;
    private readonly List<AnswerResult> answers = [];
    private SessionSummary? summary;
    private readonly string rewardKey,summaryName;
    public string RewardSpecies { get; }
    public string RewardPetName { get; }
    public bool IsChallenge => Mode is StudyMode.Challenge or StudyMode.UnitChallenge or StudyMode.SemesterChallenge;
    public StudyMode Mode { get; }
    public Topic Topic { get; }
    public IReadOnlyList<Question> Questions { get; }
    public int Index { get; private set; }
    public int HintCount { get; private set; }
    public Question Current => Questions[Index];
    public bool Answered { get; private set; }
    public bool Completed => summary is not null;
    public AnswerResult? LastResult => answers.LastOrDefault();
    public int RemainingSeconds => Math.Max(0, Current.TimeLimitSeconds - (int)(engine.Clock.GetUtcNow() - questionStarted).TotalSeconds);
    public bool TimedOut => IsChallenge && RemainingSeconds == 0;
    internal QuizSession(GameEngine engine, StudyMode mode, Topic topic, IReadOnlyList<Question> questions, string? reviewOriginal,string? rewardKey=null,string? summaryName=null)
    {
        this.rewardKey=rewardKey??topic.Id;this.summaryName=summaryName??topic.Name;
        RewardSpecies=engine.State.ActiveSpecies;RewardPetName=engine.State.PetName;
        this.engine = engine; Mode = mode; Topic = topic; Questions = questions; this.reviewOriginal = reviewOriginal;
        questionStarted = engine.Clock.GetUtcNow();
        localDate = DateOnly.FromDateTime(engine.Clock.GetLocalNow().DateTime);
    }
    public string? RevealHint()
    {
        if (Answered || Completed || HintCount >= Current.Hints.Length) return null;
        return Current.Hints[HintCount++];
    }
    public AnswerResult Submit(string input)
    {
        if (Answered || Completed) throw new InvalidOperationException("本题已经提交。");
        if (string.IsNullOrWhiteSpace(input)) throw new ArgumentException("请先输入答案。");
        if (input.Length > 100) throw new ArgumentException("答案过长，请检查输入。");
        var q = Current;
        var check=AnswerChecker.Check(q,input);
        if(check.RetryMessage is not null)throw new ArgumentException(check.RetryMessage);
        var correct = check.Correct;
        var points = correct ? engine.Score(q, HintCount > 0, IsChallenge && !TimedOut) : 0;
        var result = new AnswerResult(correct,points,TimedOut,input,q,HintCount > 0);
        var state = engine.State;
        if (!state.Progress.TryGetValue(q.Id, out var progress)) state.Progress[q.Id] = progress = new();
        progress.AttemptCount++;
        if (correct)
        {
            progress.CorrectCount++; if (HintCount == 0) progress.IndependentCorrectCount++; progress.LastCorrect = engine.Clock.GetUtcNow();
            state.Coins += points;
            if (Mode == StudyMode.Review && reviewOriginal is not null && HintCount == 0)
            {
                state.Mistakes.RemoveAll(m => m.QuestionId == reviewOriginal || m.QuestionId == q.Id);
                state.ReviewedMistakes++;
            }
        }
        else
        {
            // Keep one entry for a review pair. A failed variant preserves the original misconception.
            var mistakeKey = reviewOriginal ?? q.Id;
            var mistake = state.Mistakes.FirstOrDefault(m => m.QuestionId == mistakeKey);
            if (mistake is null) { mistake = new Mistake { QuestionId = mistakeKey }; state.Mistakes.Add(mistake); }
            mistake.LastAnswer = input; mistake.WrongCount++; mistake.LastAttempt = engine.Clock.GetUtcNow();
        }
        state.ViewedTopics.Add(q.TopicId);
        answers.Add(result); Answered = true;
        return result;
    }
    public bool Next()
    {
        if (!Answered || Completed) return false;
        if (Index == Questions.Count - 1) { Finish(); return false; }
        Index++; HintCount = 0; Answered = false; questionStarted = engine.Clock.GetUtcNow();
        return true;
    }
    public SessionSummary Finish()
    {
        if (summary is not null) return summary;
        if (answers.Count != Questions.Count) throw new InvalidOperationException("请先完成所有题目。");
        var state = engine.State;
        var correct = answers.Count(a => a.Correct);
        // Hints help learning, but hinted answers do not satisfy an independent challenge pass.
        var independent = answers.Count(a => a.Correct && !a.UsedHint);
        var passed = IsChallenge && independent >= (int)Math.Ceiling(Questions.Count * 2m / 3m);
        var bonus = 0; var stars = passed ? independent == Questions.Count ? 3 : independent >= (int)Math.Ceiling(Questions.Count * .8m) ? 2 : 1 : 0;
        if (passed)
        {
            if (!state.Stars.ContainsKey(rewardKey)) bonus = (int)(20 * GameEngine.GradeMultipliers[Topic.Grade-1]);
            state.Stars[rewardKey] = Math.Max(state.Stars.GetValueOrDefault(rewardKey), stars);
        }
        if (Mode is StudyMode.Daily or StudyMode.Practice) state.CompletedPractices++;
        if (Mode == StudyMode.Daily && state.DailyBonuses.Add($"{localDate:yyyy-MM-dd}")) bonus += 10;
        state.Coins += bonus;
        var growth = engine.AddGrowth(answers.Sum(a=>a.Points)/2,RewardSpecies);
        summary = new(Mode,correct,Questions.Count,answers.Sum(a => a.Points),bonus,growth,passed,stars,Topic.Grade,
            Mode == StudyMode.Daily ? "跨知识点每日练习" : summaryName);
        return summary;
    }
}
