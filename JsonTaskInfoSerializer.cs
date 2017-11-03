using Newtonsoft.Json;

namespace Gofer.NET
{
    public class JsonTaskInfoSerializer : ITaskInfoSerializer
    {
        public string Serialize(TaskInfo taskInfo)
        {
            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All
            };
            settings.Converters.Insert(0, new JsonPrimitiveConverter());
            
            var jsonString = JsonConvert.SerializeObject(taskInfo, settings);

            return jsonString;
        }

        public TaskInfo Deserialize(string taskInfoJsonString)
        {
            if (taskInfoJsonString == null)
            {
                return null;
            }
            
            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All
            };
            settings.Converters.Insert(0, new JsonPrimitiveConverter());

            var taskInfo = JsonConvert.DeserializeObject<TaskInfo>(taskInfoJsonString, settings);
            return taskInfo;
        }
    }
}