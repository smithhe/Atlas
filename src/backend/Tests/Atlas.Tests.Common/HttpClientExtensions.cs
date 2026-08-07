using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atlas.Tests.Common;

public static class HttpClientExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static Task<HttpResponseMessage> PostJsonAsync<T>(this HttpClient client, string uri, T payload) =>
        client.PostAsJsonAsync(uri, payload, JsonOptions);

    public static Task<HttpResponseMessage> PostEmptyJsonAsync(this HttpClient client, string uri) =>
        client.PostAsync(uri, new StringContent(string.Empty, Encoding.UTF8, "application/json"));

    public static Task<HttpResponseMessage> PutJsonAsync<T>(this HttpClient client, string uri, T payload) =>
        client.PutAsJsonAsync(uri, payload, JsonOptions);

    public static async Task<T?> ReadJsonAsync<T>(this HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<T>(JsonOptions);
}
