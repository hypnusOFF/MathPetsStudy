using MathPet.Core;
using System.Text.RegularExpressions;
using Xunit;

namespace MathPet.Core.Tests;

public sealed class QuestionBankTests
{
    private static Topic NewTopic(int grade = 1, Semester semester = Semester.First) =>
        Curriculum.ForSemester(grade, semester).First(t => t.Order < 1000);

    [Fact]
    public void TwelveSemestersHaveMultipleTopicsAndBalancedReviewPairs()
    {
        foreach (var grade in Enumerable.Range(1, 6))
        foreach (var semester in Enum.GetValues<Semester>())
        {
            var topics = Curriculum.ForSemester(grade, semester).Where(t => t.Order < 1000).ToArray();
            Assert.True(topics.Length >= 6);
            foreach (var topic in topics)
            {
                Assert.True(topic.Questions.Select(q => q.TemplateId).Distinct().Count() >= 4);
                Assert.True(topic.Questions.Select(q => q.QuestionType).Distinct().Count() >= 4);
                Assert.Equal(topic.Questions.Count, topic.ReviewQuestions.Count);
                for (var i = 0; i < topic.Questions.Count; i++)
                {
                    var original = topic.Questions[i]; var review = topic.ReviewQuestions[i];
                    Assert.Equal(original.TemplateId, review.TemplateId);
                    Assert.NotEqual(original.Text, review.Text);
                }
                Assert.Equal(topic.Questions.Count * 2, topic.Questions.Concat(topic.ReviewQuestions)
                    .Select(q => (q.Text, q.Answer)).Distinct().Count());
            }
        }
    }

    [Fact]
    public void EntireBankHasValidAnswersMetadataAndUniqueIds()
    {
        var questions = Curriculum.Topics.SelectMany(t => t.Questions.Concat(t.ReviewQuestions)).ToArray();
        Assert.True(questions.Length > 7000);
        Assert.Equal(questions.Length, questions.Select(q => q.Id).Distinct().Count());
        foreach (var q in questions)
        {
            Assert.True(AnswerChecker.IsCorrect(q, q.Answer), q.Id);
            Assert.InRange(q.TimeLimitSeconds, 30, 120);
            Assert.NotEmpty(q.CommonMistake); Assert.True(q.Hints.Length >= 2);
            Assert.NotEmpty(q.Solution); Assert.Equal(q.Grade, Curriculum.FindTopic(q.TopicId).Grade);
            if (q.Options.Length > 0) Assert.InRange(int.Parse(q.Answer), 0, q.Options.Length - 1);
            if (q.Unit is not null)
            {
                Assert.True(AnswerChecker.IsCorrect(q, q.Answer + q.Unit));
                Assert.False(AnswerChecker.IsCorrect(q, q.Answer + "不匹配的单位"));
            }
        }
    }

    [Fact]
    public void PracticeBalancesTypesAndAdvancesToUnusedVariants()
    {
        var topic = NewTopic(); var engine = new GameEngine(new());
        var first = engine.Start(StudyMode.Practice, topic.Grade, topicId: topic.Id);
        Assert.InRange(first.Questions.Count, 4, 6);
        Assert.Equal(first.Questions.Count, first.Questions.Select(q => q.TemplateId).Distinct().Count());
        Complete(first);
        var second = engine.Start(StudyMode.Practice, topic.Grade, topicId: topic.Id);
        Assert.Empty(first.Questions.Select(q => q.Id).Intersect(second.Questions.Select(q => q.Id)));
    }

    [Fact]
    public void ReviewFindsTheExactTopicAndMatchedTemplateAcrossGrades()
    {
        var topic = Curriculum.ForSemester(6, Semester.Second).First(t => t.Name == "圆锥体积");
        var state = new PlayerState(); var engine = new GameEngine(state);
        var quiz = engine.Start(StudyMode.Practice, 6, topicId: topic.Id);
        var original = quiz.Current; quiz.Submit("-999");
        var review = engine.Start(StudyMode.Review, 1, original.Id);
        Assert.Equal(topic.Id, review.Topic.Id);
        Assert.Equal(original.TemplateId, review.Current.TemplateId);
        Assert.NotEqual(original.Text, review.Current.Text);
        Complete(review); Assert.Empty(state.Mistakes);
    }

