using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;
namespace MathPet.Core;

public readonly record struct AnswerCheckResult(bool Correct,string? RetryMessage=null);

public static partial class AnswerChecker
{
    // Exact rational arithmetic avoids approximate decimal answers being accepted for fractions.
    public static bool IsCorrect(Question question,string input)=>Check(question,input).Correct;
    public static AnswerCheckResult Check(Question question, string input)
    {
        var text = Normalize(input);
        if(question.Interaction=="classify" && !question.DiagramTask.StartsWith("read-"))
        {
            if(!text.StartsWith("选中:"))return new(false,"请在图中选择符合条件的全部图形。");
            var selected=text[3..].Split('、',StringSplitOptions.RemoveEmptyEntries);
            if(selected.Any(x=>!int.TryParse(x,out _)))return new(false,"请在图中选择图形。");
            return new(selected.Select(int.Parse).Order().SequenceEqual(question.Diagram.Select((x,i)=>(x,i)).Where(p=>p.x==(question.DiagramTask=="rectangles"?4:3)).Select(p=>p.i+1)));
        }
        if(question.Interaction=="reflect" && question.DiagramTask=="" && (text.Contains(',')||text.Contains('，')))
        {
            var coordinates=text.Trim('(',')','（','）').Replace('，',',').Split(',');
            if(coordinates.Length!=2||!int.TryParse(coordinates[0],out var x)||!int.TryParse(coordinates[1],out var y)||x is <0 or >7||y is <0 or >5)return new(false,"请点击网格交点，或按(6,2)这样的格式输入有效坐标。");
            text=(x*10+y).ToString(CultureInfo.InvariantCulture);
        }
        if(question.Interaction=="clock" && question.DiagramTask=="" && text.Contains(':'))
        {
            var parts=text.Split(':');
            if(parts.Length!=2||!int.TryParse(parts[0],out var hour)||!int.TryParse(parts[1],out var minute)||hour is <1 or >12||minute is <0 or >59)return new(false,"请按3:30这样的格式输入时刻，小时为1～12，分钟为0～59。");
            text=(hour*100+minute).ToString(CultureInfo.InvariantCulture);
        }
        var originalText=text;
        if (question.Unit is { } unit && text.EndsWith(unit, StringComparison.Ordinal))
            text = text[..^unit.Length];
        if(!TryRational(text,out var numerator,out var denominator))
        {
            // A recognized but mismatched physical unit is a mathematical error.
            string[] units=["平方千米","平方厘米","平方分米","平方米","立方厘米","立方分米","千米","厘米","毫米","千克","分钟","毫升","公顷","元","角","分","人","颗","米","克","秒","度","天","格","段","吨","升"];
            if(units.Any(u=>originalText.EndsWith(u,StringComparison.Ordinal)&&TryRational(originalText[..^u.Length],out _,out _)))
                return new(false);
            return new(false,"暂时无法识别这个写法。请用数字、小数或分数填写，例如20000、2万、0.5、1/2，再试一次；本次不会记为错题。");
        }
        if(question.Options.Length>0&&(numerator%denominator!=0||numerator/denominator<0||numerator/denominator>=question.Options.Length))
            return new(false,"请点击一个选项，或填写对应的选项编号。");
        if(!TryRational(question.Answer,out var expectedNumerator,out var expectedDenominator))
            throw new InvalidOperationException("题目标准答案格式不正确。");
        var correct=numerator*expectedDenominator==expectedNumerator*denominator;
        if(correct&&question.Text.Contains("最简分数")
            &&(!FractionPattern().IsMatch(text)&&!IntegerPattern().IsMatch(text)
                ||BigInteger.GreatestCommonDivisor(BigInteger.Abs(numerator),denominator)!=1))
            return new(false,"数值是对的！这题要求最简分数，请约分后用分数填写；结果是整数时可直接填整数。本次不会记为错题。");
        return new(correct);
    }
    private static string Normalize(string input)
    {
        if (input.Length > 100) return "";
        return string.Concat(input.Trim().Select(c => c switch
        {
            >= '０' and <= '９' => (char)(c - '０' + '0'),
            '：' => ':', '．' => '.', '／' => '/', '−' or '－' => '-', '＋' => '+',
            _ => c
        }).Where(c => !char.IsWhiteSpace(c)));
    }
    private static bool TryRational(string text, out BigInteger numerator, out BigInteger denominator)
    {
        numerator = BigInteger.Zero; denominator = BigInteger.One;
        if(text.EndsWith('万')||text.EndsWith('亿'))
        {
            var factor=text[^1]=='万'?10000:100000000;
            var number=text[..^1];
            // One magnitude suffix only; ambiguous mixed Chinese notation is retried.
            if(!DecimalPattern().IsMatch(number)||!TryRational(number,out numerator,out denominator))return false;
            numerator*=factor;
            return true;
        }
        if (FractionPattern().IsMatch(text))
        {
            var parts = text.Split('/');
            return BigInteger.TryParse(parts[0], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out numerator)
                && BigInteger.TryParse(parts[1], out denominator) && denominator > 0;
        }
        if (!DecimalPattern().IsMatch(text)) return false;
        var negative = text.StartsWith('-');
        var unsigned = text.TrimStart('+', '-');
        var digits = unsigned.Split('.');
        denominator = BigInteger.Pow(10, digits.Length == 2 ? digits[1].Length : 0);
        if (!BigInteger.TryParse(string.Concat(digits), out numerator)) return false;
        if (negative) numerator = -numerator;
        return true;
    }
    [GeneratedRegex(@"^[+-]?\d+/[1-9]\d*$")]
    private static partial Regex FractionPattern();
    [GeneratedRegex(@"^[+-]?(?:\d+(?:\.\d+)?|\.\d+)$")]
    private static partial Regex DecimalPattern();
    [GeneratedRegex(@"^[+-]?\d+$")]
    private static partial Regex IntegerPattern();
}
