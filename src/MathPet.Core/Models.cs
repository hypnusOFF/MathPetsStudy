using System.Text.Json.Serialization;
namespace MathPet.Core;

public enum StudyMode { Daily, Practice, Challenge, Review, UnitChallenge, SemesterChallenge }
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
    public string? Interaction { get; init; }
    public int[] Diagram { get; init; } = [];
    // Empty keeps the original manipulation controls; other tasks read the same diagram.
    public string DiagramTask { get; init; } = "";
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
public sealed class PetProgress
{
    public string Species { get; set; } = "cat";
    public string Name { get; set; } = "团团";
    public int Experience { get; set; } = 20;
    public int Fullness { get; set; } = 65;
    public int Mood { get; set; } = 80;
    public string Headwear { get; set; } = "";
    public string Outfit { get; set; } = "";
    public string Accessory { get; set; } = "";
    public bool HideStageDecoration { get; set; }
    public string SelectedFood { get; set; } = "apple";
    public string SelectedToy { get; set; } = "";
    public DateOnly? CareDate { get; set; }
    public int FoodGrowthCount { get; set; }
    public int IndependentAnswersToday { get; set; }
    public bool ToyTrainingClaimed { get; set; }
    [JsonIgnore] public int Level => Enumerable.Range(1,20).Last(l => Experience >= RequiredExperience(l));
    [JsonIgnore] public string Stage => Level >= 13 ? "mature" : Level >= 6 ? "young" : "baby";
    public static int RequiredExperience(int level)
    {
        var n=level-1;return 35*n+5*n*(n+1)/2+n*(n+1)*(2*n+1)/3;
    }
    public static int LegacyRequiredExperience(int level) => (level-1)*25+(level-1)*level*5/2;
    public const int MaxExperience = 6555;
}
public sealed record Wearable(string Id,string Name,string Slot,int Price);
public sealed class PlayerState
{
    public int Version { get; set; } = 4;
    public string ActiveSpecies { get; set; } = "cat";
    public List<PetProgress> Pets { get; set; } = [new(), new(){Species="rabbit",Name="糯糯",Experience=0},new(){Species="fox",Name="星星",Experience=0}];
    public HashSet<string> OwnedWearables { get; set; } = new();
    public string Room { get; set; } = "study";
    public HashSet<string> OwnedRooms { get; set; } = ["study"];
    public HashSet<string> OwnedRoomDecorations { get; set; } = [];
    public HashSet<string> PlacedRoomDecorations { get; set; } = [];
    public bool ReducedMotion { get; set; }
    public bool SoundEnabled { get; set; } = true;
    [JsonIgnore] public PetProgress ActivePet => Pets.Single(p=>p.Species==ActiveSpecies);
    public int Grade { get; set; } = 1;
    public Semester Semester { get; set; }
    // Temporary acceptance-test balance; remove when room decorating is approved.
    public const int InitialTestCoins = 99999;
    public int Coins { get; set; } = InitialTestCoins;
    [JsonIgnore] public string PetName { get=>ActivePet.Name; set=>ActivePet.Name=value; }
    [JsonIgnore] public int Fullness { get=>ActivePet.Fullness; set=>ActivePet.Fullness=value; }
    [JsonIgnore] public int Mood { get=>ActivePet.Mood; set=>ActivePet.Mood=value; }
    [JsonIgnore] public int Growth { get=>Math.Min(100,ActivePet.Experience); set=>ActivePet.Experience=value; }
    public int Food { get; set; } = 2;
    public bool HasToy { get; set; }
    public Dictionary<string,int> FoodInventory { get; set; } = new();
    public HashSet<string> OwnedToys { get; set; } = new();
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
    public DateOnly? LearningDate { get; set; }
    public int DailyAnsweredCount { get; set; }
    public DateTimeOffset LastSeen { get; set; } = DateTimeOffset.UtcNow;
}
public sealed record AnswerResult(bool Correct, int Points, bool TimedOut, string Submitted,
    Question Question, bool UsedHint)
{
    public int LearningExperience { get; init; }
}
public sealed record SessionSummary(StudyMode Mode, int Correct, int Total, int QuestionPoints,
    int CompletionBonus, int GrowthGained, bool Passed, int Stars, int Grade, string TopicName);

public interface IPlayerStore
{
    Task<PlayerState?> LoadAsync();
    Task SaveAsync(PlayerState state);
}
