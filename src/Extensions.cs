using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;

namespace Photon.Barez;

static class Extensions
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
}