using System.ComponentModel;

namespace CUE4Parse_Conversion.Animations;

public enum EAnimFormat
{
    [Description("ActorX (psa)")]
    ActorX,
    [Description("UEFormat (ueanim)")]
    UEFormat,
    [Description("glTF 2.0 (glb)")]
    Gltf2
}