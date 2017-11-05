﻿using Newtonsoft.Json;

namespace Gofer.NET.Utils
{
    public class JsonTaskInfoSerializer : ITaskInfoSerializer
    {
        public string Serialize(TaskInfo taskInfo)
        {
            return Serialize((object) taskInfo);
        }
        
        public string Serialize(object obj)
        {
            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All
            };
            settings.Converters.Insert(0, new JsonPrimitiveConverter());
            
            var jsonString = JsonConvert.SerializeObject(obj, settings);

            return jsonString;
        }

        public TaskInfo Deserialize(string taskInfoJsonString)
        {
            return Deserialize<TaskInfo>(taskInfoJsonString);
        }
        
        public T Deserialize<T>(string jsonString) where T : class 
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

            var obj = JsonConvert.DeserializeObject<T>(jsonString, settings);
            return obj;
        }
    }
}