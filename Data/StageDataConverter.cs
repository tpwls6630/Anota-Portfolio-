using System;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class StageDataConverter : JsonConverter<StageData>
{
    public override void WriteJson(JsonWriter writer, StageData value, JsonSerializer serializer)
    {
        JObject obj = new JObject
        {
            ["ChapterNumber"] = value.ChapterNumber,
            ["StageNumber"] = value.StageNumber,
            ["ExecutionTurn"] = value.ExecutionTurn,
            ["ExecutionRepeat"] = value.ExecutionRepeat,
            ["ClearAnimation"] = value.ClearAnimation
        };

        JObject tileDictionary = new JObject();
        foreach (var item in value.TileDictionary)
        {
            string key = $"{item.Key.x}, {item.Key.y}";
            tileDictionary[key] = JToken.FromObject(item.Value, serializer);
        }

        obj["TileDictionary"] = tileDictionary;

        obj.WriteTo(writer);
    }

    public override StageData ReadJson(JsonReader reader, Type objectType, StageData existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        JObject obj = JObject.Load(reader);
        StageData stageData = new();

        stageData.ChapterNumber = obj["ChapterNumber"]?.ToObject<int>() ?? 0;
        stageData.StageNumber = obj["StageNumber"]?.ToObject<int>() ?? 0;
        stageData.ExecutionTurn = obj["ExecutionTurn"]?.ToObject<int>() ?? 0;
        stageData.ExecutionRepeat = obj["ExecutionRepeat"]?.ToObject<bool>() ?? false;
        stageData.ClearAnimation = obj["ClearAnimation"]?.ToObject<bool>() ?? false;

        JObject tileDictionary = (JObject)obj["TileDictionary"];
        foreach (var item in tileDictionary.Properties())
        {
            string[] coordinates = item.Name.Split(new[] { ',' });
            Vector2Int key = new Vector2Int(int.Parse(coordinates[0]), int.Parse(coordinates[1]));
            TileObject value = item.Value.ToObject<TileObject>(serializer);
            stageData.TileDictionary.Add(key, value);
        }

        return stageData;
    }
}