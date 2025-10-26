using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace KeywordWatcher.ApeWisdom
{
    internal class ApeWisdomClient
    {
        HttpClient httpClient;
        const string apiEndpoint = "https://apewisdom.io/api/v1.0/filter/all-stocks";
        public ApeWisdomClient(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        public async Task<JsonElement> GetData()
        {
            var response = await httpClient.GetAsync(apiEndpoint);
            using var stream = await response.Content.ReadAsStreamAsync();
            var json = JsonSerializer.Deserialize<JsonElement>(stream);
            return json;
        }
    }
}
