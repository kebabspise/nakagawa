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

    // 全件取得（非同期）
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var confirms = await _context.Shift_confirms.ToListAsync();
        return Ok(confirms);
    }

    // 個別取得（追加）
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var confirm = await _context.Shift_confirms.FindAsync(id);
        if (confirm == null) return NotFound();
        return Ok(confirm);
    }

    [HttpGet("by-userid/{user_id}")]
    public async Task<IActionResult> GetByUserId(int user_id)
    {
        var list = await _context.Shift_confirms
                                 .Where(sc => sc.user_id == user_id)
                                 .ToListAsync();
        return Ok(list);
    }

    // 作成
    [HttpPost]
    public async Task<IActionResult> Create(Shift_confirm shift_confirm)
    {
        _context.Shift_confirms.Add(shift_confirm);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = shift_confirm.id }, shift_confirm);
    }

    // 更新
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Shift_confirm shift_confirm)
    {
        if (id != shift_confirm.id) return BadRequest();

        _context.Entry(shift_confirm).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.Shift_confirms.Any(e => e.id == id))
                return NotFound();
            else
                throw;
        }

        return NoContent();
    }

    // 削除
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var confirm = await _context.Shift_confirms.FindAsync(id);
        if (confirm == null) return NotFound();

        _context.Shift_confirms.Remove(confirm);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
