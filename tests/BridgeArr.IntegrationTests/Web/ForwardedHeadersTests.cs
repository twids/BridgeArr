using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace BridgeArr.IntegrationTests.Web;

public sealed class ForwardedHeadersTests : IClassFixture<WebApplicationFactory<BridgeArr.Web.Program>>
{
    private readonly WebApplicationFactory<BridgeArr.Web.Program> _factory;

    public ForwardedHeadersTests(WebApplicationFactory<BridgeArr.Web.Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["BRIDGEARR_TRUSTED_PROXY_NETWORKS"] = "127.0.0.0/8"
                });
            });
            builder.ConfigureServices(services => services.RemoveAll<IHostedService>());
        });
    }

    [Fact]
    public async Task Root_TrustedForwardedHttps_RedirectsToHttpsLogin()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("http://bridgearr.widsell.nu")
        });
        client.DefaultRequestHeaders.Add("X-Forwarded-Proto", "https");

        using var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(
            "https://bridgearr.widsell.nu/account/login?ReturnUrl=%2F",
            response.Headers.Location?.AbsoluteUri);
    }
}