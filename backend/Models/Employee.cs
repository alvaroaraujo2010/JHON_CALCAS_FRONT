namespace ContaNexo.API.Models;

public class Employee
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Position { get; set; }
    public string? Department { get; set; }
    public DateTime HireDate { get; set; } = DateTime.UtcNow;
    public string? TaxId { get; set; }
    public string? BankAccount { get; set; }
    public string? BankName { get; set; }
    public string? BankAccountType { get; set; } // savings | checking
    public decimal BaseSalary { get; set; }
    public bool IsActive { get; set; } = true;

    // ─── Ley colombiana (Ley 50/1990, CST, Ley 1607/2012) ────────────
    public string ContractType { get; set; } = "indefinido"; // indefinido | fijo | obra_labor | prestacion
    public bool IntegralSalary { get; set; } = false; // >13 SMLMV, no aplica auxilio ni prestaciones separadas
    public string? TerminationReason { get; set; } // retiro_voluntario | sin_justa_causa | justa_causa | pension
    public DateTime? TerminationDate { get; set; }
    public bool WithholdingProcedure2 { get; set; } = false; // Procedimiento 2 simplificado (art. 383 ET)

    // Auxilio de transporte: solo para empleados con salario < 2 SMLMV.
    // Para salario integral NO aplica.
    // Se calcula automáticamente; este flag lo fuerza a on/off (caso de dependientes).
    public bool? TransportAllowanceOverride { get; set; }

    // Fondo de Solidaridad Pensional: para empleados con IBC > 4 SMLMV.
    // Porcentaje aplicado al IBC que excede 4 SMLMV (1% o 1.2% según rango).
    public decimal? SolidarityFundOverride { get; set; }

    // ─── Deducciones Art. 387 ET (modificado por Art. 28 Ley 2277/2022) ──
    // 1 dependiente económico: 10% del ingreso bruto, tope 32 UVT/mes.
    public bool HasDependents { get; set; } = false;

    // Intereses de vivienda: tope 100 UVT/mes.
    public bool HousingInterestEnabled { get; set; } = false;

    // Medicina prepagada: tope 16 UVT/mes (no debe estar cubierta por POS).
    public bool PrepaidHealthEnabled { get; set; } = false;

    // Aportes voluntarios a AFC/FVP: tope 30% del ingreso, sin exceder 3.800 UVT/año.
    public decimal AfcMonthlyAmount { get; set; } = 0m;

    // ─── PILA — Anexo 1 Resolución 1736/2022 (compilada en Resolución 2382/2022) ──
    /// <summary>Tipo de cotizante (Anexo 1 Res. 1736/2022). 01=Dependiente, 02=Doméstico, 03=Independiente, 04=Madre sustituta, 12=Aprendiz SENA, etc.</summary>
    public string CotizanteTipo { get; set; } = "01";
    /// <summary>Subtipo de cotizante (Anexo 1). Vacío para dependientes. Para independientes: 00=Total, 01=Parcial. Para aprendices: 1=Etapa lectiva, 2=Productiva.</summary>
    public string CotizanteSubtipo { get; set; } = "00";
    /// <summary>Código EPS (ej: EPS001 Sanitas, EPS002 Nueva EPS, EPS005 Sanitas, EPS010 Sura, EPS017 Famisanar, EPS018 SOS).</summary>
    public string? OperatorEps { get; set; }
    /// <summary>Código AFP (ej: AFP01 Porvenir, AFP02 Protección, AFP03 Old Mutual, AFP04 Colfondos).</summary>
    public string? OperatorPension { get; set; }
    /// <summary>Código ARL (ej: ARL01 Positiva, ARL02 Seguros Bolívar, ARL03 Liberty, ARL04 Suratep).</summary>
    public string? OperatorArl { get; set; }
    /// <summary>Código Caja de Compensación (ej: CCF01 Compensar, CCF07 Cafam, CCF23 Comfenalco, CCF45 Comfamiliar).</summary>
    public string? OperatorCcf { get; set; }
    /// <summary>Clase de riesgo ARL (1-5).</summary>
    public int ArlRiskClass { get; set; } = 1;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
