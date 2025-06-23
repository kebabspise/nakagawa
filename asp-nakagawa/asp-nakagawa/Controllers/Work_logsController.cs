using asp_nakagawa.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using asp_nakagawa.Data;

[Route("api/[controller]")]
[ApiController]
public class Work_logsController:ControllerBase
{
    private readonly AppDBContext _context;

     public Work_logsController(AppDBContext context)
     {
         _context = context;
     }

     [HttpGet]
     public IActionResult GetAll()
     {
         return Ok(_context.Work_logs.ToList());
     }

     [HttpDelete("{id}")]
     public async Task<IActionResult> Delete(int id)
     {
         var work_log = await _context.Work_logs.FindAsync(id);
         if (work_log == null) return NotFound();

         _context.Work_logs.Remove(work_log);
         await _context.SaveChangesAsync();
         return NoContent();
     }

     [HttpPost]
     public async Task<IActionResult> Create(Work_log work_log)
     {
         _context.Work_logs.Add(work_log);
         await _context.SaveChangesAsync();
         return CreatedAtAction(nameof(GetAll), new { id = work_log.Id }, work_log);
     }

     [HttpPut("{id}")]
     public async Task<IActionResult> Update(int id, Work_log work_log)
     {
         if (id != work_log.Id) return BadRequest();

         _context.Entry(work_log).State = EntityState.Modified;
         await _context.SaveChangesAsync();
         return NoContent();
     }
}
