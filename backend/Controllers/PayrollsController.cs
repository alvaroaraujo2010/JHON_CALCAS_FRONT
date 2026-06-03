using ContaNexo.API.Data;
using ContaNexo.API.Models;
using ContaNexo.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContaNexo.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PayrollsController(AppDbContext db, PayrollCalculator calc, LegalParameterService legal, PayrollAccountingService accounting) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<object>>> GetAll()
    {
        var list = await db.Payrolls
            .Include(p => p.Details).ThenInclude(d => d.Deductions)
            .OrderByDescending(p => p.CreatedAt).ToListAsync();
        return Ok(list.Select(ToDto));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<object>> Get(int id)
    {
        var p = await db.Payrolls
            .Include(x => x.Details).ThenInclude(d => d.Deductions)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return NotFound();
        return Ok(ToDto(p));
    }

    [HttpPost]
    [Authorize(Roles = "Administrador")]
    public async Task<ActionResult<object>> Create([FromBody] PayrollRequest req)
    {
        var payroll = new Payroll
        {
            PeriodStart = req.PeriodStart,
            PeriodEnd = req.PeriodEnd,
            PaymentDate = req.PaymentDate,
            Notes = req.Notes,
            Status = "draft"
        };
        db.Payrolls.Add(payroll);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = payroll.Id }, ToDto(payroll));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Administrador")]
    public async Task<ActionResult<object>> Update(int id, [FromBody] PayrollUpdateRequest req)
    {
        var payroll = await db.Payrolls
            .Include(p => p.Details).ThenInclude(d => d.Deductions)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (payroll == null) return NotFound();
        if (payroll.Status != "draft")
            return BadRequest(new { message = "Solo se pueden editar nóminas en borrador" });

        if (req.PeriodStart != null) payroll.PeriodStart = req.PeriodStart;
        if (req.PeriodEnd != null) payroll.PeriodEnd = req.PeriodEnd;
        if (req.PaymentDate != null) payroll.PaymentDate = req.PaymentDate;
        if (req.Notes != null) payroll.Notes = req.Notes;
        payroll.UpdatedAt = DateTime.UtcNow;

        if (req.Details != null)
        {
            db.PayrollDeductionLines.RemoveRange(payroll.Details.SelectMany(d => d.Deductions));
            db.PayrollDetails.RemoveRange(payroll.Details);
            payroll.Details.Clear();

            await RecalculateAndBuildDetailsAsync(payroll, req.Details);
        }

        await db.SaveChangesAsync();
        return Ok(ToDto(payroll));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Delete(int id)
    {
        var payroll = await db.Payrolls.FindAsync(id);
        if (payroll == null) return NotFound();
        if (payroll.Status != "draft") return BadRequest(new { message = "Solo se pueden eliminar nóminas en borrador" });
        db.Payrolls.Remove(payroll);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id}/process")]
    [Authorize(Roles = "Administrador")]
    public async Task<ActionResult<object>> Process(int id)
    {
        var payroll = await db.Payrolls
            .Include(p => p.Details).ThenInclude(d => d.Deductions)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (payroll == null) return NotFound();
        if (payroll.Status != "draft") return BadRequest(new { message = "Solo se pueden procesar nóminas en borrador" });
        if (!payroll.Details.Any())
            return BadRequest(new { message = "Debe agregar empleados antes de procesar la nómina" });

        await EnsureDetailsCalculatedAsync(payroll);
        if (payroll.TotalNet <= 0)
            return BadRequest(new { message = "El total neto de la nómina debe ser mayor a cero" });

        await EnsureSocialSecurityPaymentsAsync(payroll);
        await EnsureProvisionsAsync(payroll);
        await accounting.CreateJournalEntryForPayrollAsync(payroll, db);

        payroll.Status = "processed";
        payroll.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(ToDto(payroll));
    }

    [HttpPost("{id}/pay")]
    [Authorize(Roles = "Administrador")]
    public async Task<ActionResult<object>> Pay(int id, [FromBody] PayPayrollRequest req)
    {
        var payroll = await db.Payrolls
            .Include(p => p.Details)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (payroll == null) return NotFound();
        if (payroll.Status != "processed") return BadRequest(new { message = "Solo se pueden pagar nóminas procesadas" });

        payroll.Status = "paid";
        payroll.PaymentDate = req.PaymentDate;
        payroll.UpdatedAt = DateTime.UtcNow;

        var record = new PaymentRecord
        {
            PayrollId = id,
            PeriodStart = payroll.PeriodStart,
            PeriodEnd = payroll.PeriodEnd,
            PaymentDate = req.PaymentDate,
            TotalAmount = payroll.TotalNet,
            PaymentMethod = req.PaymentMethod,
            Reference = req.Reference,
            Status = "completed"
        };
        db.PaymentRecords.Add(record);
        await db.SaveChangesAsync();

        return Ok(new
        {
            record.Id, record.PayrollId, record.PeriodStart, record.PeriodEnd,
            record.PaymentDate, record.TotalAmount, record.PaymentMethod,
            record.Reference, record.Status
        });
    }

    [HttpPost("validate-period")]
    public async Task<ActionResult<object>> ValidatePeriod([FromBody] ValidatePeriodRequest req)
    {
        var exists = await db.Payrolls.AnyAsync(p =>
            p.PeriodStart == req.PeriodStart && p.PeriodEnd == req.PeriodEnd &&
            p.Status != "cancelled");
        return Ok(new
        {
            valid = !exists,
            message = exists ? "Ya existe una nómina para ese período" : null
        });
    }

    [HttpPost("simulate")]
    public async Task<ActionResult<object>> Simulate([FromBody] SimulatePayrollRequest req)
    {
        var emp = await db.Employees.FindAsync(req.EmployeeId);
        if (emp == null) return NotFound(new { message = "Empleado no encontrado" });
        var param = await legal.GetForPeriodAsync(req.PeriodStart, req.PeriodEnd);
        var ctx = BuildContext(emp, req.PeriodStart, req.PeriodEnd, req.CustomDeductions ?? new());
        var result = await calc.ComputeAsync(ctx, param);
        return Ok(new
        {
            year = param.Year,
            smlmv = param.Smlmv,
            uvt = param.Uvt,
            transportAllowance = param.TransportAllowance,
            baseSalary = result.BaseSalary,
            transportAllowanceApplied = result.TransportAllowance,
            grossIncome = result.GrossIncome,
            ibc = result.Ibc,
            cappedIbc = result.CappedIbc,
            deductions = new
            {
                employeeHealth = result.EmployeeHealth,
                employeePension = result.EmployeePension,
                solidarityFund = result.SolidarityFund,
                withholdingTax = result.WithholdingTax,
                customDeductions = ctx.CustomDeductions.Sum(d => d.Amount),
                total = result.TotalEmployeeDeductions
            },
            employerContributions = new
            {
                health = result.EmployerHealth,
                pension = result.EmployerPension,
                arl = result.Arl,
                compensationFund = result.CompensationFund,
                sena = result.Sena,
                icbf = result.Icbf,
                total = result.TotalEmployerContributions
            },
            provisions = new
            {
                prima = result.PrimaProvision,
                cesantias = result.CesantiasProvision,
                cesantiasInterest = result.CesantiasInterestProvision,
                vacations = result.VacationProvision,
                total = result.TotalProvisions
            },
            netPay = result.NetPay
        });
    }

    private async Task RecalculateAndBuildDetailsAsync(Payroll payroll, List<PayrollDetailRequest> requests)
    {
        var param = await legal.GetForPeriodAsync(payroll.PeriodStart, payroll.PeriodEnd);
        decimal totalGross = 0, totalTransp = 0, totalDed = 0, totalNet = 0;
        decimal totalPrima = 0, totalCes = 0, totalIntCes = 0, totalVac = 0;
        decimal totalEmpHealth = 0, totalEmpPension = 0, totalArl = 0, totalCaja = 0, totalSena = 0, totalIcbf = 0;

        foreach (var d in requests)
        {
            if (d.EmployeeId <= 0)
                throw new InvalidOperationException("Debe seleccionar un empleado válido");
            if (d.BaseSalary < 0)
                throw new InvalidOperationException($"Salario base inválido para {d.EmployeeName}");

            var emp = await db.Employees.FindAsync(d.EmployeeId);
            if (emp == null)
                throw new InvalidOperationException($"Empleado {d.EmployeeId} no encontrado");

            emp.BaseSalary = d.BaseSalary;
            emp.UpdatedAt = DateTime.UtcNow;

            var ctx = BuildContext(emp, payroll.PeriodStart, payroll.PeriodEnd, d.Deductions ?? new());
            var c = await calc.ComputeAsync(ctx, param);

            if (c.TotalEmployeeDeductions > c.GrossIncome)
                throw new InvalidOperationException($"Las deducciones de {emp.Name} exceden el ingreso bruto");

            var detail = new PayrollDetail
            {
                PayrollId = payroll.Id,
                EmployeeId = emp.Id,
                EmployeeName = emp.Name,
                BaseSalary = c.BaseSalary,
                TransportAllowance = c.TransportAllowance,
                TotalGross = c.GrossIncome,
                Ibc = c.Ibc,
                EmployeeHealthDeduction = c.EmployeeHealth,
                EmployeePensionDeduction = c.EmployeePension,
                SolidarityFundDeduction = c.SolidarityFund,
                WithholdingTax = c.WithholdingTax,
                TotalDeductions = c.TotalEmployeeDeductions,
                NetSalary = c.NetPay,
                EmployerHealthContribution = c.EmployerHealth,
                EmployerPensionContribution = c.EmployerPension,
                ArlContribution = c.Arl,
                CompensationFundContribution = c.CompensationFund,
                SenaContribution = c.Sena,
                IcbfContribution = c.Icbf,
                TotalEmployerContributions = c.TotalEmployerContributions,
                PrimaProvision = c.PrimaProvision,
                CesantiasProvision = c.CesantiasProvision,
                CesantiasInterestProvision = c.CesantiasInterestProvision,
                VacationProvision = c.VacationProvision,
                TotalProvisions = c.TotalProvisions
            };

            foreach (var custom in ctx.CustomDeductions)
            {
                detail.Deductions.Add(new PayrollDeductionLine
                {
                    DeductionId = custom.DeductionId,
                    DeductionName = custom.Name,
                    Amount = custom.Amount,
                    Category = "other"
                });
            }
            if (c.EmployeeHealth > 0)
                detail.Deductions.Add(new PayrollDeductionLine
                {
                    DeductionId = 0,
                    DeductionName = "Salud empleado (4%)",
                    Amount = c.EmployeeHealth,
                    Category = "social_security"
                });
            if (c.EmployeePension > 0)
                detail.Deductions.Add(new PayrollDeductionLine
                {
                    DeductionId = 0,
                    DeductionName = "Pensión empleado (4%)",
                    Amount = c.EmployeePension,
                    Category = "social_security"
                });
            if (c.SolidarityFund > 0)
                detail.Deductions.Add(new PayrollDeductionLine
                {
                    DeductionId = 0,
                    DeductionName = "Fondo de Solidaridad Pensional",
                    Amount = c.SolidarityFund,
                    Category = "social_security"
                });
            if (c.WithholdingTax > 0)
                detail.Deductions.Add(new PayrollDeductionLine
                {
                    DeductionId = 0,
                    DeductionName = "Retención en la fuente",
                    Amount = c.WithholdingTax,
                    Category = "tax"
                });

            payroll.Details.Add(detail);

            totalGross += c.GrossIncome;
            totalTransp += c.TransportAllowance;
            totalDed += c.TotalEmployeeDeductions;
            totalNet += c.NetPay;
            totalPrima += c.PrimaProvision;
            totalCes += c.CesantiasProvision;
            totalIntCes += c.CesantiasInterestProvision;
            totalVac += c.VacationProvision;
            totalEmpHealth += c.EmployerHealth;
            totalEmpPension += c.EmployerPension;
            totalArl += c.Arl;
            totalCaja += c.CompensationFund;
            totalSena += c.Sena;
            totalIcbf += c.Icbf;
        }

        payroll.TotalGross = totalGross;
        payroll.TotalTransportAllowance = totalTransp;
        payroll.TotalDeductions = totalDed;
        payroll.TotalNet = totalNet;
        payroll.TotalEmployerCost = totalNet + totalEmpHealth + totalEmpPension + totalArl + totalCaja + totalSena + totalIcbf;
        payroll.TotalPrimaProvision = totalPrima;
        payroll.TotalCesantiasProvision = totalCes;
        payroll.TotalCesantiasInterestProvision = totalIntCes;
        payroll.TotalVacationProvision = totalVac;
        payroll.TotalEmployerHealth = totalEmpHealth;
        payroll.TotalEmployerPension = totalEmpPension;
        payroll.TotalArl = totalArl;
        payroll.TotalCompensationFund = totalCaja;
        payroll.TotalSena = totalSena;
        payroll.TotalIcbf = totalIcbf;
    }

    private EmployeeContext BuildContext(Employee emp, string periodStart, string periodEnd, List<PayrollDeductionLineRequest> customs)
    {
        var periodEndDate = DateTime.TryParse(periodEnd, out var p) ? p : DateTime.UtcNow;
        return new EmployeeContext
        {
            BaseSalary = emp.BaseSalary,
            VariableIncome = 0m,
            IntegralSalary = emp.IntegralSalary,
            TransportAllowanceOverride = emp.TransportAllowanceOverride,
            SolidarityFundOverride = emp.SolidarityFundOverride,
            WithholdingProcedure2 = emp.WithholdingProcedure2,
            PayrollPeriodEnd = periodEndDate,
            CustomDeductions = customs.Select(c => new DeductionInput
            {
                DeductionId = c.DeductionId,
                Name = c.DeductionName,
                Amount = c.Amount
            }).ToList()
        };
    }

    private async Task EnsureDetailsCalculatedAsync(Payroll payroll)
    {
        if (payroll.Details.Count == 0) return;
        var param = await legal.GetForPeriodAsync(payroll.PeriodStart, payroll.PeriodEnd);
        foreach (var detail in payroll.Details)
        {
            var emp = await db.Employees.FindAsync(detail.EmployeeId);
            if (emp == null) continue;
            var ctx = BuildContext(emp, payroll.PeriodStart, payroll.PeriodEnd, detail.Deductions
                .Where(x => x.Category == "other")
                .Select(x => new PayrollDeductionLineRequest(x.DeductionId, x.DeductionName, x.Amount))
                .ToList());
            var c = await calc.ComputeAsync(ctx, param);
            detail.BaseSalary = c.BaseSalary;
            detail.TransportAllowance = c.TransportAllowance;
            detail.TotalGross = c.GrossIncome;
            detail.Ibc = c.Ibc;
            detail.EmployeeHealthDeduction = c.EmployeeHealth;
            detail.EmployeePensionDeduction = c.EmployeePension;
            detail.SolidarityFundDeduction = c.SolidarityFund;
            detail.WithholdingTax = c.WithholdingTax;
            detail.TotalDeductions = c.TotalEmployeeDeductions;
            detail.NetSalary = c.NetPay;
            detail.EmployerHealthContribution = c.EmployerHealth;
            detail.EmployerPensionContribution = c.EmployerPension;
            detail.ArlContribution = c.Arl;
            detail.CompensationFundContribution = c.CompensationFund;
            detail.SenaContribution = c.Sena;
            detail.IcbfContribution = c.Icbf;
            detail.TotalEmployerContributions = c.TotalEmployerContributions;
            detail.PrimaProvision = c.PrimaProvision;
            detail.CesantiasProvision = c.CesantiasProvision;
            detail.CesantiasInterestProvision = c.CesantiasInterestProvision;
            detail.VacationProvision = c.VacationProvision;
            detail.TotalProvisions = c.TotalProvisions;
        }

        payroll.TotalGross = payroll.Details.Sum(d => d.TotalGross);
        payroll.TotalTransportAllowance = payroll.Details.Sum(d => d.TransportAllowance);
        payroll.TotalDeductions = payroll.Details.Sum(d => d.TotalDeductions);
        payroll.TotalNet = payroll.Details.Sum(d => d.NetSalary);
        payroll.TotalEmployerCost = payroll.TotalNet
            + payroll.Details.Sum(d => d.TotalEmployerContributions);
        payroll.TotalPrimaProvision = payroll.Details.Sum(d => d.PrimaProvision);
        payroll.TotalCesantiasProvision = payroll.Details.Sum(d => d.CesantiasProvision);
        payroll.TotalCesantiasInterestProvision = payroll.Details.Sum(d => d.CesantiasInterestProvision);
        payroll.TotalVacationProvision = payroll.Details.Sum(d => d.VacationProvision);
        payroll.TotalEmployerHealth = payroll.Details.Sum(d => d.EmployerHealthContribution);
        payroll.TotalEmployerPension = payroll.Details.Sum(d => d.EmployerPensionContribution);
        payroll.TotalArl = payroll.Details.Sum(d => d.ArlContribution);
        payroll.TotalCompensationFund = payroll.Details.Sum(d => d.CompensationFundContribution);
        payroll.TotalSena = payroll.Details.Sum(d => d.SenaContribution);
        payroll.TotalIcbf = payroll.Details.Sum(d => d.IcbfContribution);
    }

    private async Task EnsureSocialSecurityPaymentsAsync(Payroll payroll)
    {
        var period = $"{payroll.PeriodStart} - {payroll.PeriodEnd}";
        foreach (var detail in payroll.Details)
        {
            var payment = await db.SocialSecurityPayments.FirstOrDefaultAsync(x =>
                x.Period == period && x.EmployeeId == detail.EmployeeId);
            if (payment == null)
            {
                payment = new SocialSecurityPayment
                {
                    Period = period,
                    EmployeeId = detail.EmployeeId,
                    EmployeeName = detail.EmployeeName,
                    Status = "pending"
                };
                db.SocialSecurityPayments.Add(payment);
            }
            else if (payment.Status == "paid")
            {
                continue;
            }
            payment.EmployeeName = detail.EmployeeName;
            payment.BaseSalary = detail.BaseSalary;
            payment.TransportAllowance = detail.TransportAllowance;
            payment.Ibc = detail.Ibc;
            var paramForCap = await legal.GetByYearAsync(DateTime.UtcNow.Year);
            payment.CapIbc = Math.Min(detail.Ibc, paramForCap.Smlmv * paramForCap.MaxHealthIbcSmlmv);
            payment.EmployeeHealthContribution = detail.EmployeeHealthDeduction;
            payment.EmployeePensionContribution = detail.EmployeePensionDeduction;
            payment.SolidarityFundContribution = detail.SolidarityFundDeduction;
            payment.EmployerHealthContribution = detail.EmployerHealthContribution;
            payment.EmployerPensionContribution = detail.EmployerPensionContribution;
            payment.ArlContribution = detail.ArlContribution;
            payment.CompensationFundContribution = detail.CompensationFundContribution;
            payment.SenaContribution = detail.SenaContribution;
            payment.IcbfContribution = detail.IcbfContribution;
            payment.EmployeeContributionTotal = detail.EmployeeHealthDeduction
                + detail.EmployeePensionDeduction
                + detail.SolidarityFundDeduction;
            payment.EmployerContributionTotal = detail.TotalEmployerContributions;
            payment.ContributionAmount = payment.EmployeeContributionTotal + payment.EmployerContributionTotal;
        }
    }

    private async Task EnsureProvisionsAsync(Payroll payroll)
    {
        if (!DateTime.TryParse(payroll.PeriodEnd, out var periodEnd)) return;
        var year = periodEnd.Year;
        var month = periodEnd.Month;
        var periodLabel = $"{year:D4}-{month:D2}";

        foreach (var detail in payroll.Details)
        {
            var existing = await db.PayrollProvisions.FirstOrDefaultAsync(p =>
                p.EmployeeId == detail.EmployeeId && p.Year == year && p.Month == month);
            if (existing == null)
            {
                existing = new PayrollProvision
                {
                    EmployeeId = detail.EmployeeId,
                    EmployeeName = detail.EmployeeName,
                    Year = year,
                    Month = month,
                    PeriodLabel = periodLabel,
                    PayrollId = payroll.Id
                };
                db.PayrollProvisions.Add(existing);
            }

            existing.BaseSalary = detail.BaseSalary;
            existing.TransportAllowance = detail.TransportAllowance;
            existing.PrimaProvision = detail.PrimaProvision;
            existing.CesantiasProvision = detail.CesantiasProvision;
            existing.CesantiasInterestProvision = detail.CesantiasInterestProvision;
            existing.VacationProvision = detail.VacationProvision;
            existing.TotalProvision = detail.TotalProvisions;

            var yearProvisions = await db.PayrollProvisions
                .Where(p => p.EmployeeId == detail.EmployeeId && p.Year == year)
                .ToListAsync();
            existing.AccumulatedPrima = yearProvisions.Sum(p => p.PrimaProvision);
            existing.AccumulatedCesantias = yearProvisions.Sum(p => p.CesantiasProvision);
            existing.AccumulatedCesantiasInterest = yearProvisions.Sum(p => p.CesantiasInterestProvision);
            existing.AccumulatedVacations = yearProvisions.Sum(p => p.VacationProvision);
            existing.AccumulatedTotal = yearProvisions.Sum(p => p.TotalProvision);
        }
    }

    private static object ToDto(Payroll p) => new
    {
        p.Id, p.PeriodStart, p.PeriodEnd, p.Status, p.PaymentDate,
        p.TotalGross, p.TotalTransportAllowance, p.TotalDeductions, p.TotalNet, p.TotalEmployerCost,
        p.TotalPrimaProvision, p.TotalCesantiasProvision, p.TotalCesantiasInterestProvision, p.TotalVacationProvision,
        p.TotalEmployerHealth, p.TotalEmployerPension, p.TotalArl, p.TotalCompensationFund, p.TotalSena, p.TotalIcbf,
        p.Notes,
        CreatedAt = p.CreatedAt.ToString("yyyy-MM-dd"),
        Details = p.Details.Select(d => new
        {
            d.Id, d.EmployeeId, d.EmployeeName,
            d.BaseSalary, d.TransportAllowance, d.TotalGross, d.Ibc,
            d.EmployeeHealthDeduction, d.EmployeePensionDeduction, d.SolidarityFundDeduction, d.WithholdingTax,
            d.TotalDeductions, d.NetSalary,
            d.EmployerHealthContribution, d.EmployerPensionContribution, d.ArlContribution,
            d.CompensationFundContribution, d.SenaContribution, d.IcbfContribution, d.TotalEmployerContributions,
            d.PrimaProvision, d.CesantiasProvision, d.CesantiasInterestProvision, d.VacationProvision, d.TotalProvisions,
            Deductions = d.Deductions.Select(x => new { x.DeductionId, x.DeductionName, x.Category, x.Amount })
        })
    };
}

public record PayrollRequest(string PeriodStart, string PeriodEnd, string? PaymentDate, string? Notes);
public record PayPayrollRequest(string PaymentDate, string PaymentMethod, string? Reference);
public record ValidatePeriodRequest(string PeriodStart, string PeriodEnd);
public record PayrollUpdateRequest(string? PeriodStart, string? PeriodEnd, string? PaymentDate,
    string? Notes, List<PayrollDetailRequest>? Details);
public record PayrollDetailRequest(int EmployeeId, string EmployeeName, decimal BaseSalary,
    decimal TotalDeductions, decimal NetSalary, List<PayrollDeductionLineRequest> Deductions);
public record PayrollDeductionLineRequest(int DeductionId, string DeductionName, decimal Amount);
public record SimulatePayrollRequest(int EmployeeId, string PeriodStart, string PeriodEnd, List<PayrollDeductionLineRequest>? CustomDeductions);
