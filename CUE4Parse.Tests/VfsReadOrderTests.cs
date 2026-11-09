using CUE4Parse.Compression;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.FileProvider.Vfs;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using CUE4Parse.UE4.VirtualFileSystem;
using static CUE4Parse.UE4.Versions.EGame;

namespace CUE4Parse.Tests;

public class VfsReadOrderTests
{
    [Theory]
    [InlineData("PatchPak_Asset_2_2872_P.pak", "PatchPak_Asset_4_2872_P.pak")]
    [InlineData("PatchPak_Asset_9_2872_P.pak", "PatchPak_Asset_16_2872_P.pak")]
    [InlineData("PatchPak_LuaFramework_5_2872_P.pak", "PatchPak_LuaFramework_6_2872_P.pak")]
    [InlineData("PatchPak_Asset_16_2871_P.pak", "PatchPak_Asset_1_2872_P.pak")]
    [InlineData("PatchPak_Asset_2147483647_2871_P.pak", "PatchPak_Asset_1_2872_P.pak")]
    [InlineData("PatchPak_Asset_1_2147483647_P.pak", "PatchPak_Asset_2147483647_2147483647_P.pak")]
    [InlineData("PatchPak_Asset_2_2872_P.utoc", "PatchPak_Asset_4_2872_P.utoc")]
    [InlineData("patchpak_asset_2_2872_p.PAK", "patchpak_asset_4_2872_p.PAK")]
    public void NikkiPatchIndexBreaksSameBuildTies(string older, string newer)
    {
        using var oldReader = new TestReader(older);
        using var newReader = new TestReader(newer);
        Assert.True(newReader.ReadOrder > oldReader.ReadOrder,
            $"{newer} ({newReader.ReadOrder}) must outrank {older} ({oldReader.ReadOrder}).");
    }

    [Theory]
    [InlineData("X6Game/Content/Paks/Client_2872_P.pak", "X6Game/Saved/Paks/Delay_2872_P.pak")]
    [InlineData("X6Game/Content/Paks/PatchPak_Asset_16_2872_P.pak", "X6Game/Saved/Paks/Delay_2872_P.pak")]
    [InlineData("X6Game/Saved/Paks/PatchPak_Asset_16_2872_P.pak", "X6Game/Saved/FastPatchPaks/PatchPak_Asset_1_2872_P.pak")]
    [InlineData("X6Game/Saved/Paks/Delay_2872_P.pak", "X6Game/Saved/FastPatchPaks/PatchPak_Asset_1_2872_P.pak")]
    [InlineData("X6Game\\Saved\\Paks\\Delay_2872_P.pak", "X6Game\\Saved\\FastPatchPaks\\PatchPak_Asset_1_2872_P.pak")]
    [InlineData("X6Game/Saved/FastPatchPaks/PatchPak_Asset_16_2872_P.pak", "X6Game/Content/Paks/Client_2873_P.pak")]
    public void NikkiBuildPrecedesLayerThenPatchIndex(string older, string newer)
    {
        using var oldReader = new TestReader(older);
        using var newReader = new TestReader(newer);
        Assert.True(newReader.ReadOrder > oldReader.ReadOrder);
    }

    [Theory]
    [InlineData("Client.pak", 3)]
    [InlineData("Client_P.pak", 103)]
    [InlineData("Client_0_P.pak", 103)]
    [InlineData("Client_1_P.pak", 203)]
    [InlineData("Client_2872_P.pak", 287303)]
    [InlineData("Client_2872_P.utoc", 287303)]
    [InlineData("Client_2872_P.o.utoc", 287303)]
    [InlineData("PatchPak_Asset_16_2872_P.pak", 287303)]
    public void OtherGamesKeepUnrealReadOrder(string path, long expected)
    {
        using var reader = new TestReader(path, GAME_UE5_4);
        Assert.Equal(expected, reader.ReadOrder);
    }

    [Theory]
    [InlineData("PatchPak_Asset_bad_2872_P.pak")]
    [InlineData("PatchPak_Asset_-1_2872_P.pak")]
    [InlineData("PatchPak_Asset_2147483648_2872_P.pak")]
    public void InvalidPatchIndexFallsBackToLayerOrder(string path)
    {
        using var reader = new TestReader(path);
        using var zero = new TestReader("PatchPak_Asset_0_2872_P.pak");
        Assert.Equal(zero.ReadOrder, reader.ReadOrder);
    }

