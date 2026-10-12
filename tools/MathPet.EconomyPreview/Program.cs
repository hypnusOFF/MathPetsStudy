using MathPet.Core;
using System.Text.Json;

// Read-only simulation of the approved economy, checked against the production rules.
var oldIncomeByGrade=new[]{3315,4554,6945,7981,12597,13178}; // Verified pre-v4 baseline.
var multipliers = new decimal[] { 1m, 1.15m, 1.3m, 1.5m, 1.75m, 2m };
int Round(decimal value) => (int)Math.Round(value, MidpointRounding.AwayFromZero);
int Threshold(int level) { var n = level - 1; return 35*n + 5*n*(n+1)/2 + n*(n+1)*(2*n+1)/3; }
string Answer(Question q) => q.Interaction == "classify" && !q.DiagramTask.StartsWith("read-")
    ? "选中：" + string.Join("、", q.Diagram.Select((x,i)=>(x,i)).Where(p=>p.x==(q.DiagramTask=="rectangles"?4:3)).Select(p=>p.i+1)) : q.Answer;
(int Points, int LearningExperience, SessionSummary Original) Finish(GameEngine engine, QuizSession session)
{
    var points = 0; var learningExperience = 0;
    while (!session.Completed)
    {
        var q=session.Current;
        var repeated=engine.State.Progress.GetValueOrDefault(q.Id)?.CorrectCount>0;
        learningExperience += session.HintCount>0 ? (repeated?2:4) : (repeated?3:6);
        points += Math.Max(1,Round(10*multipliers[q.Grade-1]*q.Difficulty*(repeated?0.3m:1m)))
            + (session.IsChallenge && !session.TimedOut && !repeated ? 2 : 0);
        if (!session.Submit(Answer(q)).Correct) throw new InvalidOperationException("Invalid simulation answer: "+q.Id);
        session.Next();
    }
    return (points,learningExperience,session.Finish());
}

var engine=new GameEngine(new PlayerState { Coins=0 });
var curriculum=new List<object>();
for(var grade=1;grade<=6;grade++)
{
    var old=engine.State.Coins;var practice=0;var challenge=0;var comprehensive=0;
    foreach(var semester in new[]{Semester.First,Semester.Second})
    {
        var topics=Curriculum.ForSemester(grade,semester);
        foreach(var topic in topics)
        {
            practice+=Finish(engine,engine.Start(StudyMode.Practice,grade,topicId:topic.Id)).Points;
            var result=Finish(engine,engine.Start(StudyMode.Challenge,grade,topicId:topic.Id));
            if(!result.Original.Passed)throw new InvalidOperationException("Challenge did not pass");
            challenge+=result.Points+(int)(20*multipliers[grade-1]);
        }
        foreach(var unit in topics.Where(t=>t.Order!=1000).Select(t=>t.UnitName).Distinct())
        {
            var result=Finish(engine,engine.StartComprehensive(grade,semester,unit));
            if(!result.Original.Passed)throw new InvalidOperationException("Unit did not pass");
            comprehensive+=result.Points+(int)(20*multipliers[grade-1]);
        }
        var term=Finish(engine,engine.StartComprehensive(grade,semester));
        if(!term.Original.Passed)throw new InvalidOperationException("Semester did not pass");
        comprehensive+=term.Points+(int)(20*multipliers[grade-1]);
    }
    var total=practice+challenge+comprehensive;
    if(engine.State.Coins-old!=total)throw new InvalidOperationException("Implemented income differs from proposal");
    curriculum.Add(new { grade, oldIncome=oldIncomeByGrade[grade-1], implementedIncome=engine.State.Coins-old, proposedIncome=total, practice, challenge, comprehensive });
}

// One real daily session per day, alternate semesters, exact bank rotation and repeat discounts.
// No spending outside the selected pet strategy. Toy XP needs >=3 independent correct answers.
var simulations=new List<object>();
foreach(var grade in new[]{1,3,6})foreach(var strategy in new[]{"learning-only","balanced","growth-focused"})
{
    var clock=new SimulationClock();var game=new GameEngine(new PlayerState{Coins=120,LastSeen=clock.Now},clock);
    var foodCost=0;var toyCost=0;var toyOwned=false;
    var toyId=strategy=="balanced"?"plush-ball":"treasure-box";
    var snackId=strategy=="balanced"?"cookie":"star-feast";
    var toyPrice=PetCatalog.Toys.Single(t=>t.Id==toyId).Price;
    var snackPrice=PetCatalog.Foods.Single(f=>f.Id==snackId).Price;
    int? firstEvolution=null,finalEvolution=null,maxLevel=null;
    var earned=0;var rounds=0;var repeatedQuestions=0;
    for(var day=1;day<=730;day++)
    {
        clock.Now=clock.Now.AddDays(1);game.ApplyAbsence();
        var semester=day%2==1?Semester.First:Semester.Second;
        var session=game.StartDaily(grade,semester);
        repeatedQuestions+=session.Questions.Count(q=>game.State.Progress.GetValueOrDefault(q.Id)?.CorrectCount>0);
        var (points,learningExperience,original)=Finish(game,session);
        if(original.CompletionBonus!=10||original.QuestionPoints!=points)throw new InvalidOperationException("Daily simulation differs from implementation");
        earned+=points+10;rounds++;
        if(strategy!="learning-only")
        {
            if(!toyOwned && game.State.Coins>=toyPrice)
            {
                if(!game.BuyToy(toyId).Success)throw new InvalidOperationException("Toy purchase failed");
                toyCost+=toyPrice;toyOwned=true;
            }
            if(toyOwned)game.Play(toyId);
            if(toyOwned && game.State.Coins>=snackPrice && game.State.ActivePet.Experience<PetProgress.MaxExperience)
            {
                if(!game.BuyFood(snackId).Success)throw new InvalidOperationException("Food purchase failed");
                foodCost+=snackPrice;game.Feed(snackId);
            }
        }
        var experience=game.State.ActivePet.Experience;
        if(experience>=Threshold(6))firstEvolution??=day;
        if(experience>=Threshold(13))finalEvolution??=day;
        if(experience>=Threshold(20)){maxLevel=day;break;}
    }
    simulations.Add(new{grade,strategy,firstEvolutionDay=firstEvolution,finalEvolutionDay=finalEvolution,maxLevelDay=maxLevel,
        rounds,earned,foodCost,toyCost,remainingCoins=game.State.Coins,repeatedQuestions});

}
var output=new{
    proposal=false, multipliers,
    levels=Enumerable.Range(1,20).Select(level=>new{level,cumulativeExperience=Threshold(level),nextLevelExperience=level<20?Threshold(level+1)-Threshold(level):(int?)null}),
    curriculum,simulations
};
Console.WriteLine(JsonSerializer.Serialize(output,new JsonSerializerOptions{WriteIndented=true}));

sealed class SimulationClock:TimeProvider
{
    public DateTimeOffset Now{get;set;}=new(2026,1,1,0,0,0,TimeSpan.Zero);
    public override DateTimeOffset GetUtcNow()=>Now;
    public override TimeZoneInfo LocalTimeZone=>TimeZoneInfo.Utc;
}
