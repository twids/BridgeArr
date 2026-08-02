using System.Net;
using System.Text;
using BridgeArr.Domain.Entities;
using BridgeArr.Domain.Enums;
using BridgeArr.Plugins.Plex;
using Microsoft.Extensions.Logging.Abstractions;

namespace BridgeArr.UnitTests.Plugins;

public class PlexPluginTests
{
    private const string ValidConfig = """{"url":"http://localhost:32400","token":"test-token"}""";

    [Fact]
    public async Task TestConnectionAsync_Returns_True_When_Server_Responds_With_Success()
    {
        var plugin = CreatePlugin([(HttpStatusCode.OK, "<MediaContainer/>")]);
        Assert.True(await plugin.TestConnectionAsync(ValidConfig));
    }

    [Fact]
    public async Task TestConnectionAsync_Returns_False_When_Server_Responds_With_Error()
    {
        var plugin = CreatePlugin([(HttpStatusCode.Unauthorized, "")]);
        Assert.False(await plugin.TestConnectionAsync(ValidConfig));
    }

    [Fact]
    public async Task TestConnectionAsync_Returns_False_On_Network_Exception()
    {
        var plugin = CreatePlugin(throwException: true);
        Assert.False(await plugin.TestConnectionAsync(ValidConfig));
    }

    [Fact]
    public async Task FindMediaAsync_Returns_RatingKey_For_Movie()
    {
        var xml = """
            <MediaContainer size="1">
              <Video ratingKey="123" title="Inception" year="2010"/>
            </MediaContainer>
            """;
        var plugin = CreatePlugin([(HttpStatusCode.OK, xml)]);
        var item = new MediaItem { Title = "Inception", Year = 2010, Type = MediaType.Movie };

        var result = await plugin.FindMediaAsync(item, ValidConfig);

        Assert.Equal("123", result);
    }

    [Fact]
    public async Task FindMediaAsync_Returns_RatingKey_For_Series()
    {
        var xml = """
            <MediaContainer size="1">
              <Directory ratingKey="456" title="Breaking Bad" year="2008"/>
            </MediaContainer>
            """;
        var plugin = CreatePlugin([(HttpStatusCode.OK, xml)]);
        var item = new MediaItem { Title = "Breaking Bad", Year = 2008, Type = MediaType.Series };

        var result = await plugin.FindMediaAsync(item, ValidConfig);

        Assert.Equal("456", result);
    }

    [Fact]
    public async Task FindMediaAsync_Returns_Null_For_Unknown_MediaType()
    {
        var plugin = CreatePlugin([(HttpStatusCode.OK, "<MediaContainer/>")]);
        var item = new MediaItem { Title = "Unknown", Type = (MediaType)99 };

        var result = await plugin.FindMediaAsync(item, ValidConfig);

        Assert.Null(result);
    }

    [Fact]
    public async Task FindMediaAsync_Returns_Null_When_No_Match_Found()
    {
        var xml = """
            <MediaContainer size="1">
              <Video ratingKey="123" title="Interstellar" year="2014"/>
            </MediaContainer>
            """;
        var plugin = CreatePlugin([(HttpStatusCode.OK, xml)]);
        var item = new MediaItem { Title = "Inception", Year = 2010, Type = MediaType.Movie };

        var result = await plugin.FindMediaAsync(item, ValidConfig);

        Assert.Null(result);
    }

