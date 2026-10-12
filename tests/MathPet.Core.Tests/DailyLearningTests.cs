using MathPet.Core;
using Xunit;
namespace MathPet.Core.Tests;
public class DailyLearningTests
{
    private sealed class Clock:TimeProvider
    {
        public DateTimeOffset Now=new(2026,10,12,15,59,0,TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow()=>Now;
        public override TimeZoneInfo LocalTimeZone=>TimeZoneInfo.CreateCustomTimeZone("test-local",TimeSpan.FromHours(8),"test-local","test-local");
    }
    [Fact] public void CountsWrongAndCorrectAnswersButNotInvalidOrDuplicateSubmissions()
    {
        var engine=new GameEngine(new PlayerState(),new Clock());var session=engine.Start(StudyMode.Practice,1);
        Assert.Throws<ArgumentException>(()=>session.Submit(""));Assert.Equal(0,engine.TodayAnsweredCount);
        var coins=engine.State.Coins;session.Submit("-999");Assert.Equal(1,engine.TodayAnsweredCount);Assert.Equal(coins,engine.State.Coins);
        Assert.Throws<InvalidOperationException>(()=>session.Submit("1"));Assert.Equal(1,engine.TodayAnsweredCount);
        session.Next();session.Submit(TestAnswers.For(session.Current));Assert.Equal(2,engine.TodayAnsweredCount);
    }
    [Fact] public void PersistsPartialProgressWithoutFinishingSession()
    {
        var clock=new Clock();var engine=new GameEngine(new PlayerState(),clock);var session=engine.StartDaily(1,Semester.First);
        session.Submit(TestAnswers.For(session.Current));
        var restored=new GameEngine(StateSerialization.Deserialize(StateSerialization.Serialize(engine.State)),clock);
        Assert.Equal(1,restored.TodayAnsweredCount);Assert.Equal(0,restored.State.CompletedPractices);
    }
    [Fact] public void ResetsOnLocalMidnightEvenDuringSameQuiz()
    {
        var clock=new Clock();var engine=new GameEngine(new PlayerState(),clock);var session=engine.Start(StudyMode.Practice,1);
        session.Submit(TestAnswers.For(session.Current));session.Next();clock.Now=clock.Now.AddMinutes(2);
        Assert.Equal(0,engine.TodayAnsweredCount);
        session.Submit(TestAnswers.For(session.Current));Assert.Equal(1,engine.TodayAnsweredCount);
        Assert.Equal(new DateOnly(2026,10,13),engine.State.LearningDate);
    }
    [Fact] public void CapsAtSixAcrossModesGradesAndPetChanges()
    {
        var engine=new GameEngine(new PlayerState(),new Clock());
        foreach(var grade in new[]{1,3,6})
        {
            engine.SelectPet(grade==3?"rabbit":"cat");var session=engine.Start(StudyMode.Practice,grade);
            while(!session.Completed){session.Submit(TestAnswers.For(session.Current));session.Next();}
        }
        Assert.Equal(6,engine.TodayAnsweredCount);Assert.Equal(6,engine.State.DailyAnsweredCount);
    }
    [Fact] public void RecognizesAlreadyCompletedDailyPracticeFromOldSave()
    {
        var state=new PlayerState();state.DailyBonuses.Add("2026-10-12");state.PetName="更新前的名字";state.Coins=12345;
        var json=System.Text.Json.Nodes.JsonNode.Parse(StateSerialization.Serialize(state))!.AsObject();
        json.Remove("LearningDate");json.Remove("DailyAnsweredCount");
        var restored=StateSerialization.Deserialize(json.ToJsonString());var clock=new Clock();var engine=new GameEngine(restored,clock);
        Assert.Equal(6,engine.TodayAnsweredCount);Assert.Equal(12345,restored.Coins);Assert.Equal("更新前的名字",restored.PetName);
        clock.Now=clock.Now.AddDays(1);Assert.Equal(0,engine.TodayAnsweredCount);
    }
    [Theory] [InlineData(-1)] [InlineData(7)]
    public void RejectsInvalidSavedCounters(int count)
    {
        var state=new PlayerState{LearningDate=new DateOnly(2026,10,12),DailyAnsweredCount=count};
        Assert.Throws<InvalidDataException>(()=>StateSerialization.Deserialize(StateSerialization.Serialize(state)));
    }
}
