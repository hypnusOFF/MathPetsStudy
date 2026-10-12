namespace MathPet.Core;

public sealed record PetKind(string Species, string Name, int Price);
public sealed record FoodKind(string Id, string Name, string Icon, int Price, int Fullness, int Experience);
public sealed record ToyKind(string Id, string Name, string Icon, int Price, int Mood, int Experience);

public static class PetCatalog
{
    public static IReadOnlyList<PetKind> Pets { get; } =
        [new("cat","团团",0),new("rabbit","糯糯",0),new("fox","星星",0),
         new("dog","奶油小狗",1200),new("redpanda","红棕小熊猫",2500),new("otter","软萌小水獭",4000)];
    public static IReadOnlyList<FoodKind> Foods { get; } =
        [new("apple","苹果小点心","apple",20,20,4),new("cookie","星星小饼干","biscuit",40,10,10),
         new("rainbow-meal","彩虹果蔬餐","carrot",80,25,25),new("star-feast","星光庆祝餐","food-bowl",160,35,60)];
    public static IReadOnlyList<ToyKind> Toys { get; } =
        [new("yarn","彩色毛线球","yarn",50,15,0),new("plush-ball","毛绒小球","ball",100,15,3),
         new("puzzle-wheel","益智滚轮","brush",600,20,9),new("treasure-box","寻宝玩具箱","wand",1500,25,18)];
}
