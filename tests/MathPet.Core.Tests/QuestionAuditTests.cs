using MathPet.Core;
using System.Globalization;
using Xunit;
namespace MathPet.Core.Tests;

public sealed class QuestionAuditTests
{
    [Fact]
    public void VisualBankHasFourTypesAndDisjointPairedReviews()
    {
        var topics=VisualExercises.Create();
        Assert.Equal(12,topics.Count);
        foreach(var t in topics)
        {
            Assert.Equal(12,t.Questions.Count); Assert.Equal(12,t.ReviewQuestions.Count);
            Assert.True(t.Questions.Select(q=>q.QuestionType).Distinct().Count()>=4,t.Id);
            Assert.Equal(4,t.Questions.Select(q=>q.TemplateId).Distinct().Count());
            var originals=t.Questions.Select(q=>(q.Text,string.Join(",",q.Diagram))).ToHashSet();
            for(var i=0;i<t.Questions.Count;i++)
            {
                Assert.Equal(t.Questions[i].TemplateId,t.ReviewQuestions[i].TemplateId);
                Assert.Equal(t.Questions[i].QuestionType,t.ReviewQuestions[i].QuestionType);
                Assert.DoesNotContain((t.ReviewQuestions[i].Text,string.Join(",",t.ReviewQuestions[i].Diagram)),originals);
            }
            for(var i=0;i<3;i++)
            { Assert.NotNull(Curriculum.FindQuestion($"{t.Id}-q{i}")); Assert.NotNull(Curriculum.FindQuestion($"{t.Id}-r{i}")); }
        }
    }

