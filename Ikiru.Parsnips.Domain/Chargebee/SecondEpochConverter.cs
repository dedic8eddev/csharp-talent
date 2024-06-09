using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace Ikiru.Parsnips.Domain.Chargebee
{
    public class SecondEpochConverter : DateTimeConverterBase
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null) 
                return;

            writer.WriteRawValue(((DateTimeOffset)value).ToUnixTimeSeconds().ToString());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.Value == null)
                return null;

            return DateTimeOffset.FromUnixTimeSeconds((long)reader.Value);
        }
    }
}
