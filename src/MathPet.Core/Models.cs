namespace MathPet.Core;

public enum StudyMode { Daily, Practice, Challenge, Review }
public enum Semester { First, Second }
public enum Mastery { NotStarted, Learning, NeedsReview, Mastered }
public enum ItemKind { Food, Toy, Ribbon, Room }

public sealed record Question(string Id, int Grade, string TopicId, string Text, string Answer,
    string? Unit, string[] Hints, string[] Solution, string CommonMistake, int TimeLimitSeconds,
    decimal Difficulty = 1m)
{
    public string QuestionType { get; init; } = "基础练习";
    public string TemplateId { get; init; } = "legacy";
    public string[] Options { get; init; } = [];
    public string Source { get; init; } = "独立原创";
}
public sealed record Topic(string Id, int Grade, string Name, string Area, string Goal,
    string Explanation, string Visual, string[] ExampleSteps, IReadOnlyList<Question> Questions,
    IReadOnlyList<Question> ReviewQuestions)
{
    public Semester Semester { get; init; }
    public string UnitName { get; init; } = "基础练习";
    public int Order { get; init; }
}
public sealed record ShopItem(ItemKind Kind, string Name, string Icon, int Price, string Description);
public sealed class Mistake
{
    public string QuestionId { get; set; } = "";
    public string LastAnswer { get; set; } = "";
    public int WrongCount { get; set; }
    public DateTimeOffset LastAttempt { get; set; }
}
public sealed class QuestionProgress
{
    public int CorrectCount { get; set; }
    public int IndependentCorrectCount { get; set; }
    public int AttemptCount { get; set; }
    public DateTimeOffset? LastCorrect { get; set; }
}
public sealed class PlayerState
{
    public int Version { get; set; } = 1;
    public int Grade { get; set; } = 1;
    public Semester Semester { get; set; }
    public int Coins { get; set; } = 120;
    public string PetName { get; set; } = "团团";
    public int Fullness { get; set; } = 65;
    public int Mood { get; set; } = 80;
    public int Growth { get; set; } = 20;
    public int Food { get; set; } = 2;
    public bool HasToy { get; set; }
    public bool HasRibbon { get; set; }
    public bool WearingRibbon { get; set; }
    public bool HasRoom { get; set; }
    public bool UsingRoom { get; set; }
    public int CompletedPractices { get; set; }
    public int ReviewedMistakes { get; set; }
    public Dictionary<string, int> Stars { get; set; } = new();
    public Dictionary<string, QuestionProgress> Progress { get; set; } = new();
    public List<Mistake> Mistakes { get; set; } = new();
    public HashSet<string> ViewedTopics { get; set; } = new();
    public HashSet<string> DailyBonuses { get; set; } = new();
    public DateTimeOffset LastSeen { get; set; } = DateTimeOffset.UtcNow;
}
public sealed record AnswerResult(bool Correct, int Points, bool TimedOut, string Submitted,
    Question Question, bool UsedHint);
public sealed record SessionSummary(StudyMode Mode, int Correct, int Total, int QuestionPoints,
    int CompletionBonus, int GrowthGained, bool Passed, int Stars, int Grade, string TopicName);

public interface IPlayerStore
{
    Task<PlayerState?> LoadAsync();
    Task SaveAsync(PlayerState state);
}