    [Fact]
    public void DailyMixesTopicsOnlyWithinSelectedSemesterAndIncludesMistake()
    {
        var state = new PlayerState(); var engine = new GameEngine(state);
        var topic = Curriculum.ForSemester(5, Semester.Second).Last(t => t.Order < 1000);
        var wrong = engine.Start(StudyMode.Practice, 5, topicId: topic.Id); wrong.Submit("-999");
        var daily = engine.StartDaily(5, Semester.Second);
        Assert.Equal(6, daily.Questions.Count);
        Assert.True(daily.Questions.Select(q => q.TopicId).Distinct().Count() >= 5);
        Assert.Contains(daily.Questions, q => q.Id == wrong.Current.Id);
        Assert.All(daily.Questions, q => { Assert.Equal(5, q.Grade); Assert.Equal(Semester.Second, Curriculum.FindTopic(q.TopicId).Semester); });
        Assert.Equal("跨知识点每日练习", Complete(daily).TopicName);
    }

    [Fact]
    public void FiveQuestionPassKeepsStarsInsideSaveRangeAndRequiresFourIndependentAnswers()
    {
        var topic = NewTopic(); var engine = new GameEngine(new());
        var quiz = engine.Start(StudyMode.Challenge, 1, topicId: topic.Id);
        var summary = Complete(quiz); Assert.Equal(3, summary.Stars); Assert.True(summary.Passed);
        Assert.NotNull(StateSerialization.Deserialize(StateSerialization.Serialize(engine.State)));
        var failed = engine.Start(StudyMode.Challenge, 1, topicId: topic.Id);
        for (var i = 0; i < failed.Questions.Count; i++)
        { if (i >= 3) failed.RevealHint(); failed.Submit(failed.Current.Answer); failed.Next(); }
        Assert.False(failed.Finish().Passed);
    }

    [Fact]
    public void ArithmeticNamesMatchDigitAndCarryConstraints()
    {
        foreach (var topic in Curriculum.Topics.Where(t => t.Order < 1000))
        foreach (var q in topic.Questions.Concat(topic.ReviewQuestions).Where(q => q.QuestionType == "直接计算"))
        {
            var match = Regex.Match(q.Text, @"^(\d+) [＋+−×÷] (\d+) =");
            if (!match.Success) continue;
            var a = int.Parse(match.Groups[1].Value); var b = int.Parse(match.Groups[2].Value);
            if (topic.Name == "20以内进位加法") { Assert.InRange(a, 1, 9); Assert.InRange(b, 1, 9); Assert.InRange(a + b, 11, 18); }
            if (topic.Name == "20以内退位减法") { Assert.InRange(a, 11, 18); Assert.True(a % 10 < b); Assert.InRange(b, 1, 9); }
            if (topic.Name == "乘法笔算与应用") { Assert.InRange(a, 100, 999); Assert.InRange(b, 10, 99); }
            if (topic.Name == "两位数加一位数") { Assert.InRange(a, 10, 99); Assert.InRange(b, 1, 9); Assert.True(a + b < 100); }
            if (topic.Name == "整除笔算与应用") Assert.InRange(b, 10, 99);
        }
    }

    [Fact]
    public void SameQuestionTextCannotHaveConflictingAnswersAcrossTopics()
    {
        var questions = Curriculum.Topics.SelectMany(t => t.Questions.Concat(t.ReviewQuestions));
        foreach (var group in questions.GroupBy(q => q.Text))
            Assert.All(group, q => Assert.True(AnswerChecker.IsCorrect(group.First(), q.Answer), q.Text));
    }

    private static SessionSummary Complete(QuizSession quiz)
    {
        while (!quiz.Completed) { quiz.Submit(quiz.Current.Answer); quiz.Next(); }
        return quiz.Finish();
    }
}