    [Fact]
    public async Task FindMediaAsync_Returns_Null_On_Exception()
    {
        var plugin = CreatePlugin(throwException: true);
        var item = new MediaItem { Title = "Inception", Year = 2010, Type = MediaType.Movie };

        var result = await plugin.FindMediaAsync(item, ValidConfig);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetLabelsAsync_Returns_Labels_From_Video_Element()
    {
        var xml = """
            <MediaContainer size="1">
              <Video ratingKey="123" title="Inception" year="2010">
                <Label tag="hd" id="1"/>
                <Label tag="favorite" id="2"/>
              </Video>
            </MediaContainer>
            """;
        var plugin = CreatePlugin([(HttpStatusCode.OK, xml)]);

        var result = await plugin.GetLabelsAsync("123", ValidConfig);

        Assert.Equal(["hd", "favorite"], result);
    }

    [Fact]
    public async Task GetLabelsAsync_Returns_Labels_From_Directory_Element()
    {
        var xml = """
            <MediaContainer size="1">
              <Directory ratingKey="456" title="Breaking Bad" year="2008">
                <Label tag="drama" id="1"/>
              </Directory>
            </MediaContainer>
            """;
        var plugin = CreatePlugin([(HttpStatusCode.OK, xml)]);

        var result = await plugin.GetLabelsAsync("456", ValidConfig);

        Assert.Equal(["drama"], result);
    }

    [Fact]
    public async Task GetLabelsAsync_Returns_Empty_On_Non_Success_Response()
    {
        var plugin = CreatePlugin([(HttpStatusCode.NotFound, "")]);

        var result = await plugin.GetLabelsAsync("999", ValidConfig);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetLabelsAsync_Returns_Empty_On_Exception()
    {
        var plugin = CreatePlugin(throwException: true);

        var result = await plugin.GetLabelsAsync("123", ValidConfig);

        Assert.Empty(result);
    }

    [Fact]
    public async Task SetLabelsAsync_Returns_True_On_Success()
    {
        // First response: GET existing labels; Second response: PUT new labels
        var getXml = """
            <MediaContainer size="1">
              <Video ratingKey="123" title="Inception" year="2010">
                <Label tag="old" id="1"/>
              </Video>
            </MediaContainer>
            """;
        var plugin = CreatePlugin(
        [
            (HttpStatusCode.OK, getXml),
            (HttpStatusCode.OK, "")
        ]);

        var result = await plugin.SetLabelsAsync("123", ["hd", "favorite"], ValidConfig);

        Assert.True(result);
    }

    [Fact]
    public async Task SetLabelsAsync_Returns_False_On_Non_Success_Put()
    {
        var getXml = "<MediaContainer size=\"0\"/>";
        var plugin = CreatePlugin(
        [
            (HttpStatusCode.OK, getXml),
            (HttpStatusCode.Forbidden, "")
        ]);

        var result = await plugin.SetLabelsAsync("123", ["hd"], ValidConfig);

        Assert.False(result);
    }

    [Fact]
    public async Task SetLabelsAsync_Returns_False_On_Exception()
    {
        var plugin = CreatePlugin(throwException: true);

        var result = await plugin.SetLabelsAsync("123", ["hd"], ValidConfig);

        Assert.False(result);
    }

    [Fact]
    public async Task AddToCollectionAsync_Returns_True_On_Success()
    {
        var plugin = CreatePlugin([(HttpStatusCode.OK, "")]);

        var result = await plugin.AddToCollectionAsync("123", "Best Movies", ValidConfig);

        Assert.True(result);
    }

    [Fact]
    public async Task AddToCollectionAsync_Returns_False_On_Non_Success_Response()
    {
        var plugin = CreatePlugin([(HttpStatusCode.Forbidden, "")]);

        var result = await plugin.AddToCollectionAsync("123", "Best Movies", ValidConfig);

        Assert.False(result);
    }

    [Fact]
    public async Task AddToCollectionAsync_Returns_False_On_Exception()
    {
        var plugin = CreatePlugin(throwException: true);

        var result = await plugin.AddToCollectionAsync("123", "Best Movies", ValidConfig);

        Assert.False(result);
    }

    private static PlexPlugin CreatePlugin(
        IEnumerable<(HttpStatusCode StatusCode, string Content)>? responses = null,
        bool throwException = false)
    {
        var handler = throwException
            ? new StubHttpMessageHandler()
            : new StubHttpMessageHandler(responses ?? []);
        var factory = new StubHttpClientFactory(handler);
        return new PlexPlugin(factory, NullLogger<PlexPlugin>.Instance);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<(HttpStatusCode StatusCode, string Content)> _responses = new();
        private readonly bool _throwException;

        public StubHttpMessageHandler()
        {
            _throwException = true;
        }

        public StubHttpMessageHandler(IEnumerable<(HttpStatusCode StatusCode, string Content)> responses)
        {
            foreach (var r in responses)
            {
                _responses.Enqueue(r);
            }
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_throwException)
            {
                throw new HttpRequestException("Simulated network failure.");
            }

            if (!_responses.TryDequeue(out var response))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("<MediaContainer/>", Encoding.UTF8, "application/xml")
                });
            }

            return Task.FromResult(new HttpResponseMessage(response.StatusCode)
            {
                Content = new StringContent(response.Content, Encoding.UTF8, "application/xml")
            });
        }
    }

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;

        public StubHttpClientFactory(HttpMessageHandler handler)
        {
            _handler = handler;
        }

        public HttpClient CreateClient(string name) => new(_handler) { BaseAddress = null };
    }
}
