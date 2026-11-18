using CUE4Parse.Encryption.Aes;
using CUE4Parse.GameTypes.Aion2.Objects;

namespace CUE4Parse.GameTypes.Aion2.Encryption.Aes;

internal static class Aion2L10NBuild26728906
{
    internal static readonly ulong PermutationRoot =
        Aion2DatFileEncryption.GetHeaderPermutationRoot(FAion2KeyManifestFile.GetVersionContext("2.0.12"));

    internal static readonly IReadOnlyDictionary<string, FAesKey> Keys = new Dictionary<string, FAesKey>
    {
        ["en-US"] = new("78f5debcdc39c81e148e7a7c4f3793c325b5133c16c8a5fab984b85b866ff911"),
        ["de-DE"] = new("2d0a2764c51a2fad325f9d8dc8d46760dfa462895c9385c0efb84ef6a7ffe15d"),
        ["fr-FR"] = new("85c60787a80efc4ababc3f61ac46a681c885878bba901d87752fb96ba861cb9e"),
        ["es-ES"] = new("f5c0da47edf9e90184a4ff9807550b034fa65e094228535d1075b705cd917d57"),
        ["pt-BR"] = new("bea2f7c29ecf8d5d8653ee0d6f56b5ddbbbfd9472d885ca0aa20af3b14a77cc9"),
        ["ru-RU"] = new("6a6baa72bd83eaf1c387413e098aee8f99103960add714efff2bb7f4f1005166"),
        ["ko-KR"] = new("604f8a119fc87567acdfbb941f6f0380f51b272a5dd7f417e877bdadffb73c6c"),
        ["ja-JP"] = new("30521c5eeab22c136cfde31d6d522f01e9e5e7fa34828a4c14a092742da1f79a"),
    };
}
