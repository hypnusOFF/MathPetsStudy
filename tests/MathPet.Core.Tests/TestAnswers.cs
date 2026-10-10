using MathPet.Core;
namespace MathPet.Core.Tests;

internal static class TestAnswers
{
    public static string For(Question q) => q.Interaction == "classify" && !q.DiagramTask.StartsWith("read-")
        ? "选中：" + string.Join("、", q.Diagram.Select((x,i)=>(x,i))
            .Where(p=>p.x==(q.DiagramTask=="rectangles"?4:3)).Select(p=>p.i+1))
        : q.Answer;
}
