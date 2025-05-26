using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ThunderRoad;

namespace GhettosFirearmSDKv2;

public class CartridgeSaveData : ContentCustomData
{
    public string ItemId;
    public bool IsFired;
    public bool Failed;

    public CartridgeSaveData(string itemId, bool isFired, bool failed)
    {
        ItemId = itemId;
        IsFired = isFired;
        Failed = failed;
    }

    public void Apply(Cartridge cartridge)
    {
        if (Failed)
        {
            cartridge.Failed = true;
        }
        if (IsFired)
        {
            cartridge.SetFired();
        }
    }

    public static implicit operator CartridgeSaveData(string item)
    {
        return new CartridgeSaveData(item, false, false);
    }

    public class StringArrayToDataArrayConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(string[]);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);

            if (token.Type == JTokenType.Array && token.First?.Type == JTokenType.String)
            {
                var stringArray = token.ToObject<string[]>();
                return Array.ConvertAll(stringArray, item => new CartridgeSaveData(item, false, false));
            }

            return token.ToObject<CartridgeSaveData[]>();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }
}