namespace MathPet.Core;

public static class VisualExercises
{
    private static string Calculation(string mode,int[] d)=>mode switch {
      "ring"=>$"3.14×（{d[0]}×{d[0]}−{d[1]}×{d[1]}）=3.14×{d[0]*d[0]-d[1]*d[1]}。",
      "pie"=>$"100%−{d[1]}%={d[0]}%。",
      "practice"=>$"（{d[0]}+{d[1]}）×2={(d[0]+d[1])*2}米。",
      "reflect"=>$"轴在横坐标3.5处，横坐标7−{d[0]}={7-d[0]}，纵坐标仍是{d[1]}。",
      "clock"=>$"分针走{d[1]/5}个大格，每格5分钟，共{d[1]}分钟；时针在{d[0]}附近。",
      "bars" or "views"=>$"三列分别是{string.Join("、",d)}，最高列为{d.Max()}。",
      "classify"=>$"逐个检查三条直边：第{string.Join("、",d.Select((x,i)=>(x,i)).Where(p=>p.x==3).Select(p=>p.i+1))}号，共{d.Count(x=>x==3)}个。",
      "direction"=>$"从北向东沿刻度量到射线，读数是{d[0]}度。",
      "proportion"=>d[0]==1?"直线经过原点，y/x保持不变，属于正比例。":"曲线随x增大而降低，此图表示xy保持不变，属于反比例。",
      "angle"=>$"零刻度线对齐右侧射线，按这一侧刻度读到{d[0]}度。",
      _=>$"中心固定，顺时针转动{d[0]/90}个直角，得到{d[0]}度。"
    };
    public static IReadOnlyList<Topic> Create()
    {
        var specs=new (int Grade,Semester Semester,string Mode,string Name,string Unit,int[][] Data,string[] Answers,string[] Texts)[]
        {
            (1,Semester.Second,"classify","图形分类操作","分类与整理",[[3,4,3,4,3],[4,3,4,4,3],[3,3,4,3,3]],["3","2","4"],["选中所有三角形，再提交数量。","选中所有三角形，再提交数量。","选中所有三角形，再提交数量。"]),
            (2,Semester.First,"clock","拨钟面与时分","认识时间",[[3,30],[6,15],[9,45]],["330","615","945"],["把钟面拨到3时30分。","把钟面拨到6时15分。","把钟面拨到9时45分。"]),
            (3,Semester.Second,"bars","条形统计图读数","统计",[[3,7,5],[8,4,6],[5,9,2]],["7","8","9"],["观察统计图，最高的一组有多少人？","观察统计图，最高的一组有多少人？","观察统计图，最高的一组有多少人？"]),
            (4,Semester.First,"angle","量角与画角操作","角的度量",[[60],[120,1],[90,1]],["60","120","90"],["用量角器量出图中角的度数。","画一个120度的角。拖动滑条调整射线。","画一个90度的角。拖动滑条调整射线。"]),
            (4,Semester.Second,"reflect","在方格上画对称点","图形的运动",[[1,2],[2,3],[3,1]],["62","53","41"],["关于竖直虚线画出红点的对称点。","关于竖直虚线画出红点的对称点。","关于竖直虚线画出红点的对称点。"]),
            (5,Semester.Second,"rotate","旋转图形操作","图形的运动",[[90],[180],[270]],["90","180","270"],["将图形顺时针旋转90度。","将图形顺时针旋转180度。","将图形顺时针旋转270度。"]),
            (6,Semester.First,"pie","扇形统计图读数","统计",[[25,75],[40,60],[10,90]],["25","40","10"],["紫色扇形占总量的百分之几？","紫色扇形占总量的百分之几？","紫色扇形占总量的百分之几？"]),
            (6,Semester.Second,"ring","圆环面积","圆",[[5,3],[4,2],[6,4]],["50.24","37.68","62.8"],["外圆半径5厘米、内圆半径3厘米，求圆环面积。π取3.14。","外圆半径4厘米、内圆半径2厘米，求圆环面积。π取3.14。","外圆半径6厘米、内圆半径4厘米，求圆环面积。π取3.14。"]),
            (6,Semester.Second,"proportion","正反比例图像","比例",[[1,2],[2,3],[1,4]],["1","2","1"],["图中的关系是正比例还是反比例？选1表示正比例，2表示反比例。","图中的关系是正比例还是反比例？选1表示正比例，2表示反比例。","图中的关系是正比例还是反比例？选1表示正比例，2表示反比例。"]),
            (4,Semester.Second,"direction","方向与角度定位","位置与方向",[[30,200],[45,300],[60,400]],["30","45","60"],["目标在北偏东多少度？","目标在北偏东多少度？","目标在北偏东多少度？"]),
            (2,Semester.First,"views","观察积木视图","观察物体",[[2,1,3],[1,2,2],[3,2,1]],["3","2","3"],["从正面看，最高的一列有几个小正方形？","从正面看，最高的一列有几个小正方形？","从正面看，最高的一列有几个小正方形？"]),
            (5,Semester.Second,"practice","测量与综合实践","综合与实践",[[4,3],[5,4],[6,2]],["14","18","16"],["设计一个长4米、宽3米的花圃，用绳子围一周，需要多少米？","设计一个长5米、宽4米的花圃，用绳子围一周，需要多少米？","设计一个长6米、宽2米的花圃，用绳子围一周，需要多少米？"])
        };
        return specs.Select((s,index)=>
        {
            var id="visual-"+s.Mode;
            var explanation=s.Mode switch{
                "clock"=>"钟面有12个大格，每个大格5分钟。先拨时针，再拨分针。",
                "angle"=>"量角器中心对准顶点，零刻度线与一条边重合，沿另一条边读数。",
                "reflect"=>"对称点在轴的另一侧，与对称轴的距离相等，高度不变。",
                "rotate"=>"固定旋转中心，按指定方向和角度转动。长度和形状不变。",
                "pie"=>"整圆表示100%。各部分百分比合计100%。",
                "ring"=>"圆环面积=π×（外半径²−内半径²），不能用半径差的平方。",
                "proportion"=>"正比例图像是经过原点的直线；反比例图像是弯曲的线。",
                "direction"=>"先确定北，再从北向东量出角度。距离决定目标的远近。",
                "views"=>"从同一方向观察，遮住的小方块不会在视图中单独出现。",
                "practice"=>"先量出长宽，画出草图，再用周长公式2×（长+宽）估算材料。",
                _=>"观察图形与数量，按题目要求分类或读取图表。"};
            Question Make(int i,bool review)=>new($"{id}-{(review?"r":"q")}{i}",s.Grade,id,s.Texts[i],s.Answers[i],s.Mode=="ring"?"平方厘米":null,
                [explanation,"按图中的刻度、方向和数量逐步检查。"],[explanation,Calculation(s.Mode,s.Data[i]),$"本题结果：{(s.Mode=="clock"?$"{s.Data[i][0]}:{s.Data[i][1]:00}":s.Mode=="reflect"?$"（{7-s.Data[i][0]},{s.Data[i][1]}）":s.Answers[i])}。"],"注意单位、方向和对应关系。",90)
                {Interaction=s.Mode,Diagram=s.Data[i],QuestionType="图形操作与应用",TemplateId=s.Mode+"-"+i};
            // Reviews use another parameter set rather than the identical drawing.
            return new Topic(id,s.Grade,s.Name,"图形与实践",s.Name,explanation,s.Name,[explanation],
                Enumerable.Range(0,3).Select(i=>Make(i,false)).ToArray(),Enumerable.Range(0,3).Select(i=>Make((i+1)%3,true) with {Id=$"{id}-r{i}"}).ToArray())
                {Semester=s.Semester,UnitName=s.Unit,Order=2000+index};
        }).ToArray();
    }
}
