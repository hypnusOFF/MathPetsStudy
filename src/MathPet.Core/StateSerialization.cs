using System.Text.Json;
namespace MathPet.Core;

public static class StateSerialization
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    public static string Serialize(PlayerState state) => JsonSerializer.Serialize(state,Options);
    public static PlayerState Deserialize(string text)
    {
        var state = JsonSerializer.Deserialize<PlayerState>(text,Options) ?? throw new InvalidDataException("存档为空。");
        if (state.Version != 1) throw new InvalidDataException("此存档版本暂不支持，请保留原文件。");
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
