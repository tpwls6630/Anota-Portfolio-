using System;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class TileObjectConverter : JsonConverter<TileObject>
{
    public override void WriteJson(JsonWriter writer, TileObject value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("TileType");
        serializer.Serialize(writer, value.TileType);

        if (value.TileType != TileType.None)
        {
            writer.WritePropertyName("ObjectType");
            serializer.Serialize(writer, value.ObjectType);

            if (value.ObjectType != ObjectType.None && value.ObjectType != ObjectType.Box)
            {
                writer.WritePropertyName("ObjectData");
                writer.WriteValue(value.ObjectData);
            }
        }

        writer.WriteEndObject();
    }

    public override TileObject ReadJson(JsonReader reader, Type objectType, TileObject existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        TileObject tileObject = new TileObject();
        JObject obj = JObject.Load(reader);

        tileObject.TileType = obj["TileType"].ToObject<TileType>(serializer);

        if (tileObject.TileType != TileType.None)
        {
            tileObject.ObjectType = obj["ObjectType"].ToObject<ObjectType>(serializer);

            if (tileObject.ObjectType != ObjectType.None && tileObject.ObjectType != ObjectType.Box)
            {
                tileObject.ObjectData = (int)obj["ObjectData"];
            }
        }

        return tileObject;
    }
}