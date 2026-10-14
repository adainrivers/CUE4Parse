using CUE4Parse.FileProvider;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.GameTypes.Aion2.Encryption.Aes;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.GameTypes.Aion2.Objects;

public class FAion2DataTableFile : FAion2DataFile
{
    /// <summary>Bytes left after the last row (0 = the row struct layout covers the file exactly).</summary>
    public long BytesRemaining;

    public FAion2DataTableFile(GameFile file, IFileProvider provider)
    {
        if (!file.Directory.EndsWith("Data/Table", StringComparison.OrdinalIgnoreCase)) return;

        var mappings = (provider.MappingsContainer?.MappingsForGame)
            ?? throw new ParserException("Mapping is missing, cannot deserialize");
        Read(ReadDecrypted(file, provider), file.NameWithoutExtension, mappings, provider.Versions);
    }

    /// <summary>Parses already decrypted table bytes with the given mappings (usmap repair runs this per candidate layout).</summary>
    public FAion2DataTableFile(byte[] data, string tableName, TypeMappings mappings, VersionContainer versions)
    {
        Read(data, tableName, mappings, versions);
    }

    public static byte[] ReadDecrypted(GameFile file, IFileProvider provider)
    {
        var data = file.SafeRead();
        ArgumentNullException.ThrowIfNull(data);

        if (data.Length >= 8 && BitConverter.ToUInt32(data, 0) == 13)
        {
            Aion2DatFileEncryption.Initialize(provider);
            data = Aion2DatFileEncryption.DecryptDataTable(data, file.Path);
        }

        return data;
    }

    public static string GetRowStructName(string tableName)
    {
        var name = tableName + "TableItem";
        return name switch
        {
            "AgitDecoItemTableItem" => "PersonalAgitDecoItemTableItem",
            "AgitDecoSlotTableItem" => "PersonalAgitDecoSlotTableItem",
            "ClientSliderSettingsTableItem" => "SliderSettingsTableItem",
            "EnvObjDataTableItem" => "EnvObjTableItem",
            "FieldEventPlatformerDilemmaTableItem" => "FieldEventDilemmaTableItem",
            "KiskDataTableItem" => "KiskTableItem",
            "MonsterPartsDataTableItem" => "PartsDataTableItem",
            "PeriodItemCollectionTableItem" => "PeriodCollectionTableItem",
            "PeriodItemCollectionListTableItem" => "PeriodCollectionListTableItem",
            "RandomShopTableItem" => "RandomShopCategoryTableItem",
            "ResourcesTableItem" => "ResourceTableItem",
            "RewardDataTableItem" => "RewardTableItem",
            "RewardLevelScaleDataTableItem" => "RewardLevelScaleTableItem",
            "RewardDataMonsterCubeTableItem" => "MonsterCubeTableItem",
            "SkillAbnormalTableItem" => "AbnormalTableItem",
            "SkillAbnormalEffectTableItem" => "AbnormalEffectTableItem",
            "SkillAbnormalEffectLvTableItem" => "AbnormalEffectLevelTableItem",
            "SkillAbnormalEffectTypeTableItem" => "AbnormalEffectTypeTableItem",
            "SkillAbnormalOverlapFxTableItem" => "AbnormalOverlapFxTableItem",
            "SkillAbnormalPropertyTableItem" => "AbnormalPropertyTableItem",
            "SkillAcquireDataTableItem" => "SkillAcquireTableItem",
            "SkillProjectileTableItem" => "ProjectileTableItem",
            "StringTableItem" => "AionStringTableItem",
            "ServerMessageTableItem" => "ServerBroadcastDataTableItem",
            "SeasonRankingRewardTableItem" => "SeasonRankingRewardCommonTableItem",
            "VehicleListTableItem" => "VehicleDataTableItem",
            "WaveStepDataTableItem" => "WaveStepTableItem",
            "CVResource_StartUpTableItem" => "CVResourceTableItem",
            "InputAction_StartUpTableItem" => "InputActionTableItem",
            "InputEvent_StartUpTableItem" => "InputEventTableItem",
            "InputKeyMapping_StartUpTableItem" => "InputKeyMappingTableItem",
            "PackageList_StartUpTableItem" => "PackageListTableItem",
            "ResourcePak_StartUpTableItem" => "ResourcePakTableItem",
            "String_StartUpTableItem" => "AionStringTableItem",
            _ => name
        };
    }

    /// <summary>Reads only the first row as <paramref name="structName"/> (usmap repair: prefix check while inferring a new layout).</summary>
    public static FPropertyTagType? ReadFirstRow(byte[] data, string structName, TypeMappings mappings, VersionContainer versions)
    {
        using var Ar = new FAion2DatFileArchive(data, versions);
        Ar.Read<int>();
        Ar.ReadFString();
        if (Ar.Read<int>() <= 0) return null;
        return FAion2PropertyReader.ReadPropertyTagType(Ar, mappings, "StructProperty", new FPropertyTagData(structName), false, ReadType.ARRAY);
    }

    private void Read(byte[] data, string tableName, TypeMappings mappings, VersionContainer versions)
    {
        using var Ar = new FAion2DatFileArchive(data, versions);
        Version = Ar.Read<int>();
        Ids = Ar.ReadFString().Split(",");

        var tagData = new FPropertyTagData()
        {
            Name = tableName + "Table",
            Type = "ArrayProperty",
            InnerType = "StructProperty",
            InnerTypeData = new FPropertyTagData(GetRowStructName(tableName)),
        };

        var tag = new FPropertyTag
        {
            Name = "Data",
            PropertyType = "ArrayProperty",
            Tag = FAion2PropertyReader.ReadPropertyTagType(Ar, mappings, "ArrayProperty", tagData, false, ReadType.ARRAY),
            TagData = tagData,
        };

        Properties.Add(tag);
        BytesRemaining = Ar.Length - Ar.Position;
    }
}
