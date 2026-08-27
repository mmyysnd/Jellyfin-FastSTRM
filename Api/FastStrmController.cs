using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FastSTRM.Api;

/// <summary>
/// API endpoints for FastSTRM probe management. Admin-only.
/// </summary>
[ApiController]
[Route("FastStrm")]
[Authorize(Policy = "RequiresElevation")]
public class FastStrmController : ControllerBase
{
    private readonly ProbeService _probeService;

    public FastStrmController(ProbeService probeService)
    {
        _probeService = probeService;
    }

    /// <summary>
    /// Returns a summary of .strm items in the library.
    /// </summary>
    [HttpGet("Summary")]
    public ActionResult GetSummary()
    {
        var (total, probed, unprobed) = _probeService.GetSummary();
        return Ok(new { Total = total, Probed = probed, Unprobed = unprobed });
    }

    /// <summary>
    /// Starts a probe task. Mode: "Incremental" or "Full".
    /// </summary>
    [HttpPost("Probe/Start")]
    public ActionResult StartProbe([FromQuery] string mode = "Incremental")
    {
        if (_probeService.StartProbe(mode))
        {
            return NoContent();
        }

        return Conflict(new { Message = "A probe task is already running." });
    }

    /// <summary>
    /// Stops the currently running probe task.
    /// </summary>
    [HttpPost("Probe/Stop")]
    public ActionResult StopProbe()
    {
        _probeService.StopProbe();
        return NoContent();
    }

    /// <summary>
    /// Returns the current probe task status and progress.
    /// </summary>
    [HttpGet("Probe/Status")]
    public ActionResult GetProbeStatus()
    {
        return Ok(new
        {
            _probeService.State,
            Processed = _probeService.ProcessedItems,
            Total = _probeService.TotalItems,
            CurrentItem = _probeService.CurrentItemName
        });
    }
}
