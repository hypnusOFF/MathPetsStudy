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
            (1,Semester.Second,"classify","图形分类操作","分类与整理",[[3,4,3,4,3],[4,3,4,4,3],[3,3,4,3,3]],["3","2","4"],["选中所有三角形后提交。","选中所有三角形后提交。","选中所有三角形后提交。"]),
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
                "proportion"=>"正比例图像是经过原点的直线；反比例满足xy保持不变（x不为0）；不能仅凭曲线下降判断。",
                "direction"=>"先确定北，再从北向东量出角度。距离决定目标的远近。",
                "views"=>"从同一方向观察，遮住的小方块不会在视图中单独出现。",
                "practice"=>"先量出长宽，画出草图，再用周长公式2×（长+宽）估算材料。",
                _=>"观察图形与数量，按题目要求分类或读取图表。"};
            Question Make(int i,bool review)=>new($"{id}-{(review?"r":"q")}{i}",s.Grade,id,s.Texts[i],s.Answers[i],s.Mode=="ring"?"平方厘米":null,
                [explanation,"按图中的刻度、方向和数量逐步检查。"],[explanation,Calculation(s.Mode,s.Data[i]),$"本题结果：{(s.Mode=="clock"?$"{s.Data[i][0]}:{s.Data[i][1]:00}":s.Mode=="reflect"?$"（{7-s.Data[i][0]},{s.Data[i][1]}）":s.Answers[i])}。"],"注意单位、方向和对应关系。",90)
                {Interaction=s.Mode,Diagram=s.Data[i],QuestionType="图形基础操作",TemplateId=s.Mode+"-basic"};
            // Reviews use another parameter set rather than the identical drawing.
            return Expand(new Topic(id,s.Grade,s.Name,"图形与实践",s.Name,explanation,s.Name,[explanation],
                Enumerable.Range(0,3).Select(i=>Make(i,false)).ToArray(),Enumerable.Range(0,3).Select(i=>Review(Make(i,true),i)).ToArray())
                {Semester=s.Semester,UnitName=s.Unit,Order=2000+index});
        }).ToArray();
    }
    private static Question Review(Question q,int index)
    {
        int[] d=q.Interaction switch {
            "classify"=>index switch {0=>[4,3,3,3,4],1=>[3,4,3,4,4],_=>[4,3,3,3,3]},
            "clock"=>index switch {0=>[4,35],1=>[7,20],_=>[10,50]},
            "bars"=>index switch {0=>[4,8,6],1=>[9,5,7],_=>[6,10,3]},
            "angle"=>index switch {0=>[70],1=>[130,1],_=>[100,1]},
            "reflect"=>[q.Diagram[0],q.Diagram[1]+2],
            "rotate"=>[q.Diagram[0],30],
            "pie"=>index switch {0=>[35,65],1=>[55,45],_=>[15,85]},
            "ring"=>index switch {0=>[7,3],1=>[5,2],_=>[8,4]},
            "proportion"=>[q.Diagram[0],q.Diagram[1]+1],
            "direction"=>index switch {0=>[15,250],1=>[75,350],_=>[90,450]},
            "views"=>index switch {0=>[3,1,2],1=>[2,3,1],_=>[1,1,2]},
            _=>index switch {0=>[7,4],1=>[8,3],_=>[9,5]}
        };
        var answer=q.Interaction switch {
            "classify"=>d.Count(x=>x==3),"clock"=>d[0]*100+d[1],
            "bars" or "views"=>d.Max(),"reflect"=>(7-d[0])*10+d[1],
            "ring"=>3.14m*(d[0]*d[0]-d[1]*d[1]),"practice"=>2*(d[0]+d[1]),_=>d[0]
        };
        var text=q.Interaction switch {
            "clock"=>$"把钟面拨到{d[0]}时{d[1]}分。",
            "angle"=>d.Length==1?"用量角器量出图中角的度数。":$"画一个{d[0]}度的角。拖动滑条调整射线。",
            "rotate"=>$"以图中位置为起点，将图形顺时针旋转{d[0]}度。",
            "ring"=>$"外圆半径{d[0]}厘米、内圆半径{d[1]}厘米，求圆环面积。π取3.14。",
            "practice"=>$"设计一个长{d[0]}米、宽{d[1]}米的花圃，用绳子围一周，需要多少米？",_=>q.Text
        };
        var work=Calculation(q.Interaction!,d);
        var display=q.Interaction=="clock"?$"{d[0]}:{d[1]:00}":q.Interaction=="reflect"?$"（{7-d[0]}，{d[1]}）":answer.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return q with {Diagram=d,Text=text,Answer=answer.ToString(System.Globalization.CultureInfo.InvariantCulture),Hints=[q.Hints[0],work],Solution=[q.Solution[0],work,$"结果为{display}{q.Unit}。"]};
    }

    private static Topic Expand(Topic topic)
    {
        var questions=topic.Questions.ToList();var reviews=topic.ReviewQuestions.ToList();
        for(var task=1;task<=3;task++)for(var variant=0;variant<3;variant++){
            questions.Add(Additional(topic.Questions[variant],task,variant,false));
            reviews.Add(Additional(topic.ReviewQuestions[variant],task,variant,true));
        }
        return topic with {Questions=questions,ReviewQuestions=reviews};
    }

    private static Question Additional(Question basis,int task,int variant,bool review)
    {
        var d=basis.Diagram;var mode=basis.Interaction!;
        (string Text,decimal Answer,string Work,string Kind,string? Unit) entry=(mode,task) switch {
            ("classify",1)=>("选中所有四边形。必须选对每一个图形。",d.Count(x=>x==4),$"四边形有四条边：第{string.Join("、",d.Select((x,i)=>(x,i)).Where(p=>p.x==4).Select(p=>p.i+1))}号。","四边形分类",null),
            ("classify",2)=>("三角形和四边形的数量相差几个？",Math.Abs(d.Count(x=>x==3)-d.Count(x=>x==4)),$"三角形{d.Count(x=>x==3)}个，四边形{d.Count(x=>x==4)}个；较大数量减较小数量。","分类比较","个"),
            ("classify",3)=>("拿走全部三角形，还剩几个图形？",d.Count(x=>x==4),$"总数{d.Length}−三角形{d.Count(x=>x==3)}＝{d.Count(x=>x==4)}。","分类补缺","个"),
            ("clock",1)=>($"图中钟面是{d[0]}时{d[1]}分。从这个时刻到{d[0]+1}时整还需要几分钟？",60-d[1],$"下一整时还差60−{d[1]}＝{60-d[1]}分钟。","经过时间","分钟"),
            ("clock",2)=>("读钟面：分针从12顺时针走过了几个大格？",d[1]/5,$"每个大格5分钟，{d[1]}÷5＝{d[1]/5}个大格。","钟面读数","格"),
            ("clock",3)=>($"一次活动用时{d[0]}小时{d[1]}分钟，一共多少分钟？",d[0]*60+d[1],$"1小时＝60分钟，{d[0]}×60＋{d[1]}＝{d[0]*60+d[1]}分钟。","时分换算","分钟"),
            ("bars",1)=>("统计图中三组一共有多少人？",d.Sum(),$"{string.Join("＋",d)}＝{d.Sum()}。","读图求和","人"),
            ("bars",2)=>("最多的一组比最少的一组多多少人？",d.Max()-d.Min(),$"最多{d.Max()}−最少{d.Min()}＝{d.Max()-d.Min()}。","读图比较","人"),
            ("bars",3)=>($"前三组人数如图。第四组加入后，总人数为{d.Sum()+variant+4}人，第四组有多少人？",variant+4,$"总数{d.Sum()+variant+4}−前三组{d.Sum()}＝{variant+4}。","统计补缺","人"),
            ("angle",1)=>($"图示角为{d[0]}度，与它合成一个平角的另一个角是多少度？",180-d[0],$"平角180度，180−{d[0]}＝{180-d[0]}。","角度合成","度"),
            ("angle",2)=>("图示角属于哪一类？选0锐角、1直角、2钝角。",d[0]<90?0:d[0]==90?1:2,$"{d[0]}与90比较；小于90是锐角，等于90是直角，大于90且小于180是钝角。","角的分类",null),
            ("angle",3)=>($"图示角为{d[0]}度。把角的两条边画长，度数会改变吗？选0不变、1改变。",0,"角的大小由两条边张开的程度决定，与边长无关。","角的辨析",null),
            ("reflect",1)=>("红点关于虚线的对称点，横坐标是多少？",7-d[0],$"轴横坐标3.5；两点横坐标之和为7，7−{d[0]}＝{7-d[0]}。","对称坐标",null),
            ("reflect",2)=>("红点与它的对称点之间，水平方向相隔几个小格？",7-2*d[0],$"轴两侧距离相等，两点间隔（7−{d[0]}）−{d[0]}＝{7-2*d[0]}格。","对称距离","格"),
            ("reflect",3)=>($"有人把对称点画在（{7-d[0]}，{d[1]+1}）。这样对吗？选0错误、1正确。",0,$"对称后高度不变，正确纵坐标应是{d[1]}，不是{d[1]+1}。","对称辨析",null),
            ("rotate",1)=>($"从图示位置顺时针转{d[0]}度，相当于转了几个直角？",d[0]/90,$"一个直角90度，{d[0]}÷90＝{d[0]/90}。","旋转计数",null),
            ("rotate",2)=>($"从图示位置已顺时针转{d[0]}度，还需同方向转多少度才满一周？",360-d[0],$"一周360度，360−{d[0]}＝{360-d[0]}。","补全旋转","度"),
            ("rotate",3)=>($"图形顺时针转{d[0]}度后，面积会变大吗？选0不会、1会。",0,"旋转保持形状和大小，面积不变。","旋转辨析",null),
            ("pie",1)=>($"全班{100*(variant+1)}人，图中紫色部分表示参加合唱的比例，合唱有多少人？",d[0]*(variant+1),$"紫色占100%−{d[1]}%＝{d[0]}%；{100*(variant+1)}×{d[0]}%＝{d[0]*(variant+1)}。","扇形求部分","人"),
            ("pie",2)=>($"紫色部分对应{d[0]*(variant+2)}件物品，总共有多少件？",100*(variant+2),$"紫色占{d[0]}%，总数＝{d[0]*(variant+2)}÷{d[0]}%＝{100*(variant+2)}。","扇形反求总量","件"),
            ("pie",3)=>("绿色和紫色部分相差几个百分点？",Math.Abs(d[1]-d[0]),$"先求紫色100−{d[1]}＝{d[0]}，再用较大百分比减较小百分比，得{Math.Abs(d[1]-d[0])}个百分点。","百分比比较",null),
            ("ring",1)=>("两个圆的半径相差多少厘米？",d[0]-d[1],$"外半径{d[0]}−内半径{d[1]}＝{d[0]-d[1]}。","圆环宽度","厘米"),
            ("ring",2)=>("外圆的直径是多少厘米？",2*d[0],$"直径＝半径×2＝{d[0]}×2＝{2*d[0]}。","半径与直径","厘米"),
            ("ring",3)=>("求图中圆环面积，能用3.14×（外半径−内半径）²吗？选0不能、1能。",0,"面积差＝3.14×（外半径²−内半径²），先平方再相减，不能用半径差的平方。","圆环公式辨析",null),
            ("proportion",1)=>($"图中关系满足{(d[0]==1?$"y＝{d[1]}x":$"xy＝{d[1]}")}。当x＝1时，y是多少？",d[1],$"代入x＝1，y＝{d[1]}。","比例代入",null),
            ("proportion",2)=>($"图中关系满足{(d[0]==1?$"y＝{d[1]}x":$"xy＝{d[1]}")}。当y＝{2*d[1]}时，x是多少？",d[0]==1?2:0.5m,d[0]==1?$"x＝{2*d[1]}÷{d[1]}＝2。":$"x＝{d[1]}÷{2*d[1]}＝0.5。","比例反求",null),
            ("proportion",3)=>("图中的x、y满足什么保持不变？选0表示y/x，1表示xy。",d[0]==1?0:1,d[0]==1?$"y/x＝{d[1]}，商一定。":$"xy＝{d[1]}，乘积一定。","比例不变量",null),
            ("direction",1)=>("图中目标与中心之间的距离是多少米？",d[1],$"读取距离标记：{d[1]}米。","方向读距","米"),
            ("direction",2)=>($"目标在北偏东{d[0]}度，也可以说东偏北多少度？",90-d[0],$"北与东相差90度，90−{d[0]}＝{90-d[0]}。","方位转换","度"),
            ("direction",3)=>($"目标在北偏东{d[0]}度、距离{d[1]}米处。如果角度不变、距离加倍，距离为多少米？",2*d[1],$"只改变距离：{d[1]}×2＝{2*d[1]}米；方向不变。","方位应用","米"),
            ("views",1)=>("这些积木只有一排、没有前后遮挡，总共有几个正方体？",d.Sum(),$"各列相加：{string.Join("＋",d)}＝{d.Sum()}。","积木总量","个"),
            ("views",2)=>("从正面看，最高列比最矮列多几个小正方形？",d.Max()-d.Min(),$"最高{d.Max()}−最矮{d.Min()}＝{d.Max()-d.Min()}。","视图比较","个"),
            ("views",3)=>($"将三列都补到{d.Max()}个正方体高，一共还需几个正方体？",3*d.Max()-d.Sum(),$"补齐后共3×{d.Max()}＝{3*d.Max()}；减去已有{d.Sum()}，还需{3*d.Max()-d.Sum()}。","视图补全","个"),
            ("practice",1)=>("图中长方形花圃面积是多少平方米？",d[0]*d[1],$"长×宽＝{d[0]}×{d[1]}＝{d[0]*d[1]}。","花圃面积","平方米"),
            ("practice",2)=>($"长{d[0]}米、宽{d[1]}米的花圃，围栏留出1米入口，需围多少米？",2*(d[0]+d[1])-1,$"周长（{d[0]}＋{d[1]}）×2＝{2*(d[0]+d[1])}；减去1米入口。","材料估算","米"),
            _=>($"花圃长{d[0]}米，周长{2*(d[0]+d[1])}米，宽是多少米？",d[1],$"宽＝周长÷2−长＝{2*(d[0]+d[1])}÷2−{d[0]}＝{d[1]}。","实践反求","米")
        };
        return basis with {Id=$"{basis.TopicId}-{(review?"r":"q")}-t{task}-v{variant}",Text=entry.Text,
            Answer=entry.Answer.ToString(System.Globalization.CultureInfo.InvariantCulture),Unit=entry.Unit,
            Hints=["先读清题目求的是哪种量，再读取图中对应条件。",entry.Work],Solution=["已知条件见题干与图示。",entry.Work,$"答案：{entry.Answer}{entry.Unit}。"],
            CommonMistake=mode=="proportion"?"看曲线形状还不够，需要验证比值或乘积是否保持不变。":"读图时区分总量、相差量和单位，不要把已知数直接当作答案。",
            QuestionType=entry.Kind,TemplateId=$"{mode}-task{task}",DiagramTask=mode=="classify"&&task==1?"rectangles":$"read-{task}",
            TimeLimitSeconds=task==3?90:mode is "classify" or "views"?60:90,Difficulty=task==3?1.25m:1m,
            Options=entry.Text.Contains("选0")?(entry.Text.Contains("选0锐角")?["锐角","直角","钝角"]:entry.Text.Contains("y/x")?["y/x保持不变","xy保持不变"]:entry.Text.Contains("0不会")?["不会","会"]:entry.Text.Contains("0不变")?["不变","改变"]:entry.Text.Contains("0不能")?["不能","能"]:["错误","正确"]):[]};
    }
}
