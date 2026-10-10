using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;
namespace MathPet.Core;

public static partial class AnswerChecker
{
    // Exact rational arithmetic avoids approximate decimal answers being accepted for fractions.
    public static bool IsCorrect(Question question, string input)
    {
        var text = Normalize(input);
        if(question.Interaction=="classify" && text.StartsWith("选中："))
        {
            var selected=text[3..].Split('、',StringSplitOptions.RemoveEmptyEntries);
            if(selected.Any(x=>!int.TryParse(x,out _)))return false;
            return selected.Select(int.Parse).Order().SequenceEqual(question.Diagram.Select((x,i)=>(x,i)).Where(p=>p.x==3).Select(p=>p.i+1));
        }
        if(question.Interaction=="reflect" && (text.Contains(',')||text.Contains('，')))
        {
            var coordinates=text.Trim('(',')','（','）').Replace('，',',').Split(',');
            if(coordinates.Length!=2||!int.TryParse(coordinates[0],out var x)||!int.TryParse(coordinates[1],out var y)||x is <0 or >7||y is <0 or >5)return false;
            text=(x*10+y).ToString(CultureInfo.InvariantCulture);
        }
        if(question.Interaction=="clock" && text.Contains(':'))
        {
            var parts=text.Split(':');
            if(parts.Length!=2||!int.TryParse(parts[0],out var hour)||!int.TryParse(parts[1],out var minute)||hour is <1 or >12||minute is <0 or >59)return false;
            text=(hour*100+minute).ToString(CultureInfo.InvariantCulture);
        }
        if (question.Unit is { } unit && text.EndsWith(unit, StringComparison.Ordinal))
            text = text[..^unit.Length];
        return TryRational(text, out var numerator, out var denominator)
            && TryRational(question.Answer, out var expectedNumerator, out var expectedDenominator)
            && numerator * expectedDenominator == expectedNumerator * denominator;
    }
    private static string Normalize(string input)
    {
        if (input.Length > 100) return "";
        return string.Concat(input.Trim().Select(c => c switch
        {
            >= '０' and <= '９' => (char)(c - '０' + '0'),
            '．' => '.', '／' => '/', '−' or '－' => '-', '＋' => '+',
            _ => c
        }).Where(c => !char.IsWhiteSpace(c)));
    }
    private static bool TryRational(string text, out BigInteger numerator, out BigInteger denominator)
    {
        numerator = BigInteger.Zero; denominator = BigInteger.One;
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
}
