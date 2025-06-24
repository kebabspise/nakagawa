using asp_nakagawa.Data;
using asp_nakagawa.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Net.Sockets;

[Route("api/[controller]")]
[ApiController]
public class Shift_confirmsController : ControllerBase
{
    private readonly AppDBContext _context;

    public Shift_confirmsController(AppDBContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        return Ok(_context.Shift_confirms.ToList());
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var shift_confirm = await _context.Shift_confirms.FindAsync(id);
        if (shift_confirm == null) return NotFound();
        _context.Shift_confirms.Remove(shift_confirm);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost]
    public async Task<IActionResult> Create(Shift_confirm shift_confirm)
    {
        _context.Shift_confirms.Add(shift_confirm);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAll), new { id = shift_confirm.Id }, shift_confirm);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Shift_confirm shift_confirm)
    {
        if (id != shift_confirm.Id) return BadRequest();

        _context.Entry(shift_confirm).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
