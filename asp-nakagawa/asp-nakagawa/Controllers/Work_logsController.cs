using asp_nakagawa.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using asp_nakagawa.Data;

[Route("api/[controller]")]
[ApiController]
public class Work_logsController : ControllerBase
{
    private readonly AppDBContext _context;

    public Work_logsController(AppDBContext context)
    {
        _context = context;
    }

 
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var logs = await _context.Work_logs.ToListAsync();
        return Ok(logs);
    }


    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var log = await _context.Work_logs.FindAsync(id);
        if (log == null) return NotFound();
        return Ok(log);
    }

    // ユーザーごとの勤務記録取得（オプション）
    [HttpGet("by-userid/{user_id}")]
    public async Task<IActionResult> GetByUserId(int user_id)
    {
        var logs = await _context.Work_logs
                                 .Where(w => w.user_id == user_id)
                                 .ToListAsync();
        return Ok(logs);
    }


    [HttpPost]
    public async Task<IActionResult> Create(Work_log work_log)
    {
        if (work_log.work_start.HasValue)
        {
            work_log.work_date = work_log.work_start.Value.Date;
        }
        _context.Work_logs.Add(work_log);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = work_log.id }, work_log);
    }


    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Work_log work_log)
    {
        if (id != work_log.id) return BadRequest();

        if (work_log.work_start.HasValue)
        {
            work_log.work_date = work_log.work_start.Value.Date;
        }

        _context.Entry(work_log).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.Work_logs.Any(e => e.id == id))
                return NotFound();
            else
                throw;
        }

        return NoContent();
    }

 
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var log = await _context.Work_logs.FindAsync(id);
        if (log == null) return NotFound();

        _context.Work_logs.Remove(log);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}