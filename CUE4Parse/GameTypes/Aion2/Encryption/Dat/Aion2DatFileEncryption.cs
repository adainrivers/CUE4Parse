using System.Buffers.Binary;
using System.Text;
using Blake3;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.GameTypes.Aion2.Objects;
using CUE4Parse.UE4.Exceptions;

namespace CUE4Parse.GameTypes.Aion2.Encryption.Aes;

public sealed partial class Aion2DatFileEncryption
{
    private const ulong HeaderXorConstant = 0xCD02190910CE83F6;
    private static ulong _headerKeyRoot = Blake3Hash(HeaderXorConstant);
    private static ulong _headerPermutationRoot;
    private static bool _useHeaderPermutation;

    public static Dictionary<ulong, FAesKey> AesKeys = [];
    private static readonly Lock _instanceLock = new();

    public static void Initialize(IFileProvider provider)
    {
        lock (_instanceLock)
        {
            if (AesKeys.Count != 0)
                return;

            var keyManifest = provider.Files.Values.FirstOrDefault(x => x.Path.EndsWith("/key_manifest.dat", StringComparison.OrdinalIgnoreCase))
                ?? throw new ParserException("Unable to locate key_manifest.dat");

            try
            {
                var manifestFile = new FAion2KeyManifestFile(keyManifest);
                if (manifestFile.VersionContext is { } context)
                {
                    _headerPermutationRoot = GetHeaderPermutationRoot(context);
                    _headerKeyRoot = Blake3Hash(_headerPermutationRoot);
                    _useHeaderPermutation = true;
                }
                else
                {
                    _headerPermutationRoot = 0;
                    _headerKeyRoot = Blake3Hash(HeaderXorConstant);
                    _useHeaderPermutation = false;
                }

                AesKeys = manifestFile.AesKeys;
            }
            catch (Exception e)
            {
                Log.Error(e, "Failed to read key_manifest.dat");
                throw;
            }
        }
    }

    internal static ulong GetHeaderPermutationRoot(ReadOnlySpan<byte> versionContext)
    {
        Span<byte> input = stackalloc byte[40];
        BinaryPrimitives.WriteUInt64LittleEndian(input, HeaderXorConstant);
        versionContext.CopyTo(input[8..]);
        return Blake3Hash(input);
    }

    private static byte[] DeriveHeaderKey(ulong seed, EHeaderType type) =>
        DeriveHeaderKey(seed, type, _headerKeyRoot);

    private static byte[] DeriveHeaderKey(ulong seed, EHeaderType type, ulong headerKeyRoot)
    {
        Span<byte> input = stackalloc byte[24];
        BinaryPrimitives.WriteUInt64LittleEndian(input, headerKeyRoot);
        BinaryPrimitives.WriteUInt64LittleEndian(input[8..], seed);
        BinaryPrimitives.WriteInt32LittleEndian(input[16..], (int) type);
        input[20..].Clear();
        using var hasher = Hasher.New();
        hasher.Update(input);

        return hasher.Finalize().AsSpan().ToArray();
    }

    private static ulong Blake3Hash(string value)
    {
        using var hasher = Hasher.New();
        hasher.Update(Encoding.UTF8.GetBytes(value));
        return BinaryPrimitives.ReadUInt64LittleEndian(hasher.Finalize().AsSpan());
    }

    private static ulong Blake3Hash(ulong value)
    {
        Span<byte> input = stackalloc byte[sizeof(ulong)];
        BinaryPrimitives.WriteUInt64LittleEndian(input, value);
        using var hasher = Hasher.New();
        hasher.Update(input);
        return BinaryPrimitives.ReadUInt64LittleEndian(hasher.Finalize().AsSpan());
    }

    private static ulong Blake3Hash(ReadOnlySpan<byte> value)
    {
        using var hasher = Hasher.New();
        hasher.Update(value);
        return BinaryPrimitives.ReadUInt64LittleEndian(hasher.Finalize().AsSpan());
    }

    private enum EHeaderType
    {
        CompressedDataTable = 1,
        StreamDataTable = 2,
        Localization = 3
    }

    private enum EEncryptionType : int
    {
        CompressedAES = 2,
        XorAES = 3
    }
}
