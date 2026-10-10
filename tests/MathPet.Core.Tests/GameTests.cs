using MathPet.Core;
using Xunit;
namespace MathPet.Core.Tests;

public sealed class GameTests
{
    private sealed class ManualClock:TimeProvider
    {
        public DateTimeOffset Now = new(2026,10,8,10,0,0,TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow()=>Now;
        public override TimeZoneInfo LocalTimeZone=>TimeZoneInfo.Utc;
    }
    private static SessionSummary Complete(QuizSession session, bool hints=false)
    {
        while(!session.Completed)
        {
            if(hints)session.RevealHint();
            session.Submit(TestAnswers.For(session.Current));
            session.Next();
        }
        return session.Finish();
    }
    [Theory]
    [InlineData("5/6",true)] [InlineData("10/12",true)] [InlineData("５／６",true)]
    [InlineData("0.833333333",false)] [InlineData("5/0",false)] [InlineData("",false)]
    [InlineData("NaN",false)] [InlineData("2/5",false)] [InlineData("5/6元",false)]
    public void FractionsUseExactEquivalence(string input,bool expected)
        =>Assert.Equal(expected,AnswerChecker.IsCorrect(Curriculum.ForGrade(5).Questions[0],input));
    [Theory]
    [InlineData("24",true)] [InlineData("24平方厘米",true)] [InlineData("２４ 平方厘米",true)]
    [InlineData("24厘米",false)] [InlineData("24平方米",false)] [InlineData("24.000",true)]
    [InlineData("24cm",false)] [InlineData("23",false)]
    public void UnitMustMatchIfProvided(string input,bool expected)
        =>Assert.Equal(expected,AnswerChecker.IsCorrect(Curriculum.ForGrade(3).Questions[0],input));
    [Fact] public void AllSampleAnswersAreAcceptedAndContentIsComplete()
    {
        var qs=Curriculum.Topics.Where(t=>t.Order==1000).SelectMany(t=>t.Questions.Concat(t.ReviewQuestions)).ToList();
        Assert.Equal(36,qs.Count);Assert.Equal(36,qs.Select(q=>q.Id).Distinct().Count());
        foreach(var q in qs){Assert.True(AnswerChecker.IsCorrect(q,TestAnswers.For(q)));Assert.True(q.TimeLimitSeconds>0);Assert.NotEmpty(q.Solution);Assert.NotEmpty(q.Hints);Assert.NotEmpty(q.CommonMistake);}
    }
    [Fact] public void HigherGradesGetMorePointsForSameDifficulty()
    {
        var engine=new GameEngine(new());
        var scores=Enumerable.Range(1,6).Select(g=>engine.Score(Curriculum.ForGrade(g).Questions[0],false,false)).ToList();
        Assert.Equal(new[]{10,13,16,20,25,30},scores);
    }
    [Fact] public void DuplicateSubmissionAndPrematureFinishCannotEarnRewards()
    {
        var state=new PlayerState();var quiz=new GameEngine(state).Start(StudyMode.Daily,1);
        Assert.Throws<InvalidOperationException>(()=>quiz.Finish());
        Assert.Throws<ArgumentException>(()=>quiz.Submit(" "));
        Assert.False(quiz.Next());quiz.Submit("5");
        Assert.Throws<InvalidOperationException>(()=>quiz.Submit("5"));Assert.Equal(PlayerState.InitialTestCoins+10,state.Coins);
    }
    [Fact] public void RepeatedCorrectAnswersHaveReducedReward()
    {
        var engine=new GameEngine(new());
        var first=engine.Start(StudyMode.Practice,1);var one=first.Submit("5");
        var repeat=engine.Start(StudyMode.Practice,1);var two=repeat.Submit("5");
        Assert.Equal(10,one.Points);Assert.Equal(3,two.Points);
    }
    [Fact] public void HintReducesRewardButNeverCostsCoins()
    {
        var state=new PlayerState();var quiz=new GameEngine(state).Start(StudyMode.Practice,1);
        Assert.NotNull(quiz.RevealHint());var result=quiz.Submit("5");Assert.Equal(6,result.Points);Assert.Equal(PlayerState.InitialTestCoins+6,state.Coins);
    }
    [Fact] public void ChallengeHintsCannotUnlockIndependentPass()
    {
        var state=new PlayerState();var result=Complete(new GameEngine(state).Start(StudyMode.Challenge,1),hints:true);
        Assert.Equal(3,result.Correct);Assert.False(result.Passed);Assert.Empty(state.Stars);
    }
    [Fact] public void TimeoutStillAcceptsCorrectAnswerWithoutSpeedBonus()
    {
        var clock=new ManualClock();var quiz=new GameEngine(new(),clock).Start(StudyMode.Challenge,1);
        clock.Now=clock.Now.AddMinutes(2);Assert.Equal(0,quiz.RemainingSeconds);
        var result=quiz.Submit("5");Assert.True(result.Correct);Assert.True(result.TimedOut);Assert.Equal(10,result.Points);
    }
    [Fact] public void NextQuestionGetsItsOwnTimer()
    {
        var clock=new ManualClock();var quiz=new GameEngine(new(),clock).Start(StudyMode.Challenge,1);
        clock.Now=clock.Now.AddSeconds(10);quiz.Submit("5");quiz.Next();Assert.Equal(30,quiz.RemainingSeconds);
    }
    [Fact] public void WrongAnswersPersistOnceWithoutDeductingCoins()
    {
        var state=new PlayerState();var engine=new GameEngine(state);
        engine.Start(StudyMode.Practice,1).Submit("0");engine.Start(StudyMode.Practice,1).Submit("1");
        Assert.Equal(PlayerState.InitialTestCoins,state.Coins);Assert.Single(state.Mistakes);Assert.Equal(2,state.Mistakes[0].WrongCount);Assert.Equal("1",state.Mistakes[0].LastAnswer);
    }
    [Fact] public void ReviewUsesVariantAndClearsOriginalOnlyWhenIndependent()
    {
        var state=new PlayerState();var engine=new GameEngine(state);engine.Start(StudyMode.Practice,1).Submit("0");
        var hinted=engine.Start(StudyMode.Review,6,"g1-1");Assert.Equal("g1-r1",hinted.Current.Id);Assert.Equal(1,hinted.Topic.Grade);
        hinted.RevealHint();hinted.Submit("6");hinted.Next();Assert.Single(state.Mistakes);
        var independent=engine.Start(StudyMode.Review,1,"g1-1");independent.Submit("6");independent.Next();
        Assert.Empty(state.Mistakes);Assert.Equal(1,state.ReviewedMistakes);
    }
    [Fact] public void IncorrectVariantPreservesSingleOriginalMistake()
    {
        var state=new PlayerState();var engine=new GameEngine(state);engine.Start(StudyMode.Practice,1).Submit("0");
        engine.Start(StudyMode.Review,1,"g1-1").Submit("1");Assert.Single(state.Mistakes);Assert.Equal("g1-1",state.Mistakes[0].QuestionId);
    }
    [Fact] public void FirstPassBonusCannotBeClaimedTwiceOrRepeatedByFinish()
    {
        var state=new PlayerState();var engine=new GameEngine(state);var session=engine.Start(StudyMode.Challenge,6);
        var first=Complete(session);var coins=state.Coins;Assert.Equal(60,first.CompletionBonus);Assert.Equal(3,first.Stars);
        Assert.Same(first,session.Finish());Assert.Equal(coins,state.Coins);
        var second=Complete(engine.Start(StudyMode.Challenge,6));Assert.Equal(0,second.CompletionBonus);Assert.True(second.QuestionPoints<first.QuestionPoints);
    }
    [Fact] public void DailyCompletionBonusIsOncePerLocalDayAcrossGrades()
    {
        var clock=new ManualClock();var engine=new GameEngine(new(),clock);
        Assert.Equal(10,Complete(engine.Start(StudyMode.Daily,1)).CompletionBonus);
        Assert.Equal(0,Complete(engine.Start(StudyMode.Daily,6)).CompletionBonus);
        clock.Now=clock.Now.AddDays(1);Assert.Equal(10,Complete(engine.Start(StudyMode.Daily,2)).CompletionBonus);
    }
    [Fact] public void SessionKeepsStartingGradeEvenIfSelectionChanges()
    {
        var state=new PlayerState();var quiz=new GameEngine(state).Start(StudyMode.Challenge,1);state.Grade=6;
        var result=Complete(quiz);Assert.Equal(1,result.Grade);Assert.Equal(20,result.CompletionBonus);
    }
    [Fact] public void PetShoppingCannotOverdrawOrBuyPermanentItemsTwice()
    {
        var state=new PlayerState{Coins=120};var engine=new GameEngine(state);
        Assert.True(engine.Buy(ItemKind.Ribbon).Success);Assert.Equal(40,state.Coins);
        Assert.False(engine.Buy(ItemKind.Ribbon).Success);Assert.False(engine.Buy(ItemKind.Toy).Success);
        Assert.Equal(40,state.Coins);Assert.True(engine.Buy(ItemKind.Food).Success);Assert.Equal(3,state.Food);
    }
    [Fact] public void FeedingDoesNotConsumeFoodAtFullnessCap()
    {
        var state=new PlayerState{Fullness=100};var engine=new GameEngine(state);engine.Feed();Assert.Equal(2,state.Food);
        state.Fullness=90;engine.Feed();Assert.Equal(100,state.Fullness);Assert.Equal(1,state.Food);Assert.Equal(22,state.Growth);
    }
    [Fact] public void LongAbsenceHasSafeFloorsAndNoGrowthLoss()
    {
        var clock=new ManualClock();var state=new PlayerState{LastSeen=clock.Now.AddDays(-60)};var engine=new GameEngine(state,clock);
        engine.ApplyAbsence();Assert.Equal(30,state.Fullness);Assert.Equal(40,state.Mood);Assert.Equal(20,state.Growth);
        engine.ApplyAbsence();Assert.Equal(30,state.Fullness);
    }
    [Fact] public void PetMaturityDoesNotRequireMakingMistakes()
    {
        var state=new PlayerState{Growth=100,CompletedPractices=2};state.Stars["addition"]=3;
        state.ActivePet.Experience=PetProgress.RequiredExperience(13);Assert.Equal("最终进化",new GameEngine(state).PetStage);
        state.ActivePet.Experience=0;Assert.Equal("幼年期",new GameEngine(state).PetStage);
    }
    [Fact] public void MerelyViewingOrUsingHintsCannotMarkMastered()
    {
        var engine=new GameEngine(new());var topic=Curriculum.ForGrade(1);engine.State.ViewedTopics.Add(topic.Id);
        Assert.Equal(Mastery.Learning,engine.GetMastery(topic));Complete(engine.Start(StudyMode.Practice,1),true);Complete(engine.Start(StudyMode.Practice,1),true);
        Assert.NotEqual(Mastery.Mastered,engine.GetMastery(topic));
    }
    [Fact] public void MasteryNeedsRepeatedIndependentPracticeAndPass()
    {
        var engine=new GameEngine(new());Complete(engine.Start(StudyMode.Practice,1));Complete(engine.Start(StudyMode.Challenge,1));
        Assert.Equal(Mastery.Mastered,engine.GetMastery(Curriculum.ForGrade(1)));
    }
    [Fact] public void SaveRoundTripPreservesStudyAndPetState()
    {
        var state=new PlayerState{Grade=5,PetName="小团",HasRibbon=true,WearingRibbon=true};
        var engine=new GameEngine(state);Complete(engine.Start(StudyMode.Challenge,5));
        var restored=StateSerialization.Deserialize(StateSerialization.Serialize(state));
        Assert.Equal(state.Coins,restored.Coins);Assert.Equal(3,restored.Stars["fraction"]);Assert.True(restored.WearingRibbon);Assert.Equal("小团",restored.PetName);
    }
    [Theory] [InlineData("null")] [InlineData("{}")] [InlineData("{\"Version\":999}")] [InlineData("{\"Grade\":7}")] [InlineData("{\"Coins\":-1}")] [InlineData("{\"Mistakes\":null}")]
    public void CorruptOrUnknownSaveIsNotSilentlyLoaded(string json)
    {
        if(json=="{}")Assert.NotNull(StateSerialization.Deserialize(json));
        else Assert.ThrowsAny<Exception>(()=>StateSerialization.Deserialize(json));
    }
}
