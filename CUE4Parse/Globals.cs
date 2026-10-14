global using static CUE4Parse.UE4.Versions.EGame;
global using static CUE4Parse.CUE4ParseLog;

using System.Diagnostics.CodeAnalysis;

namespace CUE4Parse;

[SuppressMessage("ReSharper", "ConvertToConstant.Global")]
[SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Global")]
public static class Globals
{
    public static bool LogVfsMounts = true;
    public static bool FatalObjectSerializationErrors = false;
    public static bool WarnMissingImportPackage = true;
    /// <summary>Called after every export deserialization with the bytes left before the export's end
    /// (negative = read past it). Lets tools check a read ended exactly at the serial size.</summary>
    public static Action<UE4.Assets.Exports.UObject, long>? ExportReadEnd = null;
}
