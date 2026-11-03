using CUE4Parse.UE4.VirtualFileSystem;

namespace CUE4Parse.Tests;

public class PakOrderTests
{
    private const string Root = @"C:\Games\Infinity Nikki\InfinityNikki\X6Game";

    [Fact]
    public void HotPatchLayersOutrankInstalledPaks()
    {
        var fastPatch = AbstractVfsReader.GetPakOrderFromPakFilePath($@"{Root}\Saved\FastPatchPaks\PatchPak_Asset_4_2872_P.pak");
        var savedPaks = AbstractVfsReader.GetPakOrderFromPakFilePath($@"{Root}\Saved\Paks\Delay_Base_Bin_m4_2865_2872_P.pak");
        var contentPaks = AbstractVfsReader.GetPakOrderFromPakFilePath($@"{Root}\Content\Paks\Delay_Base_Bin_2827_P.pak");

        Assert.True(fastPatch > savedPaks);
        Assert.True(savedPaks > contentPaks);
    }

    [Fact]
    public void FolderOrderOnlyBreaksTiesBetweenChunkVersions()
    {
        // VerifyReadOrder adds 100 per patch chunk version, so the folder order must stay below that.
        Assert.InRange(AbstractVfsReader.GetPakOrderFromPakFilePath($"{Root}/Saved/FastPatchPaks/a_P.pak"), 0, 99);
        Assert.Equal(3, AbstractVfsReader.GetPakOrderFromPakFilePath(@"D:\Game\Content\Paks\pakchunk0-Windows.pak"));
        Assert.Equal(3, AbstractVfsReader.GetPakOrderFromPakFilePath("pakchunk0-Windows.utoc"));
    }
}
