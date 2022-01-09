using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace TodayInDestiny2.Tasks;

internal class BungieService : IDisposable
{
    private readonly HttpClient httpClient;
    private readonly HttpClient httpClientWithApiKey;

    private Task<JsonDocument>? getManifestTask;
    private object getManifestLock = new Object();

    private bool isDisposed;

    public BungieService(string apiKey)
    {
        httpClient = new HttpClient();
        httpClientWithApiKey = new HttpClient();
        httpClientWithApiKey.DefaultRequestHeaders.Add("X-API-Key", apiKey);
    }

    public async Task<JsonDocument> GetCurrentActivitiesAsync(string membershipType, string membershipId)
    {
        Console.WriteLine("Fetching current activities...");
        string uri = $"/Platform/Destiny2/{membershipType}/Profile/{membershipId}/?components=204";
        return await GetJsonDocAsync(uri, includeApiKey: true);
    }

    public async Task<JsonDocument> GetModifiersAsync()
    {
        Console.WriteLine("Downloading modifier definitions...");
        return await GetDefinitionsAsync("DestinyActivityModifierDefinition");
    }

    private async Task<JsonDocument> GetDefinitionsAsync(string componentName)
    {
        var manifestResponse = await GetManifestAsync();
        if (manifestResponse.TryGetPropertyChain(out var componentUri,
            "Response", "jsonWorldComponentContentPaths", "en", componentName))
        {
            string? uri = componentUri.GetString();
            if (uri != null)
            {
                return await GetJsonDocAsync(uri);
            }
        }

        throw new Exception($"Unable to retrieve {componentName} definitions.");
    }

    private Task<JsonDocument> GetManifestAsync()
    {
        lock (getManifestLock)
        {
            if (getManifestTask == null)
            {
                getManifestTask = GetManifestAsyncInternal();
            }
            return getManifestTask;
        }
    }

    private async Task<JsonDocument> GetManifestAsyncInternal()
    {
        Console.WriteLine("Downloading manifest...");
        return await GetJsonDocAsync("/Platform/Destiny2/Manifest");
    }

    private async Task<JsonDocument> GetJsonDocAsync(string bungieRelativeUri, bool includeApiKey = false)
    {
        var client = includeApiKey ? httpClientWithApiKey : httpClient;

        string uri = $"https://www.bungie.net{bungieRelativeUri}";
        var stream = await client.GetStreamAsync(uri);
        return await JsonDocument.ParseAsync(stream);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!isDisposed)
        {
            if (disposing)
            {
                httpClient.Dispose();
                httpClientWithApiKey.Dispose();
            }
            isDisposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}