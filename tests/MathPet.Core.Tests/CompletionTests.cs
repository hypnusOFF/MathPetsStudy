using MathPet.Core;
using Xunit;
namespace MathPet.Core.Tests;
public sealed class CompletionTests
{
 [Fact] public void PetsKeepIndependentCareAndSessionRewards()
 {
  var state=new PlayerState();var engine=new GameEngine(state);var quiz=engine.Start(StudyMode.Practice,1,topicId:"addition");
  engine.SelectPet("rabbit");engine.Feed();var rabbit=state.ActivePet.Experience;
  while(!quiz.Completed){quiz.Submit(TestAnswers.For(quiz.Current));quiz.Next();}var result=quiz.Finish();
  Assert.Equal("cat",quiz.RewardSpecies);Assert.Equal(rabbit,state.ActivePet.Experience);Assert.Equal(20+result.GrowthGained,state.Pets[0].Experience);Assert.Equal(65,state.Pets[0].Fullness);Assert.Equal(85,state.ActivePet.Fullness);
 }
 [Fact] public void WardrobeHasOwnershipSlotAndIndependentPetChecks()
 {
  var state=new PlayerState();var engine=new GameEngine(state);state.Coins=1000;
  Assert.True(engine.BuyWearable("bow").Success);var coins=state.Coins;Assert.False(engine.BuyWearable("bow").Success);Assert.Equal(coins,state.Coins);
  engine.Equip("head","bow");Assert.Equal("bow",state.ActivePet.Headwear);engine.SelectPet("rabbit");Assert.Equal("",state.ActivePet.Headwear);engine.Equip("head","bow");engine.Equip("head","");Assert.Equal("bow",state.Pets[0].Headwear);
  Assert.Throws<ArgumentException>(()=>engine.Equip("body","bow"));Assert.Throws<ArgumentException>(()=>engine.Equip("accessory","bell"));
 }
 [Fact] public void MigrationPreservesOldEvolutionAndLearningProgress()
 {
  var state=StateSerialization.Deserialize("{\"Version\":1,\"Growth\":100,\"CompletedPractices\":2,\"PetName\":\"小团\",\"Coins\":999,\"HasRibbon\":true,\"WearingRibbon\":true,\"Stars\":{\"addition\":3}}");
  Assert.Equal(4,state.Version);Assert.Equal(13,state.ActivePet.Level);Assert.Equal("mature",state.ActivePet.Stage);Assert.Equal("小团",state.PetName);Assert.Equal(999,state.Coins);Assert.Equal("bow",state.ActivePet.Headwear);Assert.Equal(0,state.Pets[1].Experience);
  var restored=StateSerialization.Deserialize(StateSerialization.Serialize(state));Assert.Equal(state.ActivePet.Experience,restored.ActivePet.Experience);Assert.Equal(3,restored.Stars["addition"]);
 }
 [Fact] public void TwentyLevelsHaveStableEvolutionThresholds()
 {
  var pet=new PetProgress();foreach(var level in Enumerable.Range(1,20)){pet.Experience=PetProgress.RequiredExperience(level);Assert.Equal(level,pet.Level);Assert.Equal(level<6?"baby":level<13?"young":"mature",pet.Stage);}Assert.Equal(PetProgress.MaxExperience,pet.Experience);
 }
 [Fact] public void ComprehensiveScoresHaveIndependentKeys()
 {
  var state=new PlayerState();var engine=new GameEngine(state);var unit=Curriculum.ForSemester(1,Semester.First).First(t=>t.Order<1000).UnitName;
  foreach(var quiz in new[]{engine.StartComprehensive(1,Semester.First,unit),engine.StartComprehensive(1,Semester.First)}){
   Assert.InRange(quiz.Questions.Count,3,12);Assert.True(quiz.IsChallenge);while(!quiz.Completed){quiz.Submit(TestAnswers.For(quiz.Current));quiz.Next();}Assert.True(quiz.Finish().Passed);
  }
  Assert.Equal(2,state.Stars.Count);Assert.All(state.Stars.Keys,k=>Assert.StartsWith("comprehensive:",k));
 }
 [Fact] public void EveryVisualExerciseAndReviewHasConsistentAnswerAndDistinctParameters()
 {
  foreach(var topic in VisualExercises.Create())for(var i=0;i<topic.Questions.Count;i++){
   var q=topic.Questions[i];var review=topic.ReviewQuestions[i];Assert.NotEmpty(q.Diagram);Assert.NotEqual(string.Join(",",q.Diagram),string.Join(",",review.Diagram));Assert.True(AnswerChecker.IsCorrect(q,TestAnswers.For(q)));Assert.False(AnswerChecker.IsCorrect(q,"-999"));Assert.True(q.Solution.Length>=3);
  }
  var reflection=VisualExercises.Create().Single(t=>t.Id=="visual-reflect").Questions[0];Assert.True(AnswerChecker.IsCorrect(reflection,"(6,2)"));Assert.False(AnswerChecker.IsCorrect(reflection,"(6,3)"));
  var clock=VisualExercises.Create().Single(t=>t.Id=="visual-clock").Questions[0];Assert.True(AnswerChecker.IsCorrect(clock,"3:30"));Assert.False(AnswerChecker.IsCorrect(clock,"3:75"));
  var classify=VisualExercises.Create().Single(t=>t.Id=="visual-classify").Questions[0];Assert.True(AnswerChecker.IsCorrect(classify,"选中：1、3、5"));Assert.False(AnswerChecker.IsCorrect(classify,"选中：1、2、3"));
 }
}
