using System;
using CUE4Parse.UE4.Assets.Exports;
using Newtonsoft.Json;

namespace CUE4Parse.GameTypes.DuneAwakening;

public class BitMapData: UObject
{
    public byte[] BitmapBytes;
    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);
        writer.WritePropertyName(nameof(BitmapBytes));
        serializer.Serialize(writer, BitmapBytes);
    }
}
