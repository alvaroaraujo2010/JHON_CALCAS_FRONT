using ContaNexo.API.Data;
using ContaNexo.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ContaNexo.API.Services;

/// <summary>
/// Cálculo de retención en la fuente sobre ingresos laborales (Art. 383 ET).
/// Procedimiento 1 (general) — Tabla progresiva en UVT.
/// Procedimiento 2 (simplificado) — Tarifa fija según rango (Art. 383 ET, par.).
/// </summary>
public class WithholdingTaxService
{
    private readonly AppDbContext _db;

    public static readonly List<WithholdingTaxBracket> ProcedureOneTable2025 = new()
    {
        new() { Year = 2025, FromUvt = 0,        ToUvt = 95,       MarginalRate = 0,    BaseTaxUvt = 0,     Procedure = "1" },
        new() { Year = 2025, FromUvt = 95,       ToUvt = 150,      MarginalRate = 19,   BaseTaxUvt = 0,     Procedure = "1" },
        new() { Year = 2025, FromUvt = 150,      ToUvt = 360,      MarginalRate = 28,   BaseTaxUvt = 10.45m, Procedure = "1" },
        new() { Year = 2025, FromUvt = 360,      ToUvt = 640,      MarginalRate = 33,   BaseTaxUvt = 69.25m, Procedure = "1" },
        new() { Year = 2025, FromUvt = 640,      ToUvt = 945,      MarginalRate = 35,   BaseTaxUvt = 160.05m,Procedure = "1" },
        new() { Year = 2025, FromUvt = 945,      ToUvt = 2300,     MarginalRate = 37,   BaseTaxUvt = 268.40m,Procedure = "1" },
        new() { Year = 2025, FromUvt = 2300,     ToUvt = null,     MarginalRate = 39,   BaseTaxUvt = 770.45m,Procedure = "1" }
    };

    public static readonly List<WithholdingTaxBracket> ProcedureOneTable2026 = new()
    {
        new() { Year = 2026, FromUvt = 0,        ToUvt = 95,       MarginalRate = 0,    BaseTaxUvt = 0,     Procedure = "1" },
        new() { Year = 2026, FromUvt = 95,       ToUvt = 150,      MarginalRate = 19,   BaseTaxUvt = 0,     Procedure = "1" },
        new() { Year = 2026, FromUvt = 150,      ToUvt = 360,      MarginalRate = 28,   BaseTaxUvt = 10.45m, Procedure = "1" },
        new() { Year = 2026, FromUvt = 360,      ToUvt = 640,      MarginalRate = 33,   BaseTaxUvt = 69.25m, Procedure = "1" },
        new() { Year = 2026, FromUvt = 640,      ToUvt = 945,      MarginalRate = 35,   BaseTaxUvt = 160.05m,Procedure = "1" },
        new() { Year = 2026, FromUvt = 945,      ToUvt = 2300,     MarginalRate = 37,   BaseTaxUvt = 268.40m,Procedure = "1" },
        new() { Year = 2026, FromUvt = 2300,     ToUvt = null,     MarginalRate = 39,   BaseTaxUvt = 770.45m,Procedure = "1" }
    };

    public WithholdingTaxService(AppDbContext db) => _db = db;

    /// <summary>
    /// Procedimiento 1 Art. 383 ET.
    /// Opcionalmente aplica las deducciones de Art. 387 ET (reformado por Ley 2277/2022):
    ///   - 1 dependiente económico: 10% ingresos, tope 32 UVT/mes
    ///   - Intereses de vivienda: tope 100 UVT/mes
    ///   - Medicina prepagada: tope 16 UVT/mes
    ///   - Aportes AFC/FVP: 30% ingresos, tope 3.800 UVT/año (≈316,67 UVT/mes)
    /// </summary>
    public decimal CalculateProcedureOne(
        decimal grossIncome,
        LegalParameter param,
        decimal nonTaxableIncome = 0m,
        Art387Deductions? deductions = null)
    {
        var totalIngresos = Math.Max(0, grossIncome);
        var ingresosNoGravados = Math.Max(0, nonTaxableIncome);
        var subtotal1 = Math.Max(0, totalIngresos - ingresosNoGravados);
        var maxExemptUvt = param.ExemptIncomeUvt;
        var maxExemptValue = maxExemptUvt * param.Uvt;
        var exempt25 = Math.Min(subtotal1 * 0.25m, maxExemptValue);
        var subtotal2 = Math.Max(0, subtotal1 - exempt25);

        // Aplica deducciones opcionales del Art. 387 ET sobre el subtotal
        // que ya pasó la exención del 25% (estas deducciones son adicionales).
        if (deductions != null)
            subtotal2 = Math.Max(0, subtotal2 - CalculateArt387Deductions(subtotal2, param, deductions));

        var subtotalUvt = subtotal2 / param.Uvt;
        if (subtotalUvt < param.MinimumWithholdingUvt) return 0m;

        var brackets = GetTableForYear(param.Year);
        WithholdingTaxBracket? bracket = null;
        foreach (var b in brackets.OrderBy(x => x.FromUvt))
        {
            if (subtotalUvt >= b.FromUvt && (b.ToUvt == null || subtotalUvt < b.ToUvt))
            {
                bracket = b;
                break;
            }
        }
        if (bracket == null) return 0m;
        if (bracket.MarginalRate == 0) return 0m;

        var taxableUvt = Math.Max(0, subtotalUvt - bracket.FromUvt);
        var taxUvt = bracket.BaseTaxUvt + (taxableUvt * bracket.MarginalRate / 100m);
        var tax = taxUvt * param.Uvt;
        return RoundCurrency(tax);
    }

