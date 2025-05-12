using LibraryAPI.Data;
using LibraryAPI.Model;
using LibraryAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryAPI;

[ApiController]
[Route("[controller]")]
public class LoanController(ApplicationDbContext db) : ControllerBase
{
    
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Loan>>> Get()
    {
        var loans = await db.Loans.ToListAsync();
        return Ok(loans);
    }

    [HttpGet("{username}")]
    public async Task<ActionResult<IEnumerable<Loan>>> GetByUserName(string username)
    {
        var loans = await db.Loans.Where(l => l.User == username).ToListAsync();
        return Ok(loans);
    }

    [HttpPut("{id}/return")]
    public async Task<ActionResult> ReturnLoan(int id)
    {
        var loan = await db.Loans.FindAsync(id);
        loan.Return = true;
        await db.SaveChangesAsync();
        return Ok();
    }

}