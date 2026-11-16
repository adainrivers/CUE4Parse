using System.Text;
using CUE4Parse.UE4.Assets.Exports.Engine;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using Xunit;

namespace CUE4Parse.Tests;

public sealed class WorldTileInfoTests
{
    [Fact]
    public void UE427TileInfoStopsBeforeFollowingLodPackageNames()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8, true);
        writer.Write(201600); writer.Write(-100800); writer.Write(0); // Position
        foreach (var bound in new[] { -100f, -200f, -30f, 100f, 200f, 30f }) writer.Write(bound);
        writer.Write((byte)1); // FBox validity
        WriteString(writer, "Landscape");
        writer.Write(0); writer.Write(0); writer.Write(0); // layer reserved fields
        writer.Write(50000); writer.Write(1); // distance, enabled
        writer.Write(0); // hidden
        WriteString(writer, "None");
        writer.Write(0); // LODList count
        writer.Write(7); // ZOrder
        var expectedEnd = stream.Position;
        writer.Write(0); // following FWorldCompositionTile.LODPackageNames count
        writer.Write(658); writer.Write(0); // following tile's FName, not part of Info
        using var archive = new FAssetArchive(new FByteArchive("TileFixture", stream.ToArray(), new VersionContainer(EGame.GAME_UE4_27)), null);
        var tile = new FWorldTileInfo(archive);
        Assert.Equal(201600, tile.Position.X);
        Assert.Equal("Landscape", tile.Layer.Name);
        Assert.Equal("None", tile.ParentTilePackageName);
        Assert.Equal(7, tile.ZOrder);
        Assert.Equal(expectedEnd, archive.Position);
        Assert.Equal(0, archive.Read<int>());
        Assert.Equal(658, archive.Read<int>());
    }

    private static void WriteString(BinaryWriter writer, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value + '\0');
        writer.Write(bytes.Length);
        writer.Write(bytes);
    }
}
