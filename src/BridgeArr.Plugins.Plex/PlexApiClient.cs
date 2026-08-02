using System.Text;
using System.Xml.Serialization;
using BridgeArr.Plugins.Plex.Models;

namespace BridgeArr.Plugins.Plex;

/// <summary>
/// HTTP client for the Plex Media Server API.
/// </summary>
internal class PlexApiClient
{
    private readonly HttpClient _httpClient;

    public PlexApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<PlexLibrarySection>> GetLibrarySectionsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("/library/sections", cancellationToken);
        response.EnsureSuccessStatusCode();

        var container = await DeserializeAsync(response, cancellationToken);
        return container?.Directories
            .Select(d => new PlexLibrarySection { Key = d.RatingKey, Title = d.Title })
            .ToList() ?? new();
    }

    public async Task<string?> SearchMovieAsync(string title, int year, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/search?query={Uri.EscapeDataString(title)}&type=1", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var container = await DeserializeAsync(response, cancellationToken);
        return container?.Videos
            .FirstOrDefault(v => v.Title.Equals(title, StringComparison.OrdinalIgnoreCase) && (year == 0 || v.Year == year))
            ?.RatingKey;
    }

    public async Task<string?> SearchSeriesAsync(string title, int year, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/search?query={Uri.EscapeDataString(title)}&type=2", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var container = await DeserializeAsync(response, cancellationToken);
        return container?.Directories
            .FirstOrDefault(d => d.Title.Equals(title, StringComparison.OrdinalIgnoreCase) && (year == 0 || d.Year == year))
            ?.RatingKey;
    }

    public async Task<IReadOnlyList<string>> GetLabelsAsync(string ratingKey, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/library/metadata/{ratingKey}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return Array.Empty<string>();
        }

        var container = await DeserializeAsync(response, cancellationToken);
        var video = container?.Videos.FirstOrDefault();
        if (video is not null)
        {
            return video.Labels.Select(label => label.Tag).ToList();
        }

        var directory = container?.Directories.FirstOrDefault();
        return directory?.Labels.Select(label => label.Tag).ToList() ?? new List<string>();
    }

    public async Task<bool> SetLabelsAsync(string ratingKey, IReadOnlyList<string> labels, CancellationToken cancellationToken = default)
    {
        var existing = await GetLabelsAsync(ratingKey, cancellationToken);
        var uriBuilder = new StringBuilder($"/library/metadata/{ratingKey}?");

        foreach (var label in existing)
        {
            uriBuilder.Append($"label[].tag.tag-={Uri.EscapeDataString(label)}&");
        }

        foreach (var label in labels)
        {
            uriBuilder.Append($"label[].tag.tag={Uri.EscapeDataString(label)}&");
        }

        var response = await _httpClient.PutAsync(uriBuilder.ToString().TrimEnd('&'), null, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> AddToCollectionAsync(string ratingKey, string collectionName, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsync(
            $"/library/metadata/{ratingKey}?collection[].tag.tag={Uri.EscapeDataString(collectionName)}",
            null,
            cancellationToken);
        return response.IsSuccessStatusCode;
    }

    private static async Task<PlexMediaContainer?> DeserializeAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var serializer = new XmlSerializer(typeof(PlexMediaContainer));
        using var reader = new StringReader(content);
        return serializer.Deserialize(reader) as PlexMediaContainer;
    }
}
