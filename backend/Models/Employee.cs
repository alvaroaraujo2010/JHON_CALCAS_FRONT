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

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
