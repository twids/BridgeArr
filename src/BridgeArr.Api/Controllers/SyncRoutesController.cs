using BridgeArr.Application.Interfaces;
using BridgeArr.Application.Services;
using BridgeArr.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BridgeArr.Api.Controllers;

/// <summary>API endpoints for user-configured synchronization routes.</summary>
[ApiController]
[Route("api/sync-routes")]
[Authorize]
public class SyncRoutesController : ControllerBase
{
    private readonly SyncRouteService _service;
    private readonly ISyncRouteRepository _repository;

    public SyncRoutesController(SyncRouteService service, ISyncRouteRepository repository)
    {
        _service = service;
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SyncRoute>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await _service.GetAllAsync(cancellationToken));

    [HttpPost]
    public async Task<ActionResult<SyncRoute>> Create(SyncRouteRequest request, CancellationToken cancellationToken)
    {
        var route = await _service.CreateAsync(request.ToEntity(), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = route.Id }, route);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SyncRoute>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var route = await _repository.GetByIdAsync(id, cancellationToken);
        return route is null ? NotFound() : Ok(route);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SyncRoute>> Update(Guid id, SyncRouteRequest request, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByIdAsync(id, cancellationToken);
        if (existing is null) return NotFound();
        existing.Name = request.Name;
        existing.SourceIntegrationId = request.SourceIntegrationId;
        existing.TargetIntegrationId = request.TargetIntegrationId;
        existing.IntervalMinutes = request.IntervalMinutes;
        existing.Enabled = request.Enabled;
        return Ok(await _service.UpdateAsync(existing, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (await _repository.GetByIdAsync(id, cancellationToken) is null) return NotFound();
        await _service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/run")]
    public async Task<IActionResult> Run(Guid id, CancellationToken cancellationToken)
    {
        var route = await _repository.GetByIdAsync(id, cancellationToken);
        if (route is null) return NotFound();
        var job = await _service.QueueAsync(route, cancellationToken: cancellationToken);
        return job is null ? Conflict(new { message = "A matching sync job is already active." }) : Accepted(new { job.Id, job.Status });
    }
}

public sealed record SyncRouteRequest(string Name, Guid SourceIntegrationId, Guid TargetIntegrationId, int IntervalMinutes, bool Enabled)
{
    public SyncRoute ToEntity() => new()
    {
        Name = Name,
        SourceIntegrationId = SourceIntegrationId,
        TargetIntegrationId = TargetIntegrationId,
        IntervalMinutes = IntervalMinutes,
        Enabled = Enabled
    };
}
