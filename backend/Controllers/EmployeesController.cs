using ContaNexo.API.Data;
using ContaNexo.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContaNexo.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EmployeesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<object>>> GetAll()
    {
        var list = await db.Employees.OrderBy(e => e.Name).ToListAsync();
        return Ok(list.Select(ToDto));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<object>> Get(int id)
    {
        var e = await db.Employees.FindAsync(id);
        if (e == null) return NotFound();
        return Ok(ToDto(e));
    }

    [HttpPost]
    [Authorize(Roles = "Administrador")]
    public async Task<ActionResult<object>> Create([FromBody] EmployeeRequest req)
    {
        var emp = new Employee
        {
            Name = req.Name,
            Email = req.Email,
            Phone = req.Phone,
            Position = req.Position,
            Department = req.Department,
            HireDate = req.HireDate ?? DateTime.UtcNow,
            TaxId = req.TaxId,
            BankAccount = req.BankAccount,
            BankName = req.BankName,
            BankAccountType = req.BankAccountType,
            BaseSalary = req.BaseSalary,
            IsActive = req.IsActive,
            ContractType = req.ContractType ?? "indefinido",
            IntegralSalary = req.IntegralSalary,
            WithholdingProcedure2 = req.WithholdingProcedure2,
            TransportAllowanceOverride = req.TransportAllowanceOverride,
            SolidarityFundOverride = req.SolidarityFundOverride
        };
        db.Employees.Add(emp);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = emp.Id }, ToDto(emp));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Administrador")]
    public async Task<ActionResult<object>> Update(int id, [FromBody] EmployeeRequest req)
    {
        var emp = await db.Employees.FindAsync(id);
        if (emp == null) return NotFound();
        emp.Name = req.Name;
        emp.Email = req.Email;
        emp.Phone = req.Phone;
        emp.Position = req.Position;
        emp.Department = req.Department;
        emp.HireDate = req.HireDate ?? emp.HireDate;
        emp.TaxId = req.TaxId;
        emp.BankAccount = req.BankAccount;
        emp.BankName = req.BankName;
        emp.BankAccountType = req.BankAccountType;
        emp.BaseSalary = req.BaseSalary;
        emp.IsActive = req.IsActive;
        emp.ContractType = req.ContractType ?? emp.ContractType;
        emp.IntegralSalary = req.IntegralSalary;
        emp.WithholdingProcedure2 = req.WithholdingProcedure2;
        emp.TransportAllowanceOverride = req.TransportAllowanceOverride;
        emp.SolidarityFundOverride = req.SolidarityFundOverride;
        emp.TerminationReason = req.TerminationReason;
        emp.TerminationDate = req.TerminationDate;
        emp.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(ToDto(emp));
    }

    private static object ToDto(Employee e) => new
    {
        e.Id, e.Name, e.Email, e.Phone, e.Position, e.Department,
        HireDate = e.HireDate.ToString("yyyy-MM-dd"),
        e.TaxId, e.BankAccount, e.BankName, e.BankAccountType, e.BaseSalary, e.IsActive,
        e.ContractType, e.IntegralSalary, e.TerminationReason,
        TerminationDate = e.TerminationDate?.ToString("yyyy-MM-dd"),
        e.WithholdingProcedure2, e.TransportAllowanceOverride, e.SolidarityFundOverride
    };
}

public record EmployeeRequest(
    string Name, string? Email, string? Phone, string? Position, string? Department,
    DateTime? HireDate, string? TaxId, string? BankAccount, string? BankName,
    string? BankAccountType, decimal BaseSalary, bool IsActive,
    string? ContractType = null, bool IntegralSalary = false, string? TerminationReason = null,
    DateTime? TerminationDate = null, bool WithholdingProcedure2 = false,
    bool? TransportAllowanceOverride = null, decimal? SolidarityFundOverride = null);
