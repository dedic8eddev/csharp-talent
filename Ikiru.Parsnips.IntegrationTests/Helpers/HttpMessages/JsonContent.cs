using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

namespace Ikiru.Parsnips.IntegrationTests.Helpers.HttpMessages
{
    public class JsonContent : StringContent
    {
        public JsonContent(object obj)
            : base(JsonConvert.SerializeObject(obj), Encoding.UTF8, "application/json")
        {
        }
    }
}
