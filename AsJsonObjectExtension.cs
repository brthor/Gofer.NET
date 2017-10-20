using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Serialization.Json;

namespace Thor.Tasks
{
    public static class AsJsonObjectExtension
    {
        public static string AsJson(this object obj)
        {
            var serializationStream = new MemoryStream();
            var serializer = new DataContractJsonSerializer(obj.GetType());
            serializer.WriteObject(serializationStream, obj);

            serializationStream.Position = 0;
            var sr = new StreamReader(serializationStream);
            var jsonString = sr.ReadToEnd();

            return jsonString;
        }

        public static T FromJson<T>(this T obj, string jsonStr) where T : class, new()
        {
            if (string.IsNullOrWhiteSpace(jsonStr))
            {
                return new T();
            }
            
            using(Stream stream = new MemoryStream()) {
                byte[] data = System.Text.Encoding.UTF8.GetBytes(jsonStr);
                stream.Write(data, 0, data.Length);
                stream.Position = 0;
                
                var deserializer = new DataContractJsonSerializer(typeof(T),
                    new DataContractJsonSerializerSettings {UseSimpleDictionaryFormat = true});
                return (T) deserializer.ReadObject(stream);
            }
        }
        
    }
}