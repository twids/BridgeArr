using BridgeArr.Application.Services;
using BridgeArr.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BridgeArr.Api.Controllers;

/// <summary>
/// API endpoints for managing integrations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class IntegrationsController : ControllerBase
{
    private readonly IntegrationService _service;

    public IntegrationsController(IntegrationService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        => Ok(await _service.GetAllAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var integration = await _service.GetByIdAsync(id, cancellationToken);
        return integration is null ? NotFound() : Ok(integration);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Integration integration, CancellationToken cancellationToken)
    {
        var created = await _service.CreateAsync(integration, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] Integration integration, CancellationToken cancellationToken)
    {
        if (id != integration.Id)
        {
            return BadRequest();
        }

        return Ok(await _service.UpdateAsync(integration, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/test")]
    public async Task<IActionResult> TestConnection(Guid id, CancellationToken cancellationToken)
    {
        var success = await _service.TestConnectionAsync(id, cancellationToken);
        return Ok(new { success });
    }

    [HttpGet("plugins")]
    public IActionResult GetAvailablePlugins()
        => Ok(_service.GetAvailablePlugins().Select(p => new
        {
            p.PluginType,
            p.DisplayName,
            p.Version,
            p.Capabilities
        }));
}
