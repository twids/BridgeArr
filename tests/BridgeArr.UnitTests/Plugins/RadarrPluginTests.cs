using System.Net;
using System.Text;
using BridgeArr.Domain.Enums;
using BridgeArr.Plugins.Radarr;
using Microsoft.Extensions.Logging.Abstractions;

namespace BridgeArr.UnitTests.Plugins;

public class RadarrPluginTests
{
    [Fact]
    public async Task HandleAsync_Maps_Webhook_Payload_To_MediaItem()
    {
        var plugin = new RadarrPlugin(new DummyHttpClientFactory(), NullLogger<RadarrPlugin>.Instance);
        var payload = """
            {
              "eventType": "Download",
              "movie": {
                "id": 42,
                "title": "Inception",
                "year": 2010,
                "tmdbId": 27205,
                "imdbId": "tt1375666"
              }
            }
            """;

        var result = await plugin.HandleAsync(payload, "Download");

        Assert.True(result.Success);
        Assert.NotNull(result.MediaItem);
        Assert.Equal("Inception", result.MediaItem!.Title);
        Assert.Equal(MediaType.Movie, result.MediaItem.Type);
        Assert.Contains(result.MediaItem.ExternalIds, id => id.Provider == "tmdb" && id.Value == "27205");
        Assert.Contains(result.MediaItem.ExternalIds, id => id.Provider == "imdb" && id.Value == "tt1375666");
    }

    [Fact]
    public async Task HandleAsync_Returns_Success_With_No_MediaItem_For_Test_Event()
    {
        var plugin = new RadarrPlugin(new DummyHttpClientFactory(), NullLogger<RadarrPlugin>.Instance);
        var payload = """{ "eventType": "Test" }""";

        var result = await plugin.HandleAsync(payload, "Test");

        Assert.True(result.Success);
        Assert.Null(result.MediaItem);
    }

