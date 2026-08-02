using BridgeArr.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BridgeArr.Api.Controllers;

/// <summary>
/// API endpoints for manual synchronization requests.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SyncController : ControllerBase
{
    private readonly SyncService _syncService;

    public SyncController(SyncService syncService)
    {
        _syncService = syncService;
    }

    [HttpPost]
    public async Task<IActionResult> RequestSync(
        [FromQuery] Guid sourceIntegrationId,
        [FromQuery] Guid targetIntegrationId,
        CancellationToken cancellationToken)
    {
        var job = await _syncService.RequestSyncAsync(
            sourceIntegrationId,
            targetIntegrationId,
            cancellationToken: cancellationToken);

        return Accepted(new { job.Id, job.Status });
    }
}
