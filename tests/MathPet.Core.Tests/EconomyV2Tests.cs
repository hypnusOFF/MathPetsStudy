using MathPet.Core;
using Xunit;
namespace MathPet.Core.Tests;

public class EconomyV2Tests
{
    private sealed class Clock:TimeProvider
    {
        public DateTimeOffset Now=new(2026,10,12,10,0,0,TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow()=>Now;
        public override TimeZoneInfo LocalTimeZone=>TimeZoneInfo.Utc;
    }
    private static SessionSummary Complete(QuizSession session,bool hints=false)
    {
        while(!session.Completed){if(hints)session.RevealHint();session.Submit(TestAnswers.For(session.Current));session.Next();}
        return session.Finish();
    }

    [Theory] [InlineData(1)] [InlineData(3)] [InlineData(6)]
    public void LearningExperienceDependsOnAnswerHistoryAndHintsRatherThanGrade(int grade)
    {
        var state=new PlayerState();var engine=new GameEngine(state);
        var first=Complete(engine.Start(StudyMode.Practice,grade));
        Assert.Equal(18,first.GrowthGained);
        var repeat=Complete(engine.Start(StudyMode.Practice,grade));Assert.Equal(9,repeat.GrowthGained);
        var hints=Complete(engine.Start(StudyMode.Practice,grade),true);Assert.Equal(6,hints.GrowthGained);
        var fresh=new GameEngine(new PlayerState());Assert.Equal(12,Complete(fresh.Start(StudyMode.Practice,grade),true).GrowthGained);
    }
    [Fact] public void AbandonedSessionDoesNotCreditExperienceOrToyEligibility()
    {
        var state=new PlayerState();var engine=new GameEngine(state);
        var session=engine.Start(StudyMode.Practice,1);session.Submit(TestAnswers.For(session.Current));
        Assert.Equal(20,state.ActivePet.Experience);Assert.Equal(0,state.ActivePet.IndependentAnswersToday);
        Assert.Throws<InvalidOperationException>(()=>session.Finish());
    }
    [Fact] public void FoodGrowthCapSurvivesSavingAndIsSeparateForEachPetAndDay()
    {
        var clock=new Clock();var state=new PlayerState{Fullness=100,Food=8};var engine=new GameEngine(state,clock);
        for(var i=0;i<3;i++)engine.Feed();
        var restored=StateSerialization.Deserialize(StateSerialization.Serialize(state));engine=new GameEngine(restored,clock);
        Assert.Equal(0,engine.RemainingFoodGrowth);var food=restored.Food;engine.Feed();Assert.Equal(food,restored.Food);
        engine.SelectPet("rabbit");Assert.Equal(3,engine.RemainingFoodGrowth);engine.Feed();Assert.Equal(4,restored.ActivePet.Experience);
        engine.SelectPet("cat");clock.Now=clock.Now.AddDays(1);Assert.Equal(3,engine.RemainingFoodGrowth);engine.Feed();Assert.Equal(36,restored.ActivePet.Experience);
    }
    [Fact] public void FoodAndToyOwnershipCannotOverdrawOrConsumeUnknownItems()
    {
        var state=new PlayerState{Coins=80};var engine=new GameEngine(state);
        Assert.True(engine.BuyFood("rainbow-meal").Success);Assert.Equal(0,state.Coins);Assert.Equal(1,engine.FoodCount("rainbow-meal"));
        Assert.False(engine.BuyFood("star-feast").Success);Assert.False(engine.BuyToy("plush-ball").Success);
        engine.Feed("rainbow-meal");Assert.Equal(45,state.ActivePet.Experience);Assert.Equal(0,engine.FoodCount("rainbow-meal"));
        engine.Feed("rainbow-meal");Assert.Equal(45,state.ActivePet.Experience);
        Assert.Throws<ArgumentException>(()=>engine.Feed("unknown"));Assert.Throws<ArgumentException>(()=>engine.Play("treasure-box"));
        Assert.Throws<ArgumentException>(()=>engine.SelectToy("treasure-box"));
    }
    [Fact] public void ToyTrainingRequiresCompletedLearningAndCannotStackAcrossToysOrReloads()
    {
        var clock=new Clock();var state=new PlayerState();var engine=new GameEngine(state,clock);
        engine.BuyToy("plush-ball");engine.BuyToy("treasure-box");
        engine.Play("treasure-box");Assert.Equal(20,state.ActivePet.Experience);Assert.False(state.ActivePet.ToyTrainingClaimed);
        Complete(engine.Start(StudyMode.Practice,1));Assert.Equal(3,state.ActivePet.IndependentAnswersToday);
        engine.Play("treasure-box");Assert.Equal(56,state.ActivePet.Experience);Assert.True(state.ActivePet.ToyTrainingClaimed);
        engine.Play("plush-ball");Assert.Equal(56,state.ActivePet.Experience);
        var restored=StateSerialization.Deserialize(StateSerialization.Serialize(state));engine=new GameEngine(restored,clock);engine.Play("treasure-box");Assert.Equal(56,restored.ActivePet.Experience);
        clock.Now=clock.Now.AddDays(1);engine.Play("treasure-box");Assert.Equal(56,restored.ActivePet.Experience);Assert.False(restored.ActivePet.ToyTrainingClaimed);
        Complete(engine.Start(StudyMode.Practice,1));engine.Play("plush-ball");Assert.Equal(68,restored.ActivePet.Experience);
    }
    [Fact] public void HintedLearningDoesNotUnlockTrainingAndRewardPetKeepsCredit()
    {
        var state=new PlayerState();var engine=new GameEngine(state);
        Complete(engine.Start(StudyMode.Practice,1),true);Assert.Equal(0,state.ActivePet.IndependentAnswersToday);
        var session=engine.Start(StudyMode.Practice,1);engine.SelectPet("rabbit");Complete(session);
        Assert.Equal(3,state.Pets[0].IndependentAnswersToday);Assert.Equal(0,state.ActivePet.IndependentAnswersToday);
    }
    [Fact] public void EachLegacyLevelAndItsProgressAndEvolutionArePreservedWithoutRepeatConversion()
    {
        for(var level=1;level<=20;level++)
        {
            var old=new PlayerState{Version=3,Coins=123,Food=7,HasToy=true};
            var start=PetProgress.LegacyRequiredExperience(level);
            var offset=level==20?0:(PetProgress.LegacyRequiredExperience(level+1)-start)*2/3;
            old.Pets[0].Experience=start+offset;
            var migrated=StateSerialization.Deserialize(StateSerialization.Serialize(old));
            Assert.Equal(level,migrated.ActivePet.Level);Assert.Equal(123,migrated.Coins);Assert.Equal(7,migrated.Food);Assert.True(migrated.HasToy);
            var target=level==20?6555:PetProgress.RequiredExperience(level)+(PetProgress.RequiredExperience(level+1)-PetProgress.RequiredExperience(level))*offset/(PetProgress.LegacyRequiredExperience(level+1)-start);
            Assert.Equal(target,migrated.ActivePet.Experience);
            Assert.Equal(target,StateSerialization.Deserialize(StateSerialization.Serialize(migrated)).ActivePet.Experience);
        }
    }
    [Fact] public void PaidPetsAreIndependentAndSavedWithoutChangingTheActivePet()
    {
        var state=new PlayerState{Coins=1200};var engine=new GameEngine(state);
        Assert.True(engine.UnlockPet("dog").Success);Assert.Equal(0,state.Coins);Assert.Equal("cat",state.ActiveSpecies);
        Assert.False(engine.UnlockPet("dog").Success);Assert.False(engine.UnlockPet("redpanda").Success);
        engine.SelectPet("dog");Assert.Equal(0,state.ActivePet.Experience);engine.Feed();Assert.Equal(4,state.ActivePet.Experience);Assert.Equal(20,state.Pets[0].Experience);
        var restored=StateSerialization.Deserialize(StateSerialization.Serialize(state));Assert.Equal("dog",restored.ActiveSpecies);Assert.Equal(4,restored.ActivePet.Experience);Assert.Equal(4,restored.Pets.Count);
    }
    [Fact] public void NewThemesAndTheirItemsMatchTheApprovedBudgets()
    {
        Assert.Equal(6,RoomCatalog.Themes.Count);Assert.Equal(30,RoomCatalog.Decorations.Count);
        foreach(var pair in new[]{("forest",2600),("ocean",3300),("autumn",3900)})
            Assert.Equal(pair.Item2,RoomCatalog.Themes.Single(r=>r.Id==pair.Item1).Price+RoomCatalog.Decorations.Where(d=>d.Theme==pair.Item1).Sum(d=>d.Price));
        Assert.Equal(360,PetProgress.RequiredExperience(6));Assert.Equal(2110,PetProgress.RequiredExperience(13));Assert.Equal(6555,PetProgress.RequiredExperience(20));
    }
    [Theory] [InlineData("\"FoodInventory\":{\"unknown\":1}")] [InlineData("\"FoodInventory\":{\"cookie\":-1}")]
    [InlineData("\"OwnedToys\":[\"unknown\"]")]
    public void InvalidCultivationInventoriesAreRejected(string field)
        =>Assert.Throws<InvalidDataException>(()=>StateSerialization.Deserialize("{\"Version\":4,"+field+"}"));
}
