using System;
using Newtonsoft.Json;

namespace Gofer.NET.Utils
{
    public static class JsonTaskInfoSerializer
    {
        public static string Serialize(TaskInfo taskInfo)
        {
            return Serialize((object) taskInfo);
        }

        public static string Serialize(object obj)
        {
            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All
            };

            settings.Converters.Insert(0, new JsonPrimitiveConverter());
            settings.Converters.Insert(1, new ExceptionConverter());
            
            var jsonString = JsonConvert.SerializeObject(obj, settings);

            return jsonString;
        }

        public static TaskInfo Deserialize(string taskInfoJsonString)
        {
            return Deserialize<TaskInfo>(taskInfoJsonString);
        }
        
        public static T Deserialize<T>(string jsonString) where T : class 
        {
            if (jsonString == null)
            {
                return null;
            }
            
            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All
            };
            settings.Converters.Insert(0, new JsonPrimitiveConverter());
            settings.Converters.Insert(1, new ExceptionConverter());

            var obj = JsonConvert.DeserializeObject<T>(jsonString, settings);
            return obj;
        }
    }
}