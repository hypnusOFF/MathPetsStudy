using MathPet.Core;
using System.Text.Json;

// Read-only numerical proposal. The production GameEngine remains unchanged.
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
    curriculum.Add(new { grade, oldIncome=engine.State.Coins-old, proposedIncome=total, practice, challenge, comprehensive });
}

// One real daily session per day, alternate semesters, exact bank rotation and repeat discounts.
// No spending outside the selected pet strategy. Toy XP needs >=3 independent correct answers.
var simulations=new List<object>();
foreach(var grade in new[]{1,3,6})foreach(var strategy in new[]{"learning-only","balanced","growth-focused"})
{
    var clock=new SimulationClock();var game=new GameEngine(new PlayerState{Coins=0},clock);
    var coins=120;var experience=20;var foodCost=0;var toyCost=0;var toyOwned=false;
    var toyPrice=strategy=="balanced"?100:1500;var toyXp=strategy=="balanced"?3:18;
    var snackPrice=strategy=="balanced"?40:160;var snackXp=strategy=="balanced"?10:60;
    int? firstEvolution=null,finalEvolution=null,maxLevel=null;
    var earned=0;var rounds=0;var repeatedQuestions=0;
    for(var day=1;day<=730;day++)
    {
        clock.Now=clock.Now.AddDays(1);
        var semester=day%2==1?Semester.First:Semester.Second;
        var session=game.StartDaily(grade,semester);
        repeatedQuestions+=session.Questions.Count(q=>game.State.Progress.GetValueOrDefault(q.Id)?.CorrectCount>0);
        var (points,learningExperience,original)=Finish(game,session);
        if(original.CompletionBonus!=10)throw new InvalidOperationException("Daily date simulation failed");
        var income=points+10;coins+=income;earned+=income;rounds++;
        experience+=learningExperience;
        if(strategy!="learning-only")
        {
            if(!toyOwned && coins>=toyPrice){coins-=toyPrice;toyCost+=toyPrice;toyOwned=true;}
            if(toyOwned && session.Questions.Count>=3)experience+=toyXp;
            // One snack per day, only after acquiring the selected toy and when affordable.
            if(toyOwned && coins>=snackPrice){coins-=snackPrice;foodCost+=snackPrice;experience+=snackXp;}
        }
        experience=Math.Min(Threshold(20),experience);
        if(experience>=Threshold(6))firstEvolution??=day;
        if(experience>=Threshold(13))finalEvolution??=day;
        if(experience>=Threshold(20)){maxLevel=day;break;}
    }
    simulations.Add(new{grade,strategy,firstEvolutionDay=firstEvolution,finalEvolutionDay=finalEvolution,maxLevelDay=maxLevel,
        rounds,earned,foodCost,toyCost,remainingCoins=coins,repeatedQuestions});
}
var output=new{
    proposal=true, multipliers,
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