    /// <summary>
    /// Suma de las deducciones opcionales del Art. 387 ET. Cada una con su tope.
    /// </summary>
    public decimal CalculateArt387Deductions(decimal subtotal, LegalParameter param, Art387Deductions d)
    {
        decimal total = 0m;
        if (d.HasDependents)
        {
            var dep = Math.Min(subtotal * 0.10m, 32m * param.Uvt);
            total += dep;
        }
        if (d.HousingInterestEnabled)
            total += 100m * param.Uvt;
        if (d.PrepaidHealthEnabled)
            total += 16m * param.Uvt;
        if (d.AfcMonthlyAmount > 0)
        {
            // Tope anual 3.800 UVT → ~316,67 UVT/mes (sin exceder 30% del ingreso).
            var monthlyCap = (3800m / 12m) * param.Uvt;
            var pctCap = subtotal * 0.30m;
            total += Math.Min(d.AfcMonthlyAmount, Math.Min(monthlyCap, pctCap));
        }
        return total;
    }

    public decimal CalculateProcedureTwo(decimal grossIncome, LegalParameter param, decimal mandatoryContributions)
    {
        var baseGravable = Math.Max(0, grossIncome - mandatoryContributions);
        var baseUvt = baseGravable / param.Uvt;
        if (baseUvt < param.MinimumWithholdingUvt) return 0m;

        decimal rate;
        if (baseUvt < 95) rate = 0;
        else if (baseUvt < 150) rate = 1.5m;
        else if (baseUvt < 360) rate = 3.5m;
        else if (baseUvt < 640) rate = 7.0m;
        else if (baseUvt < 945) rate = 12.0m;
        else if (baseUvt < 2300) rate = 20.0m;
        else rate = 35.0m;

        return RoundCurrency(baseGravable * rate / 100m);
    }

    public List<WithholdingTaxBracket> GetTableForYear(int year)
    {
        if (year >= 2026) return ProcedureOneTable2026;
        return ProcedureOneTable2025;
    }

    public async Task<List<WithholdingTaxBracket>> GetTableForYearFromDbAsync(int year)
    {
        var fromDb = await _db.WithholdingTaxBrackets
            .Where(x => x.Year == year && x.Procedure == "1")
            .OrderBy(x => x.FromUvt)
            .ToListAsync();
        if (fromDb.Count > 0) return fromDb;
        return GetTableForYear(year);
    }

    public async Task SeedTableAsync(int year)
    {
        var existing = await _db.WithholdingTaxBrackets.AnyAsync(x => x.Year == year);
        if (existing) return;
        foreach (var b in GetTableForYear(year))
        {
            _db.WithholdingTaxBrackets.Add(new WithholdingTaxBracket
            {
                Year = b.Year,
                FromUvt = b.FromUvt,
                ToUvt = b.ToUvt,
                MarginalRate = b.MarginalRate,
                BaseTaxUvt = b.BaseTaxUvt,
                Procedure = b.Procedure
            });
        }
        await _db.SaveChangesAsync();
    }

    private static decimal RoundCurrency(decimal value) => Math.Round(value, 0, MidpointRounding.AwayFromZero);
}

/// <summary>
/// Deducciones opcionales del Art. 387 ET (reformado por Art. 28 Ley 2277/2022).
/// Se calculan sobre el subtotal después de la exención del 25% (Art. 383 ET num. 3).
/// </summary>
public class Art387Deductions
{
    public bool HasDependents { get; set; }
    public bool HousingInterestEnabled { get; set; }
    public bool PrepaidHealthEnabled { get; set; }
    public decimal AfcMonthlyAmount { get; set; }

    public static Art387Deductions From(Employee e) => new()
    {
        HasDependents = e.HasDependents,
        HousingInterestEnabled = e.HousingInterestEnabled,
        PrepaidHealthEnabled = e.PrepaidHealthEnabled,
        AfcMonthlyAmount = e.AfcMonthlyAmount
    };
}
