using System.Runtime.InteropServices;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.GameTypes.Aion2.Objects;
using CUE4Parse.UE4.Exceptions;
using GenericReader;
using K4os.Compression.LZ4;

namespace CUE4Parse.GameTypes.Aion2.Encryption.Aes;

public sealed partial class Aion2DatFileEncryption
{
    public static byte[] DecryptL10N(byte[] dat, string filePath) => DecryptL10N(dat, filePath, null);

    internal static byte[] DecryptL10N(byte[] dat, string filePath, IFileProvider? provider)
    {
        using var Ar = new GenericBufferReader(dat);
        if (Ar.Length < 0x14 || Ar.Read<int>() != 2)
            throw new ParserException("AION2 L10N container is too small");

        var locale = GetL10NLocale(filePath);
        var seed = Blake3Hash($"L10NString_{locale}");

        if (Aion2L10NBuild26728906.Keys.TryGetValue(locale, out var buildKey))
        {
            var permutationRoot = Aion2L10NBuild26728906.PermutationRoot;
            var candidate = new FL10NHeader(Ar, seed, Blake3Hash(permutationRoot), permutationRoot);
            if (candidate.EncryptionType == EEncryptionType.CompressedAES &&
                candidate.HasValidSizes(Ar.Length - Ar.Position) &&
                DecodePayload(Ar, candidate, buildKey) is { } payload)
                return payload;

            // Other client versions or a hotfix with a different key use the mounted manifest.
            Ar.Position = sizeof(int);
        }

        if (provider is not null)
            Initialize(provider);

        var header = new FL10NHeader(Ar, seed, _headerKeyRoot, _useHeaderPermutation ? _headerPermutationRoot : null);
        if (header.EncryptionType != EEncryptionType.CompressedAES)
            throw new ParserException($"Unsupported AION2 L10N encryption type '{header.EncryptionType}'");
        if (!header.HasValidSizes(Ar.Length - Ar.Position))
            throw new ParserException("Invalid AION2 L10N payload sizes");
        if (!AesKeys.TryGetValue(seed, out var key))
            throw new ParserException("AION2 L10N encryption key not found in manifest");

        return DecodePayload(Ar, header, key) ?? throw new ParserException("AION2 L10N LZ4 decode failed");
    }

    private static byte[]? DecodePayload(GenericBufferReader Ar, FL10NHeader header, FAesKey key)
    {
        var encryptedMemory = Ar.ReadMemory(header.AlignedSize);
        if (!MemoryMarshal.TryGetArray(encryptedMemory, out ArraySegment<byte> encrypted))
            throw new ParserException("AION2 L10N payload is not backed by an array");

        var decrypted = encrypted.Decrypt(key);
        var output = new byte[header.RawSize];
        var written = LZ4Codec.Decode(decrypted.AsSpan(0x20, header.PackedSize), output);
        return written == output.Length ? output : null;
    }

    private static string GetL10NLocale(string filePath)
    {
        var parts = filePath.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
            return parts[^2];

        throw new ParserException($"Unable to derive AION2 L10N locale from '{filePath}'");
    }

    private readonly struct FL10NHeader
    {
        public readonly int PackedSize;
        public readonly EEncryptionType EncryptionType;
        public readonly int AlignedSize;
        public readonly int RawSize;

        public bool HasValidSizes(long remaining) =>
            AlignedSize > 0 && AlignedSize <= remaining && AlignedSize % 16 == 0 &&
            PackedSize > 0 && PackedSize <= AlignedSize - 0x20 &&
            RawSize > 0 && RawSize <= 255L * PackedSize;

        public FL10NHeader(GenericBufferReader Ar, ulong seed, ulong keyRoot, ulong? permutationRoot)
        {
            var data = Ar.ReadArray<byte>(0x10);
            FAion2DatFileArchive.DecryptData(data, DeriveHeaderKey(seed, EHeaderType.Localization, keyRoot));

            using var headerAr = new GenericBufferReader(data);
            if (permutationRoot is null)
            {
                PackedSize = headerAr.Read<int>();
                EncryptionType = headerAr.Read<EEncryptionType>();
                AlignedSize = headerAr.Read<int>();
                RawSize = headerAr.Read<int>();
            }
            else
            {
                Span<byte> permutation = stackalloc byte[4];
                GetHeaderPermutation(permutation, permutationRoot.Value);
                Span<int> fields = stackalloc int[4];
                for (var i = 0; i < 4; i++) fields[permutation[i]] = headerAr.Read<int>();
                EncryptionType = (EEncryptionType) fields[0];
                RawSize = fields[1];
                PackedSize = fields[2];
                AlignedSize = fields[3];
            }
        }
    }
}
