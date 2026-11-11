using CUE4Parse.Encryption.Aes;

namespace CUE4Parse.GameTypes.Aion2.Encryption.Aes;

public sealed partial class Aion2DatFileEncryption
{
    // Since 2.0.11 (Steam build 26581260) the L10NString.dat keys are no longer listed in key_manifest.dat
    private static readonly Dictionary<string, FAesKey> _l10nKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        ["en-US"] = new FAesKey("38efed84543f7324fa6e8f4b84b2ec8fc8debd65f1b5ac2ef0e215d929cd63d3"),
        ["de-DE"] = new FAesKey("e02e2fb6733a8c3f8b8e483faaf3abbcfac9b4ae221f798c60257bc53f9e4db9"),
        ["fr-FR"] = new FAesKey("66bef2e5a6d2e5c17d6a036d9958450a628372ff4cf5f0dcfdea4925c1768ca9"),
        ["es-ES"] = new FAesKey("b86d99f324cb6a8fbaf165b2a52d063c900d90e484da26c2e217e2bf377d5c52"),
        ["ja-JP"] = new FAesKey("e728739cb6e930895746a8a17ae95867219799caca913c842d1ce33e2a65cd01"),
        ["ko-KR"] = new FAesKey("5cf481d8927fccaee11e778beae0cd6271c274ae673a8d5f07769465725cb40d"),
        ["pt-BR"] = new FAesKey("ec2ab058728f3ceb392e94c977e32a01dff59324ea59282b158143b53e6a6ee2"),
        ["ru-RU"] = new FAesKey("5a9db4dda4a1ddda59aab37ef9f1c75c5e56c28e6cc83ad4a827c856e78ac210")
    };

    // Older builds carry the key in the manifest, so it is tried first
    private static IEnumerable<FAesKey> GetL10NKeys(ulong seed, string locale)
    {
        if (AesKeys.TryGetValue(seed, out var manifestKey))
            yield return manifestKey;
        if (_l10nKeys.TryGetValue(locale, out var builtInKey))
            yield return builtInKey;
    }
}
