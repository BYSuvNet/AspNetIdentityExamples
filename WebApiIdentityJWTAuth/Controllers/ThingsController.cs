using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ThingsController(AppDbContext _db) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<Thing>> CreateThing(CreateThingRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var thing = new Thing
        {
            Name = request.Name,
            Description = request.Description,
            OwnerId = userId
        };

        _db.Things.Add(thing);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetMyThings), new { id = thing.Id }, thing);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Thing>>> GetMyThings()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var things = await _db.Things
            .Where(t => t.OwnerId == userId)
            .OrderBy(t => t.Id)
            .ToListAsync();

        return Ok(things);
    }
}

public record CreateThingRequest(string Name, string Description);
