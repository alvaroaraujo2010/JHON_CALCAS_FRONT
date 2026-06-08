using ContaNexo.API.Data;
using ContaNexo.API.Models;
using ContaNexo.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContaNexo.API.Controllers;

[ApiController]
[Route("api/payroll-settlements")]
[Authorize]
public class PayrollSettlementsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly PayrollSettlementService _svc;
    private readonly WithholdingTaxService _withholding;
    private readonly LegalParameterService _legal;

    public PayrollSettlementsController(
        AppDbContext db,
        PayrollSettlementService svc,
        WithholdingTaxService withholding,
        LegalParameterService legal)
    {
        _db = db;
        _svc = svc;
        _withholding = withholding;
        _legal = legal;
    }

    [HttpGet]
    public async Task<ActionResult<List<object>>> List()
    {
        var list = await _db.PayrollSettlements
            .Include(s => s.Employee)
            .OrderByDescending(s => s.SettlementDate)
            .ToListAsync();
        return Ok(list.Select(ToDto));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<object>> Get(int id)
    {
        var s = await _db.PayrollSettlements.Include(x => x.Employee).FirstOrDefaultAsync(x => x.Id == id);
        if (s == null) return NotFound();
        return Ok(ToDto(s));
    }

    [HttpPost("simulate")]
    [Authorize(Policy = "payroll.settlement")]
    public async Task<ActionResult<object>> Simulate([FromBody] SettlementRequest req)
    {
        var emp = await _db.Employees.FindAsync(req.EmployeeId);
        if (emp == null) return NotFound(new { message = "Empleado no encontrado" });
        if (string.IsNullOrEmpty(emp.TerminationReason))
            return BadRequest(new { message = "El empleado debe tener un motivo de terminación configurado" });

        var result = await _svc.CalculateAsync(
            emp,
            req.SettlementDate,
            req.LastDayWorked ?? emp.TerminationDate ?? DateTime.UtcNow,
            req.VariableAverage3Months ?? 0m,
            req.PrimaAlreadyPaid ?? 0m,
            req.VacationsAlreadyPaid ?? 0m);

        var (ret, exempt, nonSeveranceGross, severanceTaxable) = await ApplyWithholdingAsync(result);

        return Ok(new
        {
            employee = new { emp.Id, emp.Name, emp.TaxId, emp.HireDate, emp.BaseSalary, emp.ContractType },
            workedDaysCurrentYear = result.WorkedDaysCurrentYear,
            workedDaysCurrentSemester = result.WorkedDaysCurrentSemester,
            totalWorkedDays = result.TotalWorkedDays,
            semesterStart = result.SemesterStart,
            cesantias = result.CesantiasAmount,
            cesantiasInterest = result.CesantiasInterestAmount,
            prima = result.PrimaAmount,
            primaGross = result.PrimaAmount + result.PrimaAlreadyPaid,
            primaAlreadyPaid = result.PrimaAlreadyPaid,
            vacationDays = result.VacationDays,
            vacations = result.VacationAmount,
            vacationsAlreadyPaid = result.VacationsAlreadyPaid,
            severance = result.SeveranceAmount,
            totalGross = result.TotalGross,
            nonSeveranceGross,
            severanceTaxable,
            exemptUvt = 200m,
            exemptAmount = exempt,
            retencionFuente = ret,
            netToPay = result.TotalGross - ret,
            smlmv = result.BaseForProvisions > 0 ? (await _legal.GetByYearAsync(req.SettlementDate.Year)).Smlmv : 0m,
            uvt = (await _legal.GetByYearAsync(req.SettlementDate.Year)).Uvt,
            year = req.SettlementDate.Year
        });
    }

    [HttpPost]
    [Authorize(Policy = "payroll.settlement")]
    public async Task<ActionResult<object>> Create([FromBody] SettlementRequest req)
    {
        var emp = await _db.Employees.FindAsync(req.EmployeeId);
        if (emp == null) return NotFound(new { message = "Empleado no encontrado" });
        if (emp.TerminationDate == null) return BadRequest(new { message = "El empleado debe tener fecha de terminación" });

        var result = await _svc.CalculateAsync(
            emp,
            req.SettlementDate,
            emp.TerminationDate.Value,
            req.VariableAverage3Months ?? 0m,
            req.PrimaAlreadyPaid ?? 0m,
            req.VacationsAlreadyPaid ?? 0m);

        var (ret, exempt, nonSeveranceGross, severanceTaxable) = await ApplyWithholdingAsync(result);
        var net = result.TotalGross - ret;

        var settlement = new PayrollSettlement
        {
            EmployeeId = emp.Id,
            EmployeeName = emp.Name,
            TaxId = emp.TaxId ?? string.Empty,
            SettlementDate = req.SettlementDate,
            HireDate = emp.HireDate,
            LastContractDate = emp.TerminationDate,
            TerminationReason = emp.TerminationReason ?? string.Empty,
            BaseSalary = emp.BaseSalary,
            WorkedDays = result.WorkedDaysCurrentYear,
            WorkedDaysCurrentSemester = result.WorkedDaysCurrentSemester,
            CesantiasAmount = result.CesantiasAmount,
            CesantiasInterestAmount = result.CesantiasInterestAmount,
            PrimaAmount = result.PrimaAmount,
            VacationAmount = result.VacationAmount,
            SeveranceAmount = result.SeveranceAmount,
            TotalGross = result.TotalGross,
            RetencionFuente = ret,
            TotalDeductions = ret,
            NetToPay = net,
            Status = "draft",
            Notes = req.Notes
        };
        _db.PayrollSettlements.Add(settlement);
        await _db.SaveChangesAsync();

        emp.IsActive = false;
        emp.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var saved = await _db.PayrollSettlements.Include(s => s.Employee).FirstAsync(s => s.Id == settlement.Id);
        return CreatedAtAction(nameof(Get), new { id = saved.Id }, ToDto(saved));
    }

    [HttpPost("{id}/pay")]
    [Authorize(Policy = "payroll.settlement")]
    public async Task<ActionResult<object>> Pay(int id, [FromBody] PaySettlementRequest req)
    {
        var s = await _db.PayrollSettlements.FindAsync(id);
        if (s == null) return NotFound();
        if (s.Status == "paid") return BadRequest(new { message = "La liquidación ya fue pagada" });
        s.Status = "paid";
        s.PaymentDate = req.PaymentDate;
        s.PaymentMethod = req.PaymentMethod;
        s.Reference = req.Reference;
        s.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { s.Id, s.Status, s.PaymentDate, s.PaymentMethod, s.Reference });
    }

    /// <summary>
    /// Aplica retención en la fuente al total de la liquidación:
    ///   * Sobre (cesantías + intereses + prima + vacaciones) → Procedimiento 1 Art. 383 ET
    ///   * Sobre indemnización → Procedimiento 1 sobre el excedente de 200 UVT
    ///     (Art. 401 ET par. 1, modificado por Art. 32 Ley 2277/2022)
    ///   Aplica también las deducciones del Art. 387 ET si el empleado las tiene activas.
    /// </summary>
    private async Task<(decimal Retencion, decimal Exempt, decimal NonSeveranceGross, decimal SeveranceTaxable)>
        ApplyWithholdingAsync(PayrollSettlementResult result)
    {
        var param = await _legal.GetByYearAsync(result.SettlementDate.Year);
        var nonSeverance = result.CesantiasAmount + result.CesantiasInterestAmount
                         + result.PrimaAmount + result.VacationAmount;

        var exempt = param.Uvt * 200m;
        var severanceTaxable = Math.Max(0, result.SeveranceAmount - exempt);

        var deductions = Art387Deductions.From(result.Employee);

        decimal ret = 0m;
        if (nonSeverance > 0)
            ret += _withholding.CalculateProcedureOne(nonSeverance, param, 0m, deductions);
        if (severanceTaxable > 0)
            ret += _withholding.CalculateProcedureOne(severanceTaxable, param, 0m, deductions);

        return (ret, exempt, nonSeverance, severanceTaxable);
    }

    private static object ToDto(PayrollSettlement s) => new
    {
        s.Id, s.EmployeeId, s.EmployeeName, s.TaxId, s.SettlementDate, s.HireDate, s.LastContractDate,
        s.TerminationReason, s.BaseSalary,
        s.WorkedDays, s.WorkedDaysCurrentSemester,
        s.CesantiasAmount, s.CesantiasInterestAmount, s.PrimaAmount,
        s.VacationAmount, s.SeveranceAmount, s.OtherAmounts, s.TotalGross,
        s.RetencionFuente, s.TotalDeductions, s.NetToPay,
        s.Status, s.PaymentDate, s.PaymentMethod, s.Reference, s.Notes,
        s.CreatedAt, s.UpdatedAt
    };
}

public record SettlementRequest(
    int EmployeeId,
    DateTime SettlementDate,
    DateTime? LastDayWorked,
    decimal? VariableAverage3Months,
    string? Notes,
    decimal? PrimaAlreadyPaid = null,
    decimal? VacationsAlreadyPaid = null);

public record PaySettlementRequest(string PaymentDate, string PaymentMethod, string? Reference);
