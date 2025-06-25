using asp_nakagawa.Data;
using asp_nakagawa.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("api/[controller]")]
[ApiController]
public class Shift_requestsController : ControllerBase
{
    private readonly AppDBContext _context;

    public Shift_requestsController(AppDBContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var requests = await _context.Shift_requests.ToListAsync();
        return Ok(requests);
    }

    // GET: api/Shift_requests/5
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var shiftRequest = await _context.Shift_requests.FindAsync(id);
        if (shiftRequest == null) return NotFound();

        return Ok(shiftRequest);
    }

    // ユーザーごとのシフト申請一覧取得
    [HttpGet("by-userid/{user_id}")]
    public async Task<IActionResult> GetByUserId(int user_id)
    {
        var list = await _context.Shift_requests
                                 .Where(sr => sr.user_id == user_id)
                                 .ToListAsync();
        return Ok(list);
    }

    // DELETE: api/Shift_requests/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var shiftRequest = await _context.Shift_requests.FindAsync(id);
        if (shiftRequest == null) return NotFound();

        _context.Shift_requests.Remove(shiftRequest);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // POST: api/Shift_requests
    [HttpPost]
    public async Task<IActionResult> Create(Shift_request shiftRequest)
    {
        _context.Shift_requests.Add(shiftRequest);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = shiftRequest.id }, shiftRequest);
    }

    // PUT: api/Shift_requests/5
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Shift_request shiftRequest)
    {
        if (id != shiftRequest.id) return BadRequest();

        _context.Entry(shiftRequest).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.Shift_requests.Any(e => e.id == id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }
}
