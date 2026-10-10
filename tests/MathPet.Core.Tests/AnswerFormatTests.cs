using MathPet.Core;
using Xunit;
namespace MathPet.Core.Tests;

public sealed class AnswerFormatTests
{
    private static Question Number=>Curriculum.FindQuestion("pep-g4-s1-058-q01")!;
    [Theory]
    [InlineData("20000")][InlineData("2万")][InlineData("２ 万")][InlineData("2.0万")]
    [InlineData("0.0002亿")][InlineData("20000.000")]
    public void PlaceValueAcceptsEquivalentMagnitudeNotation(string input)
    {
        var check=AnswerChecker.Check(Number,input);
        Assert.True(check.Correct);Assert.Null(check.RetryMessage);
    }
    [Theory]
    [InlineData("2亿","200000000")][InlineData("0.2亿","20000000")]
    [InlineData("1.25万","12500")][InlineData("-0.5万","-5000")]
    public void MagnitudesAreExact(string input,string expected)
        =>Assert.True(AnswerChecker.IsCorrect(Number with {Answer=expected},input));
    [Theory]
    [InlineData("2")][InlineData("2亿")][InlineData("2.01万")]
    public void RecognizedWrongValuesStillCountAsWrong(string input)
    {
        var check=AnswerChecker.Check(Number,input);
        Assert.False(check.Correct);Assert.Null(check.RetryMessage);
    }
    [Theory]
    [InlineData("两万")][InlineData("2万万")][InlineData("1/0")][InlineData("hello")]
    public void UnrecognizedInputDoesNotMutateSessionOrProgress(string input)
    {
        var state=new PlayerState();var quiz=new GameEngine(state).Start(StudyMode.Practice,4,topicId:Number.TopicId);
        Assert.Equal(Number.Id,quiz.Current.Id);
        quiz.RevealHint();var coins=state.Coins;
        Assert.Throws<ArgumentException>(()=>quiz.Submit(input));
        Assert.False(quiz.Answered);Assert.Equal(0,quiz.Index);Assert.Null(quiz.LastResult);
        Assert.Equal(1,quiz.HintCount);Assert.Equal(coins,state.Coins);
        Assert.Empty(state.Progress);Assert.Empty(state.Mistakes);
        Assert.True(quiz.Submit("2万").Correct);
        Assert.Equal(1,state.Progress[Number.Id].AttemptCount);
    }
    [Theory]
    [InlineData("6/8")][InlineData("0.75")][InlineData("75/100")]
    public void CorrectValueInWrongFractionFormCanBeRetried(string input)
    {
        var q=Number with {Text="请填写最简分数",Answer="3/4"};
        var check=AnswerChecker.Check(q,input);
        Assert.False(check.Correct);Assert.Contains("数值是对的",check.RetryMessage);
        Assert.True(AnswerChecker.IsCorrect(q,"3/4"));
        Assert.Null(AnswerChecker.Check(q,"1/2").RetryMessage);
    }
    [Fact]
    public void ScalingBeforeAValidUnitAndMismatchedUnitsStayDistinct()
    {
        Assert.True(AnswerChecker.IsCorrect(Number with {Unit="元"},"2万元"));
        var q=Number with {Answer="24",Unit="厘米"};
        Assert.False(AnswerChecker.Check(q,"24平方厘米").Correct);
        Assert.Null(AnswerChecker.Check(q,"24平方厘米").RetryMessage);
    }
    [Fact]
    public void WrongNumericAnswerStillCreatesMistake()
    {
        var state=new PlayerState();var quiz=new GameEngine(state).Start(StudyMode.Practice,4,topicId:Number.TopicId);
        Assert.False(quiz.Submit("3万").Correct);
        Assert.True(quiz.Answered);Assert.Single(state.Mistakes);
        Assert.Equal(1,state.Progress[Number.Id].AttemptCount);
    }
}
