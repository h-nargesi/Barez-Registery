using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;

namespace Photon.Barez;

static class JsonHandler
{
    public static string SerializeJson(this object obj)
    {
        return JsonConvert.SerializeObject(obj, new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        });
    }

    public static T DeserializeJson<T>(this string obj) where T : struct
    {
        return JsonConvert.DeserializeObject<T>(obj, new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        });
    }

    public static T LoadFromFile<T>(string path) where T : struct
    {
        var data = File.ReadAllText(path);
        return data.DeserializeJson<T>();
    }
}