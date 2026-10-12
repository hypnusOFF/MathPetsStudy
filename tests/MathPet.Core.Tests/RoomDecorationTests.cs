using MathPet.Core;
using Xunit;

namespace MathPet.Core.Tests;

public class RoomDecorationTests
{
    [Fact] public void RoomsAndDecorationsArePurchasedIndependentlyAndCannotOverdraw()
    {
        var state = new PlayerState { Coins = 90 }; var engine = new GameEngine(state);
        Assert.False(engine.BuyRoom("stars").Success);
        Assert.True(engine.BuyRoomDecoration("stars-mobile").Success);
        Assert.Equal(0, state.Coins);
        Assert.False(engine.BuyRoomDecoration("stars-mobile").Success);
        Assert.False(engine.BuyRoomDecoration("spring-rug").Success);
        Assert.DoesNotContain("stars", state.OwnedRooms);
        Assert.Throws<ArgumentException>(() => engine.SelectRoom("stars"));
        Assert.Throws<ArgumentException>(() => engine.PlaceRoomDecoration("spring-rug", true));
        Assert.Throws<ArgumentException>(() => engine.PlaceRoomDecoration("unknown", false));
    }

    [Fact] public void MixedDecorationsStayPlacedAcrossBackgroundsPetsAndSaving()
    {
        var state = new PlayerState(); var engine = new GameEngine(state);
        engine.BuyRoom("stars"); engine.BuyRoom("spring");
        engine.BuyRoomDecoration("study-bookcase"); engine.BuyRoomDecoration("stars-mobile");
        engine.PlaceRoomDecoration("study-bookcase", true); engine.PlaceRoomDecoration("stars-mobile", true);
        engine.PlaceRoomDecoration("stars-mobile", true);
        engine.SelectRoom("spring"); engine.SelectPet("rabbit");
        var restored = StateSerialization.Deserialize(StateSerialization.Serialize(state));
        Assert.Equal("spring", restored.Room); Assert.Equal("rabbit", restored.ActiveSpecies);
        Assert.Equal(2, restored.PlacedRoomDecorations.Count);
        Assert.Contains("study-bookcase", restored.PlacedRoomDecorations);
        new GameEngine(restored).SelectRoom("stars");
        Assert.Equal(2, restored.PlacedRoomDecorations.Count);
        new GameEngine(restored).PlaceRoomDecoration("stars-mobile", false);
        Assert.Contains("stars-mobile", restored.OwnedRoomDecorations);
        Assert.DoesNotContain("stars-mobile", restored.PlacedRoomDecorations);
    }

    [Fact] public void LegacyClothesAreRefundedOnceAndOldRoomAccessIsPreserved()
    {
        var old = new PlayerState { Version = 2, Coins = 123, HasRoom = true, Room = "stars" };
        old.OwnedWearables.UnionWith(["bow", "bell", "scarf", "overalls", "cape", "backpack"]);
        old.Pets[0].Outfit = "scarf"; old.Pets[1].Outfit = "scarf";
        old.Pets[0].Headwear = "bow"; old.Pets[2].Accessory = "backpack";
        old.CompletedPractices = 7;
        var migrated = StateSerialization.Deserialize(StateSerialization.Serialize(old));
        Assert.Equal(123 + 110 + 160 + 180 + 140, migrated.Coins);
        Assert.Equal(4, migrated.Version); Assert.Equal("stars", migrated.Room);
        Assert.Equal(3, migrated.OwnedRooms.Count); Assert.Equal(7, migrated.CompletedPractices);
        Assert.All(migrated.Pets, p => Assert.Equal("", p.Outfit));
        Assert.Equal("", migrated.Pets[2].Accessory); Assert.Equal("bow", migrated.Pets[0].Headwear);
        Assert.Equal(2, migrated.OwnedWearables.Count);
        var twice = StateSerialization.Deserialize(StateSerialization.Serialize(migrated));
        Assert.Equal(migrated.Coins, twice.Coins);
    }

    [Fact] public void CurrentSavesRejectUnownedUnknownOrRetiredDecorations()
    {
        var state = new PlayerState(); state.PlacedRoomDecorations.Add("stars-rug");
        Assert.Throws<InvalidDataException>(() => StateSerialization.Deserialize(StateSerialization.Serialize(state)));
        state.PlacedRoomDecorations.Clear(); state.OwnedRoomDecorations.Add("unknown");
        Assert.Throws<InvalidDataException>(() => StateSerialization.Deserialize(StateSerialization.Serialize(state)));
        state.OwnedRoomDecorations.Clear(); state.OwnedWearables.Add("scarf");
        Assert.Throws<InvalidDataException>(() => StateSerialization.Deserialize(StateSerialization.Serialize(state)));
    }

    [Fact] public void NewPlayersHaveTemporaryTestBalanceButExistingBalancesRemain()
    {
        Assert.Equal(99999, new PlayerState().Coins);
        Assert.Equal(42, StateSerialization.Deserialize(StateSerialization.Serialize(new PlayerState { Coins = 42 })).Coins);
        Assert.Equal(42, StateSerialization.Deserialize("{\"Version\":2,\"Coins\":42}").Coins);
    }
}