    [Fact]
    public async Task HandleAsync_Returns_Failed_When_Json_Is_Invalid()
    {
        var plugin = new RadarrPlugin(new DummyHttpClientFactory(), NullLogger<RadarrPlugin>.Instance);

        var result = await plugin.HandleAsync("not valid json {{", "Download");

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task HandleAsync_Omits_Tmdb_When_Id_Is_Zero()
    {
        var plugin = new RadarrPlugin(new DummyHttpClientFactory(), NullLogger<RadarrPlugin>.Instance);
        var payload = """
            {
              "eventType": "Download",
              "movie": {
                "id": 1,
                "title": "Unknown",
                "year": 2020,
                "tmdbId": 0,
                "imdbId": "tt0000001"
              }
            }
            """;

        var result = await plugin.HandleAsync(payload, "Download");

        Assert.True(result.Success);
        Assert.DoesNotContain(result.MediaItem!.ExternalIds, id => id.Provider == "tmdb");
        Assert.Contains(result.MediaItem.ExternalIds, id => id.Provider == "imdb");
    }

    [Fact]
    public async Task HandleAsync_Omits_Imdb_When_Id_Is_Empty()
    {
        var plugin = new RadarrPlugin(new DummyHttpClientFactory(), NullLogger<RadarrPlugin>.Instance);
        var payload = """
            {
              "eventType": "Download",
              "movie": {
                "id": 1,
                "title": "Unknown",
                "year": 2020,
                "tmdbId": 99999,
                "imdbId": ""
              }
            }
            """;

        var result = await plugin.HandleAsync(payload, "Download");

        Assert.True(result.Success);
        Assert.Contains(result.MediaItem!.ExternalIds, id => id.Provider == "tmdb");
        Assert.DoesNotContain(result.MediaItem.ExternalIds, id => id.Provider == "imdb");
    }

    [Fact]
    public async Task HandleAsync_Sets_Year_And_Type_Movie()
    {
        var plugin = new RadarrPlugin(new DummyHttpClientFactory(), NullLogger<RadarrPlugin>.Instance);
        var payload = """
            {
              "eventType": "Grab",
              "movie": {
                "id": 5,
                "title": "The Matrix",
                "year": 1999,
                "tmdbId": 603
              }
            }
            """;

        var result = await plugin.HandleAsync(payload, "Grab");

        Assert.True(result.Success);
        Assert.Equal("The Matrix", result.MediaItem!.Title);
        Assert.Equal(1999, result.MediaItem.Year);
        Assert.Equal(MediaType.Movie, result.MediaItem.Type);
    }

    [Fact]
    public async Task TestConnectionAsync_Returns_True_On_Successful_Status_Response()
    {
        var factory = new MockHttpClientFactory(HttpStatusCode.OK, "{}");
        var plugin = new RadarrPlugin(factory, NullLogger<RadarrPlugin>.Instance);
        var config = """{"url":"http://radarr:7878","apiKey":"abc123"}""";

        var result = await plugin.TestConnectionAsync(config);

        Assert.True(result);
    }

    [Fact]
    public async Task TestConnectionAsync_Returns_False_On_Http_Error()
    {
        var factory = new MockHttpClientFactory(HttpStatusCode.Unauthorized, "Unauthorized");
        var plugin = new RadarrPlugin(factory, NullLogger<RadarrPlugin>.Instance);
        var config = """{"url":"http://radarr:7878","apiKey":"wrong"}""";

        var result = await plugin.TestConnectionAsync(config);

        Assert.False(result);
    }

    [Fact]
    public async Task TestConnectionAsync_Returns_False_On_Exception()
    {
        var factory = new ThrowingHttpClientFactory();
        var plugin = new RadarrPlugin(factory, NullLogger<RadarrPlugin>.Instance);
        var config = """{"url":"http://radarr:7878","apiKey":"abc"}""";

        var result = await plugin.TestConnectionAsync(config);

        Assert.False(result);
    }

    [Fact]
    public async Task GetAllMediaAsync_Returns_Mapped_Movies()
    {
        var moviesJson = """
            [
              {
                "id": 1,
                "title": "Inception",
                "year": 2010,
                "tmdbId": 27205,
                "imdbId": "tt1375666",
                "tags": [1],
                "genres": ["Action", "Sci-Fi"],
                "images": [
                  { "coverType": "poster", "remoteUrl": "https://example.com/poster.jpg" }
                ]
              }
            ]
            """;
        var tagsJson = """[{ "id": 1, "label": "hd" }]""";

        var factory = new SequencedMockHttpClientFactory(
            (HttpStatusCode.OK, moviesJson),
            (HttpStatusCode.OK, tagsJson));
        var plugin = new RadarrPlugin(factory, NullLogger<RadarrPlugin>.Instance);
        var config = """{"url":"http://radarr:7878","apiKey":"abc123"}""";

        var items = await plugin.GetAllMediaAsync(config);

        Assert.Single(items);
        Assert.Equal("Inception", items[0].Title);
        Assert.Equal(2010, items[0].Year);
        Assert.Equal(MediaType.Movie, items[0].Type);
        Assert.Contains(items[0].ExternalIds, id => id.Provider == "tmdb" && id.Value == "27205");
        Assert.Contains(items[0].Tags, t => t.Name == "hd");
        Assert.Contains(items[0].Genres, g => g == "Action");
        Assert.Contains(items[0].Artwork, a => a.Type == BridgeArr.Domain.Enums.ArtworkType.Poster);
    }

    [Fact]
    public async Task GetMediaByIdAsync_Returns_Null_For_NonNumeric_SourceId()
    {
        var plugin = new RadarrPlugin(new DummyHttpClientFactory(), NullLogger<RadarrPlugin>.Instance);

        var result = await plugin.GetMediaByIdAsync("not-a-number", "{}");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetMediaByIdAsync_Returns_Null_When_Movie_Not_Found()
    {
        var factory = new MockHttpClientFactory(HttpStatusCode.NotFound, "");
        var plugin = new RadarrPlugin(factory, NullLogger<RadarrPlugin>.Instance);
        var config = """{"url":"http://radarr:7878","apiKey":"abc123"}""";

        var result = await plugin.GetMediaByIdAsync("999", config);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetMediaByIdAsync_Returns_Mapped_Movie_When_Found()
    {
        var movieJson = """
            {
              "id": 42,
              "title": "Interstellar",
              "year": 2014,
              "tmdbId": 157336,
              "imdbId": "tt0816692",
              "tags": [],
              "genres": ["Drama", "Sci-Fi"],
              "images": []
            }
            """;
        var tagsJson = "[]";

        var factory = new SequencedMockHttpClientFactory(
            (HttpStatusCode.OK, movieJson),
            (HttpStatusCode.OK, tagsJson));
        var plugin = new RadarrPlugin(factory, NullLogger<RadarrPlugin>.Instance);
        var config = """{"url":"http://radarr:7878","apiKey":"abc123"}""";

        var result = await plugin.GetMediaByIdAsync("42", config);

        Assert.NotNull(result);
        Assert.Equal("Interstellar", result!.Title);
        Assert.Equal(2014, result.Year);
        Assert.Equal(MediaType.Movie, result.Type);
        Assert.Contains(result.ExternalIds, id => id.Provider == "tmdb" && id.Value == "157336");
        Assert.Contains(result.ExternalIds, id => id.Provider == "imdb" && id.Value == "tt0816692");
    }

    [Fact]
    public async Task GetTagsAsync_Returns_Mapped_Tag_Names()
    {
        var tagsJson = """
            [
              { "id": 1, "label": "hd" },
              { "id": 2, "label": "favorite" }
            ]
            """;
        var factory = new MockHttpClientFactory(HttpStatusCode.OK, tagsJson);
        var plugin = new RadarrPlugin(factory, NullLogger<RadarrPlugin>.Instance);
        var config = """{"url":"http://radarr:7878","apiKey":"abc123"}""";

        var tags = await plugin.GetTagsAsync(config);

        Assert.Equal(2, tags.Count);
        Assert.Contains(tags, t => t.Name == "hd");
        Assert.Contains(tags, t => t.Name == "favorite");
    }

    private sealed class DummyHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }

    /// <summary>
    /// Returns a fixed response to every HTTP request.
    /// </summary>
    private sealed class MockHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _content;

        public MockHttpClientFactory(HttpStatusCode statusCode, string content)
        {
            _statusCode = statusCode;
            _content = content;
        }

        public HttpClient CreateClient(string name)
            => new(new FixedResponseHandler(_statusCode, _content))
            {
                BaseAddress = new Uri("http://radarr:7878/")
            };

        private sealed class FixedResponseHandler : HttpMessageHandler
        {
            private readonly HttpStatusCode _statusCode;
            private readonly string _content;

            public FixedResponseHandler(HttpStatusCode statusCode, string content)
            {
                _statusCode = statusCode;
                _content = content;
            }

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
                => Task.FromResult(new HttpResponseMessage(_statusCode)
                {
                    Content = new StringContent(_content, Encoding.UTF8, "application/json")
                });
        }
    }

    /// <summary>
    /// Returns responses from a preset queue — first request gets the first response, and so on.
    /// </summary>
    private sealed class SequencedMockHttpClientFactory : IHttpClientFactory
    {
        private readonly Queue<(HttpStatusCode Status, string Content)> _responses;

        public SequencedMockHttpClientFactory(params (HttpStatusCode, string)[] responses)
        {
            _responses = new Queue<(HttpStatusCode, string)>(responses);
        }

        public HttpClient CreateClient(string name)
            => new(new SequencedHandler(_responses))
            {
                BaseAddress = new Uri("http://radarr:7878/")
            };

        private sealed class SequencedHandler : HttpMessageHandler
        {
            private readonly Queue<(HttpStatusCode Status, string Content)> _responses;

            public SequencedHandler(Queue<(HttpStatusCode Status, string Content)> responses)
            {
                _responses = responses;
            }

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                var (status, content) = _responses.Dequeue();
                return Task.FromResult(new HttpResponseMessage(status)
                {
                    Content = new StringContent(content, Encoding.UTF8, "application/json")
                });
            }
        }
    }

    /// <summary>
    /// Always throws an <see cref="HttpRequestException"/> to simulate network failures.
    /// </summary>
    private sealed class ThrowingHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
            => new(new ThrowingHandler())
            {
                BaseAddress = new Uri("http://radarr:7878/")
            };

        private sealed class ThrowingHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
                => throw new HttpRequestException("Connection refused.");
        }
    }
}
