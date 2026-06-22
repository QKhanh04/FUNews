using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FUNewsManagement.Services
{
    public class ODataResponse<T>
    {
        [JsonPropertyName("value")]
        public List<T> Value { get; set; } = new();

        [JsonPropertyName("@odata.count")]
        public int? Count { get; set; }
    }
}
