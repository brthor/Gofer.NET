using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Gofer.NET.Utils
{
    public class ExceptionConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var jo = JObject.FromObject(value as Exception, JsonSerializer.CreateDefault(new JsonSerializerSettings
            {
               TypeNameHandling = TypeNameHandling.All
            }));
            
            jo.WriteTo(writer);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override bool CanRead => false;

        public override bool CanConvert(Type objectType)
        {
            return objectType.IsSubclassOf(typeof(Exception)) || objectType == typeof(Exception);
        }
    }
}