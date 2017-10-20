using System.Reflection;
using Aq.ExpressionJsonSerializer;
using Newtonsoft.Json;

namespace Thor.Tasks
{
    /// <summary>
    /// https://gist.github.com/UizzUW/945c5740c93ecbf505f789143734d22f
    /// </summary>
    public static class JsonNetAdapter
    {
        public static string Serialize<T>(T obj)
        {
            var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Objects };
            settings.Converters.Add(new ExpressionJsonConverter(Assembly.GetAssembly(typeof(T))));
            
            return JsonConvert.SerializeObject(obj, settings);
        }

        public static T Deserialize<T>(string json)
        {
            var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Objects };
            settings.Converters.Add(new ExpressionJsonConverter(Assembly.GetAssembly(typeof(T))));
            
            return JsonConvert.DeserializeObject<T>(json, settings);
        }
    }
}