    [Fact]
    public void VisualAnswersMatchIndependentDiagramCalculations()
    {
        foreach(var q in VisualExercises.Create().SelectMany(t=>t.Questions.Concat(t.ReviewQuestions)))
        {
            var d=q.Diagram;
            var task=q.DiagramTask==""?0:q.DiagramTask=="rectangles"?1:int.Parse(q.DiagramTask[5..]);
            var variant=task==0?0:int.Parse(q.Id[^1..]);
            decimal expected=(q.Interaction,task) switch
            {
                ("classify",0)=>d.Count(x=>x==3), ("classify",1)=>d.Count(x=>x==4),
                ("classify",2)=>Math.Abs(d.Count(x=>x==3)-d.Count(x=>x==4)), ("classify",3)=>d.Length-d.Count(x=>x==3),
                ("clock",0)=>d[0]*100+d[1], ("clock",1)=>60-d[1], ("clock",2)=>d[1]/5,("clock",3)=>60*d[0]+d[1],
                ("bars",0)=>d.Max(),("bars",1)=>d.Sum(),("bars",2)=>d.Max()-d.Min(),("bars",3)=>variant+4,
                ("angle",0)=>d[0],("angle",1)=>180-d[0],("angle",2)=>d[0]<90?0:d[0]==90?1:2,("angle",3)=>0,
                ("reflect",0)=>(7-d[0])*10+d[1],("reflect",1)=>7-d[0],("reflect",2)=>7-2*d[0],("reflect",3)=>0,
                ("rotate",0)=>d[0],("rotate",1)=>d[0]/90,("rotate",2)=>360-d[0],("rotate",3)=>0,
                ("pie",0)=>100-d[1],("pie",1)=>(100-d[1])*(variant+1),
                ("pie",2)=>d[0]*(variant+2)/(d[0]/100m),("pie",3)=>Math.Abs(d[1]-d[0]),
                ("ring",0)=>3.14m*d[0]*d[0]-3.14m*d[1]*d[1],("ring",1)=>d[0]-d[1],("ring",2)=>d[0]*2,("ring",3)=>0,
                ("proportion",0)=>d[0],("proportion",1)=>d[1],("proportion",2)=>d[0]==1?2:0.5m,("proportion",3)=>d[0]==1?0:1,
                ("direction",0)=>d[0],("direction",1)=>d[1],("direction",2)=>90-d[0],("direction",3)=>d[1]*2,
                ("views",0)=>d.Max(),("views",1)=>d.Sum(),("views",2)=>d.Max()-d.Min(),("views",3)=>d.Sum(x=>d.Max()-x),
                ("practice",0)=>d[0]+d[1]+d[0]+d[1],("practice",1)=>d[0]*d[1],
                ("practice",2)=>2*d[0]+2*d[1]-1,("practice",3)=>d[1],
                _=>throw new InvalidOperationException(q.Id)
            };
            Assert.Equal(expected,decimal.Parse(q.Answer,CultureInfo.InvariantCulture));
            Assert.True(AnswerChecker.IsCorrect(q,TestAnswers.For(q)),q.Id);
            Assert.False(AnswerChecker.IsCorrect(q,"-999"),q.Id);
            if(q.Interaction=="ring") Assert.True(d[0]>d[1]&&d[1]>0);
            if(q.Interaction=="pie") Assert.Equal(100,d.Sum());
            if(q.Interaction=="reflect") {Assert.InRange(d[0],0,3);Assert.InRange(d[1],0,5);}
        }
        // Opt-in export lets the browser checks submit the actual compiled bank.
        var exportPath=Environment.GetEnvironmentVariable("MATHPET_AUDIT_EXPORT");
        if(!string.IsNullOrEmpty(exportPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(exportPath))!);
            File.WriteAllText(exportPath,System.Text.Json.JsonSerializer.Serialize(VisualExercises.Create()));
        }
    }

    [Fact]
    public void ClassificationCannotPassUsingOnlyCountOrDuplicateSelections()
    {
        var topic=Curriculum.FindTopic("visual-classify");
        var triangle=topic.Questions[0]; var rectangle=topic.Questions.Single(q=>q.Id=="visual-classify-q-t1-v0");
        Assert.False(AnswerChecker.IsCorrect(triangle,"3"));
        Assert.False(AnswerChecker.IsCorrect(triangle,"选中：1、3、3"));
        Assert.True(AnswerChecker.IsCorrect(rectangle,"选中：2、4"));
        Assert.False(AnswerChecker.IsCorrect(rectangle,"选中：1、2"));
        Assert.True(AnswerChecker.IsCorrect(topic.Questions.Single(q=>q.Id=="visual-classify-q-t2-v0"),"1"));
    }

    [Fact]
    public void SimplestFractionRequiresReducedFormButOrdinaryFractionAllowsEquivalence()
    {
        var q=Curriculum.Topics.SelectMany(t=>t.Questions).First(q=>q.Text.Contains("最简分数")&&q.Answer=="3/4");
        Assert.True(AnswerChecker.IsCorrect(q,"3/4"));Assert.False(AnswerChecker.IsCorrect(q,"6/8"));
        Assert.False(AnswerChecker.IsCorrect(q,"0.75"));
        Assert.True(AnswerChecker.IsCorrect(q with {Text="计算结果是多少？"},"6/8"));
        var whole=q with {Answer="2"};Assert.True(AnswerChecker.IsCorrect(whole,"2"));Assert.False(AnswerChecker.IsCorrect(whole,"4/2"));
    }

    [Fact]
    public void FullWidthClockInputWorksWithoutChangingClassificationTokens()
    {
        var q=Curriculum.FindTopic("visual-clock").Questions[0];Assert.True(AnswerChecker.IsCorrect(q,"３：３０"));
        Assert.False(AnswerChecker.IsCorrect(q,"3:75"));
        Assert.True(AnswerChecker.IsCorrect(Curriculum.FindTopic("visual-classify").Questions[0],"选中：１、３、５"));
    }

    [Fact]
    public void ReviewSolutionsDisplayTimesAndCoordinatesRatherThanInternalCodes()
    {
        var clock=Curriculum.FindTopic("visual-clock").ReviewQuestions[0];
        Assert.Contains("4:35",clock.Solution[^1]);Assert.DoesNotContain("435",clock.Solution[^1]);
        var reflect=Curriculum.FindTopic("visual-reflect").ReviewQuestions[0];
        Assert.Contains("（6，4）",reflect.Solution[^1]);Assert.DoesNotContain("64",reflect.Solution[^1]);
    }

    [Fact]
    public void RetiredParametersKeepReviewableMistakesAndUnchangedPetRewards()
    {
        const string retired="pep-g1-s1-008-q26";
        Assert.Null(Curriculum.FindQuestion(retired));
        var original=new PlayerState {Coins=321};
        original.Mistakes.Add(new(){QuestionId=retired,WrongCount=2,LastAnswer="8"});
        var replacement=Curriculum.FindTopic("pep-g1-s1-008").Questions.First(q=>q.TemplateId=="pep-g1-s1-008-t1");
        original.Mistakes.Add(new(){QuestionId=replacement.Id,WrongCount=1,LastAnswer="9"});
        var restored=StateSerialization.Deserialize(StateSerialization.Serialize(original));
        Assert.Equal(321,restored.Coins);Assert.Equal(original.Pets[0].Experience,restored.Pets[0].Experience);
        var mistake=Assert.Single(restored.Mistakes);Assert.Equal(replacement.Id,mistake.QuestionId);Assert.Equal(3,mistake.WrongCount);
        var quiz=new GameEngine(restored).Start(StudyMode.Review,1,mistake.QuestionId);
        Assert.Equal(replacement.TemplateId,quiz.Current.TemplateId);
        quiz.Submit(TestAnswers.For(quiz.Current));quiz.Next();quiz.Finish();Assert.Empty(restored.Mistakes);
    }

    [Fact]
    public void VisualMistakesReviewSameTypeWithNewDiagramAndCanBeCleared()
    {
        foreach(var topic in VisualExercises.Create())
        {
            var state=new PlayerState();var engine=new GameEngine(state);
            var quiz=engine.Start(StudyMode.Practice,topic.Grade,topicId:topic.Id);
            Assert.Equal(4,quiz.Questions.Count);
            var wrong=quiz.Current.Interaction=="classify"&&!quiz.Current.DiagramTask.StartsWith("read-")?"选中："
                :quiz.Current.Options.Length>0?((int.Parse(quiz.Current.Answer)+1)%quiz.Current.Options.Length).ToString():"-999";
            Assert.False(quiz.Submit(wrong).Correct);
            var review=engine.Start(StudyMode.Review,1,quiz.Current.Id);
            Assert.Equal(quiz.Current.TemplateId,review.Current.TemplateId);
            Assert.NotEqual(string.Join(",",quiz.Current.Diagram),string.Join(",",review.Current.Diagram));
            review.Submit(TestAnswers.For(review.Current));review.Next();Assert.Equal(1,review.Finish().Correct);Assert.Empty(state.Mistakes);
        }
    }
}
