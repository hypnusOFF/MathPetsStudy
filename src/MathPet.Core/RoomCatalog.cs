namespace MathPet.Core;

public sealed record RoomTheme(string Id, string Name, int Price);
public sealed record RoomDecoration(string Id, string Theme, string Name, int Price,
    int X, int Y, int Width, int Layer, int Height = 0, string? Atlas = null, int Tile = 0);

public static class RoomCatalog
{
    public static IReadOnlyList<RoomTheme> Themes { get; } =
        [new("study", "暖木学习屋", 0), new("spring", "春日花园", 100), new("stars", "星空梦境", 150),
         new("forest","雨林小屋",400),new("ocean","海洋贝壳屋",500),new("autumn","秋日丰收屋",600)];

    // Percent coordinates describe the center on a shared 16:10 canvas.
    // They belong to each item and never change with the selected background.
    public static IReadOnlyList<RoomDecoration> Decorations { get; } =
    [
        new("spring-plant", "spring", "垂叶吊盆", 80, 17, 22, 16, 10),
        new("spring-picture", "spring", "小花挂画", 90, 79, 23, 17, 10),
        new("spring-bench", "spring", "碎花窗边长凳", 160, 17, 61, 29, 30),
        new("spring-cabinet", "spring", "花朵展示柜", 180, 87, 57, 21, 30),
        new("spring-rug", "spring", "奶油花边地毯", 110, 50, 81, 63, 20, 28),
        new("stars-mobile", "stars", "月亮星星吊饰", 90, 19, 22, 18, 10),
        new("stars-picture", "stars", "星座挂画", 100, 78, 24, 20, 10),
        new("stars-bed", "stars", "云朵宠物床", 160, 17, 67, 28, 30),
        new("stars-telescope", "stars", "小望远镜", 180, 86, 64, 19, 30),
        new("stars-rug", "stars", "淡紫星星地毯", 120, 50, 81, 63, 20, 28),
        new("study-abacus", "study", "木珠算盘挂饰", 80, 19, 24, 18, 10),
        new("study-picture", "study", "趣味数字挂画", 90, 79, 23, 18, 10),
        new("study-bench", "study", "藏书小长凳", 160, 17, 63, 28, 30),
        new("study-bookcase", "study", "低矮小书柜", 180, 86, 59, 23, 30),
        new("study-rug", "study", "蓝色格纹地毯", 110, 50, 81, 63, 20, 28),
        new("forest-lamp","forest","叶子灯",300,17,22,16,10,0,"forest-props",0),
        new("forest-picture","forest","森林挂画",300,79,23,19,10,0,"forest-props",1),
        new("forest-bed","forest","蘑菇宠物床",450,17,66,29,30,0,"forest-props",2),
        new("forest-shelf","forest","树枝书架",600,86,62,23,30,0,"forest-props",3),
        new("forest-rug","forest","苔藓地毯",550,50,81,63,20,28),
        new("ocean-mobile","ocean","贝壳吊饰",400,17,22,17,10,0,"ocean-props",0),
        new("ocean-picture","ocean","珊瑚挂画",400,79,23,19,10,0,"ocean-props",1),
        new("ocean-bed","ocean","贝壳宠物床",650,17,66,29,30,0,"ocean-props",2),
        new("ocean-cabinet","ocean","水族展示柜",750,86,62,23,30,0,"ocean-props",3),
        new("ocean-rug","ocean","海浪地毯",600,50,81,63,20,28),
        new("autumn-lantern","autumn","小灯笼",450,17,22,16,10,0,"autumn-props",0),
        new("autumn-picture","autumn","丰收挂画",450,79,23,19,10,0,"autumn-props",1),
        new("autumn-bench","autumn","藤篮长凳",700,17,66,29,30,0,"autumn-props",2),
        new("autumn-cabinet","autumn","点心展示柜",1000,86,62,23,30,0,"autumn-props",3),
        new("autumn-rug","autumn","落叶地毯",700,50,81,63,20,28)
    ];
}
