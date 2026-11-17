using Claims.Models;
using Claims.Services;
using Microsoft.AspNetCore.Mvc;

namespace Claims.Controllers;

[ApiController]
[Route("[controller]")]
public class CoversController : ControllerBase
{
    private readonly ICoverService _coverService;

    public CoversController(ICoverService coverService)
    {
        _coverService = coverService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Cover>>> GetAsync()
    {
        var covers = await _coverService.GetAllAsync();
        return Ok(covers);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Cover>> GetAsync(string id)
    {
        var cover = await _coverService.GetByIdAsync(id);
        if (cover is null)
        {
            return NotFound();
        }

        return Ok(cover);
    }

    [HttpPost("compute")]
    public ActionResult<decimal> ComputePremium([FromQuery] DateTime startDate, [FromQuery] DateTime endDate, [FromQuery] CoverType coverType)
    {
        try
        {
            return Ok(_coverService.ComputePremium(startDate, endDate, coverType));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost]
    public async Task<ActionResult<Cover>> CreateAsync([FromBody] Cover cover)
    {
        try
        {
            var created = await _coverService.CreateAsync(cover);
            return Ok(created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAsync(string id)
    {
        await _coverService.DeleteAsync(id);
        return NoContent();
    }
}
