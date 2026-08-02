using BridgeArr.Api.Controllers;
using BridgeArr.Application.Services;
using BridgeArr.Infrastructure.BackgroundServices;
using BridgeArr.Web.Components.Pages;

namespace BridgeArr.IntegrationTests.Architecture;

public class ProjectCompositionTests
{
    [Fact]
    public void Solution_Contains_Primary_Web_And_Api_Assets()
    {
        Assert.NotNull(typeof(IntegrationsController));
        Assert.NotNull(typeof(SyncService));
        Assert.NotNull(typeof(SyncWorker));
        Assert.NotNull(typeof(Home));
    }
}
