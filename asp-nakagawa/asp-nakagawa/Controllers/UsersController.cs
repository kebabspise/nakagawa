using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using asp_nakagawa.Models;
using asp_nakagawa.Data;

[Route("api/[controller]")]
[ApiController]
public class UsersController : ControllerBase 
{
    private readonly AppDBContext _context;

    public UsersController(AppDBContext context)
    {
        _context = context;
    }

    [HttpGet("by-userid/{user_id}")] 
    public async Task<IActionResult> GetByUserId(int user_id)
    { 
        var user = await _context.Users.FirstOrDefaultAsync(u => u.user_id == user_id);
        if (user == null) return NotFound();
        return Ok(user);
    }

    [HttpDelete("by-userid/{user_id}")]
    public async Task<IActionResult> DeleteByUserId(int user_id)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.user_id == user_id);
        if (user == null) return NotFound();

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost]
    public async Task<IActionResult> Create(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                     
        return CreatedAtAction(nameof(GetByUserId), new {  user_id = user.id }, user);
    }

    [HttpPut("by-userid/{user_id}")]
    public async Task<IActionResult> UpdateByUserId(int user_id, User updated)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.user_id == user_id);
        if (user == null) return NotFound();

        user.name = updated.name;
        user.pass = updated.pass;
        user.admin = updated.admin;
        user.wages = updated.wages;

        await _context.SaveChangesAsync();
        return NoContent();
    }

}
