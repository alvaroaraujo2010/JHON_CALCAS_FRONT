using System.Security.Claims;
using ContaNexo.API.Data;
using ContaNexo.API.DTOs;
using ContaNexo.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContaNexo.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrador,Contador")]
public class AccountingController(AppDbContext db) : ControllerBase
{
    [HttpGet("accounts")]
    public async Task<ActionResult<List<AccountDto>>> GetAccounts()
    {
        var list = await db.Accounts.Where(a => a.IsActive).OrderBy(a => a.Code).ToListAsync();
        return Ok(list.Select(a => new AccountDto(a.Id, a.Code, a.Name, a.Type.ToString(), a.ParentId, a.IsActive)).ToList());
    }

    [HttpPost("accounts")]
    [Authorize(Roles = "Administrador")]
    public async Task<ActionResult<AccountDto>> CreateAccount([FromBody] AccountRequest req)
    {
        if (!Enum.TryParse<AccountType>(req.Type, true, out var type))
            return BadRequest(new { message = "Tipo de cuenta inválido" });
        if (await db.Accounts.AnyAsync(a => a.Code == req.Code))
            return BadRequest(new { message = "Código de cuenta ya existe" });
        var a = new Account { Code = req.Code, Name = req.Name, Type = type, ParentId = req.ParentId, IsActive = req.IsActive };
        db.Accounts.Add(a);
        await db.SaveChangesAsync();
        return Ok(new AccountDto(a.Id, a.Code, a.Name, a.Type.ToString(), a.ParentId, a.IsActive));
    }

    [HttpGet("journal")]
    public async Task<ActionResult<List<JournalEntryDto>>> GetJournal()
    {
        var entries = await db.JournalEntries.Include(j => j.Lines).ThenInclude(l => l.Account)
            .OrderByDescending(j => j.EntryDate).ToListAsync();
        return Ok(entries.Select(ToDto).ToList());
    }

    [HttpGet("journal/{id}")]
    public async Task<ActionResult<JournalEntryDto>> GetJournalEntry(int id)
    {
        var j = await db.JournalEntries.Include(x => x.Lines).ThenInclude(l => l.Account)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (j == null) return NotFound();
        return Ok(ToDto(j));
    }

    [HttpPost("journal")]
    public async Task<ActionResult<JournalEntryDto>> CreateJournalEntry([FromBody] CreateJournalEntryRequest req)
    {
        if (!req.Lines.Any()) return BadRequest(new { message = "Debe incluir líneas" });
        var totalDebit = req.Lines.Sum(l => l.Debit);
        var totalCredit = req.Lines.Sum(l => l.Credit);
        if (totalDebit != totalCredit)
            return BadRequest(new { message = "El asiento no cuadra: débitos y créditos deben ser iguales" });

        var count = await db.JournalEntries.CountAsync() + 1;
        var entry = new JournalEntry
        {
            EntryNumber = $"AST-{DateTime.UtcNow:yyyyMM}-{count:D4}",
            EntryDate = req.EntryDate ?? DateTime.UtcNow,
            Description = req.Description,
            Reference = req.Reference,
            CreatedByUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)
        };

        foreach (var line in req.Lines)
        {
            if (await db.Accounts.FindAsync(line.AccountId) == null)
                return BadRequest(new { message = $"Cuenta {line.AccountId} no encontrada" });
            entry.Lines.Add(new JournalEntryLine
            {
                AccountId = line.AccountId, Debit = line.Debit, Credit = line.Credit, Description = line.Description
            });
        }

        db.JournalEntries.Add(entry);
        await db.SaveChangesAsync();
        await db.Entry(entry).Collection(e => e.Lines).Query().Include(l => l.Account).LoadAsync();
        return CreatedAtAction(nameof(GetJournalEntry), new { id = entry.Id }, ToDto(entry));
    }

    [HttpGet("trial-balance")]
    public async Task<ActionResult<TrialBalanceDto>> TrialBalance()
    {
        var raw = await db.JournalEntryLines.Include(l => l.Account).ToListAsync();
        var lines = raw
            .GroupBy(l => l.AccountId)
            .Select(g =>
            {
                var a = g.First().Account!;
                var debit = g.Sum(x => x.Debit);
                var credit = g.Sum(x => x.Credit);
                return new TrialBalanceLineDto(a.Code, a.Name, a.Type.ToString(), debit, credit, debit - credit);
            })
            .OrderBy(x => x.Code)
            .ToList();

        return Ok(new TrialBalanceDto(lines, lines.Sum(l => l.Debit), lines.Sum(l => l.Credit)));
    }

    private static JournalEntryDto ToDto(JournalEntry j)
    {
        var lines = j.Lines.Select(l => new JournalLineDto(
            l.AccountId, l.Account?.Code ?? "", l.Account?.Name ?? "", l.Debit, l.Credit, l.Description)).ToList();
        return new(j.Id, j.EntryNumber, j.EntryDate, j.Description, j.Reference, j.Status, lines,
            lines.Sum(l => l.Debit), lines.Sum(l => l.Credit));
    }
}