    [Fact]
    public void IncrementalPatchFilesResolveIndependentlyOfConcurrentMountOrder()
    {
        using var content = new TestReader("X6Game/Content/Paks/Client_2872_P.pak", files: ["TbObjectiveBasic.bin", "TbObjectiveBasic.binextra"]);
        using var saved = new TestReader("X6Game/Saved/Paks/Delay_2872_P.pak", files: ["TbObjectiveBasic.bin", "TbObjectiveBasic.binextra"]);
        using var patch2 = new TestReader("PatchPak_Asset_2_2872_P.pak", files: ["TbObjectiveBasic.bin", "TbObjectiveBasic.binextra"]);
        using var patch4 = new TestReader("PatchPak_Asset_4_2872_P.pak", files: ["TbObjectiveBasic.bin"]);
        using var patch6 = new TestReader("PatchPak_LuaFramework_6_2872_P.pak", files: ["Script/CfgTypes"]);
        TestReader[] readers = [content, saved, patch2, patch4, patch6];

        for (var iteration = 0; iteration < 20; iteration++)
        {
            var dictionary = new FileProviderDictionary();
            var order = readers.Skip(iteration % readers.Length).Concat(readers.Take(iteration % readers.Length));
            if (iteration % 2 == 1) order = order.Reverse();
            Parallel.ForEach(order, reader => dictionary.AddFiles(reader.Files, reader.ReadOrder));

            Assert.Same(patch4.Files["TbObjectiveBasic.bin"], dictionary["TbObjectiveBasic.bin"]);
            Assert.Same(patch2.Files["TbObjectiveBasic.binextra"], dictionary["TbObjectiveBasic.binextra"]);
            Assert.Same(patch6.Files["Script/CfgTypes"], dictionary["Script/CfgTypes"]);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MissingHotPatchPayloadFallsBackToSavedThenContent(bool savedHasPayload)
    {
        using var content = new TestReader("X6Game/Content/Paks/Client_2872_P.pak", files: ["TbObjectiveBasic.bin", "TbObjectiveBasic.binextra"]);
        using var saved = new TestReader("X6Game/Saved/Paks/Delay_2872_P.pak",
            files: savedHasPayload ? ["TbObjectiveBasic.bin", "TbObjectiveBasic.binextra"] : ["TbObjectiveBasic.bin"]);
        using var patch = new TestReader("X6Game/Saved/FastPatchPaks/PatchPak_Asset_4_2872_P.pak", files: ["TbObjectiveBasic.bin"]);
        var expectedPayload = (savedHasPayload ? saved : content).Files["TbObjectiveBasic.binextra"];
        TestReader[] readers = [content, saved, patch];
        foreach (var order in new[] { readers, readers.Reverse().ToArray() })
        {
            var dictionary = new FileProviderDictionary();
            foreach (var reader in order) dictionary.AddFiles(reader.Files, reader.ReadOrder);
            Assert.Same(patch.Files["TbObjectiveBasic.bin"], dictionary["TbObjectiveBasic.bin"]);
            Assert.Same(expectedPayload, dictionary["TbObjectiveBasic.binextra"]);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NewContentBuildOverridesStaleHotPatchInProviderLookup(bool reverse)
    {
        using var content = new TestReader("X6Game/Content/Paks/Client_2873_P.pak", files: ["TbSpawner.bin"]);
        using var patch = new TestReader("X6Game/Saved/FastPatchPaks/PatchPak_Asset_16_2872_P.pak", files: ["TbSpawner.bin"]);
        TestReader[] readers = reverse ? [patch, content] : [content, patch];
        var dictionary = new FileProviderDictionary();
        foreach (var reader in readers) dictionary.AddFiles(reader.Files, reader.ReadOrder);
        Assert.Same(content.Files["TbSpawner.bin"], dictionary["TbSpawner.bin"]);
    }

    private sealed class TestReader : AbstractVfsReader
    {
        public override string MountPoint { get; protected set; } = "../../../X6Game/";
        public override bool HasDirectoryIndex => true;

        public TestReader(string path, EGame game = GAME_InfinityNikki, string[]? files = null)
            : base(path, new VersionContainer(game))
        {
            var mountPoint = MountPoint;
            ValidateMountPoint(ref mountPoint);
            MountPoint = mountPoint;
            Files = (files ?? []).ToDictionary(file => file, file => (GameFile) new TestFile(file));
        }

        public override void Mount(StringComparer pathComparer) { }
        public override byte[] Extract(VfsEntry entry, FByteBulkDataHeader? header = null) => throw new NotSupportedException();
        public override void Dispose() { }
    }

    private sealed class TestFile(string path) : GameFile(path, 0)
    {
        public override bool IsEncrypted => false;
        public override CompressionMethod CompressionMethod => CompressionMethod.None;
        public override byte[] Read(FByteBulkDataHeader? header = null) => [];
        public override FArchive CreateReader(FByteBulkDataHeader? header = null) => new FByteArchive(Path, []);
    }
}
