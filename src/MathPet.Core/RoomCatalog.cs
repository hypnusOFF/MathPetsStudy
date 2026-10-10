namespace MathPet.Core;

public sealed record RoomTheme(string Id, string Name, int Price);
public sealed record RoomDecoration(string Id, string Theme, string Name, int Price,
    int X, int Y, int Width, int Layer, int Height = 0);

public static class RoomCatalog
{
    public static IReadOnlyList<RoomTheme> Themes { get; } =
        [new("study", "暖木学习屋", 0), new("spring", "春日花园", 100), new("stars", "星空梦境", 150)];

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
        new("study-rug", "study", "蓝色格纹地毯", 110, 50, 81, 63, 20, 28)
    ];
}
