using ContaNexo.API.Data;
using ContaNexo.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ContaNexo.API.Services;

/// <summary>
/// Acceso a parámetros legales colombianos por año gravable.
/// Provee defaults de 2025/2026 si no existen registros en BD.
/// </summary>
public class LegalParameterService
{
    private readonly AppDbContext _db;
    private static readonly Dictionary<int, LegalParameter> _defaults = new()
    {
        [2025] = new LegalParameter
        {
            Year = 2025,
            Smlmv = 1_423_500m,
            Uvt = 49_799m,
            TransportAllowance = 200_000m,
            TransportAllowanceTop = 2m,
            MinimumWithholdingUvt = 95m,
            ExemptIncomeUvt = 240m,
            MaxHealthIbcSmlmv = 25m,
            ArlRiskOneRate = 0.522m,
            EmployerHealthRate = 8.5m,
            EmployerPensionRate = 12.0m,
            CompensationFundRate = 4.0m,
            SenaRate = 2.0m,
            IcbfRate = 3.0m,
            EmployeeHealthRate = 4.0m,
            EmployeePensionRate = 4.0m,
            SolidarityFundLowRate = 1.0m,
            SolidarityFundHighRate = 1.2m,
            PrimaYearFraction = 1m,
            CesantiasYearFraction = 1m,
            CesantiasInterestRate = 12m,
            VacationDaysPerYear = 15m,
            EffectiveFrom = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Notes = "Decreto 1184/2024 (SMLMV), Decreto 1312/2024 (aux. transporte), Resolución DIAN 233/2024 (UVT)"
        },
        [2026] = new LegalParameter
        {
            Year = 2026,
            Smlmv = 1_750_905m,
            Uvt = 53_286m,
            TransportAllowance = 249_095m,
            TransportAllowanceTop = 2m,
            MinimumWithholdingUvt = 95m,
            ExemptIncomeUvt = 240m,
            MaxHealthIbcSmlmv = 25m,
            ArlRiskOneRate = 0.522m,
            EmployerHealthRate = 8.5m,
            EmployerPensionRate = 12.0m,
            CompensationFundRate = 4.0m,
            SenaRate = 2.0m,
            IcbfRate = 3.0m,
            EmployeeHealthRate = 4.0m,
            EmployeePensionRate = 4.0m,
            SolidarityFundLowRate = 1.0m,
            SolidarityFundHighRate = 1.2m,
            PrimaYearFraction = 1m,
            CesantiasYearFraction = 1m,
            CesantiasInterestRate = 12m,
            VacationDaysPerYear = 15m,
            EffectiveFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Notes = "Decreto 0955/2025 (SMLMV), Decreto 0956/2025 (aux. transporte), Resolución DIAN 164/2025 (UVT)"
        }
    };

    public LegalParameterService(AppDbContext db) => _db = db;

    public async Task<LegalParameter> GetByYearAsync(int year)
    {
        var p = await _db.LegalParameters.FirstOrDefaultAsync(x => x.Year == year);
        if (p != null) return p;
        if (_defaults.TryGetValue(year, out var def)) return def;
        var latest = await _db.LegalParameters.OrderByDescending(x => x.Year).FirstOrDefaultAsync();
        if (latest != null) return latest;
        if (_defaults.Count > 0) return _defaults.Values.OrderByDescending(x => x.Year).First();
        throw new InvalidOperationException($"No hay parámetros legales configurados para el año {year}");
    }

    public static int GetYearFromDate(DateTime date) => date.Year;

    public async Task<int> GetYearFromPeriodAsync(string periodStart, string periodEnd)
    {
        if (DateTime.TryParse(periodEnd, out var end)) return end.Year;
        if (DateTime.TryParse(periodStart, out var start)) return start.Year;
        return DateTime.UtcNow.Year;
    }

    public async Task<LegalParameter> GetForPeriodAsync(string periodStart, string periodEnd)
        => await GetByYearAsync(await GetYearFromPeriodAsync(periodStart, periodEnd));

    public IReadOnlyDictionary<int, LegalParameter> GetDefaults() => _defaults;

    public async Task<List<LegalParameter>> ListAllAsync()
    {
        var fromDb = await _db.LegalParameters.OrderByDescending(x => x.Year).ToListAsync();
        foreach (var kvp in _defaults)
        {
            if (!fromDb.Any(x => x.Year == kvp.Key))
            {
                fromDb.Add(kvp.Value);
            }
        }
        return fromDb.OrderByDescending(x => x.Year).ToList();
    }

    public async Task<LegalParameter> UpsertAsync(LegalParameter param)
    {
        var existing = await _db.LegalParameters.FirstOrDefaultAsync(x => x.Year == param.Year);
        if (existing == null)
        {
            param.CreatedAt = DateTime.UtcNow;
            param.UpdatedAt = DateTime.UtcNow;
            _db.LegalParameters.Add(param);
            await _db.SaveChangesAsync();
            return param;
        }
        existing.Smlmv = param.Smlmv;
        existing.Uvt = param.Uvt;
        existing.TransportAllowance = param.TransportAllowance;
        existing.TransportAllowanceTop = param.TransportAllowanceTop;
        existing.MinimumWithholdingUvt = param.MinimumWithholdingUvt;
        existing.ExemptIncomeUvt = param.ExemptIncomeUvt;
        existing.MaxHealthIbcSmlmv = param.MaxHealthIbcSmlmv;
        existing.ArlRiskOneRate = param.ArlRiskOneRate;
        existing.EmployerHealthRate = param.EmployerHealthRate;
        existing.EmployerPensionRate = param.EmployerPensionRate;
        existing.CompensationFundRate = param.CompensationFundRate;
        existing.SenaRate = param.SenaRate;
        existing.IcbfRate = param.IcbfRate;
        existing.EmployeeHealthRate = param.EmployeeHealthRate;
        existing.EmployeePensionRate = param.EmployeePensionRate;
        existing.SolidarityFundLowRate = param.SolidarityFundLowRate;
        existing.SolidarityFundHighRate = param.SolidarityFundHighRate;
        existing.PrimaYearFraction = param.PrimaYearFraction;
        existing.CesantiasYearFraction = param.CesantiasYearFraction;
        existing.CesantiasInterestRate = param.CesantiasInterestRate;
        existing.VacationDaysPerYear = param.VacationDaysPerYear;
        existing.EffectiveFrom = param.EffectiveFrom;
        existing.Notes = param.Notes;
        existing.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return existing;
    }
}
