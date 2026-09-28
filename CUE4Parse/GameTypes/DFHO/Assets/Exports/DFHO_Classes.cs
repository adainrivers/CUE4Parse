using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Engine;

namespace CUE4Parse.GameTypes.DFHO.Assets.Exports;

public class UVirtualMaterialInstanceConstant : UMaterialInstanceConstant;
public class UStaticLabelMesh : UStaticMesh;
public class USMBlueprintGeneratedClass : UBlueprintGeneratedClass;
public class USMNodeBlueprintGeneratedClass : UBlueprintGeneratedClass;
public class UMultiBodyStaticMesh : UStaticMesh;
// UI sprite atlases (UIAtlas/*/BakedTexture/*) — a plain Texture2D subclass; the Paper2D
// BakedSprites (map legend / marker icons) reference it as their BakedSourceTexture.
public class UTexture2DSpriteAtlas : UTexture2D;
