using asp_nakagawa.Data;
using asp_nakagawa.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Net.Sockets;

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
    public IActionResult GetAll()
    {
        return Ok(_context.Shift_requests.ToList());
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var shift_request = await _context.Shift_requests.FindAsync(id);
        if (shift_request == null) return NotFound();

        _context.Shift_requests.Remove(shift_request);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost]
    public async Task<IActionResult> Create(Shift_request shift_request)
    {
        _context.Shift_requests.Add(shift_request);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAll), new { id = shift_request.Id }, shift_request);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Shift_request shift_request)
    {
        if (id != shift_request.Id) return BadRequest();

        _context.Entry(shift_request).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
