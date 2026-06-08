using ContaNexo.API.Models;

namespace ContaNexo.API.Services;

/// <summary>
/// Genera el archivo plano de la Planilla Integrada de Liquidación de Aportes
/// (PILA) según el Anexo Técnico 1 de la Resolución 1736/2022 (compilada
/// y vigente en la Resolución 2382/2022 del Ministerio de Salud y Protección Social).
///
/// Estructura del archivo (cada línea de 195 caracteres, terminada en CRLF):
///   Línea Tipo 01 — Encabezado:                1 por archivo
///   Línea Tipo 02 — Liquidación del cotizante:  N por archivo
///   Línea Tipo 03 — Totales:                   1 por archivo
///
/// NOTA: las posiciones/columnas siguen la especificación oficial.
/// Si el operador PILA (SOI, Asopagos, Mi Planilla, etc.) requiere un
/// layout ligeramente distinto, ajustar solo el formateo de cada línea.
/// </summary>
public class PilaFileService
{
    private const int LineLength = 195;
    private static readonly string Crlf = "\r\n";

    public class PilaContext
    {
        public string CompanyNit { get; set; } = string.Empty;
        public string CompanyDv { get; set; } = "0";
        public string Period { get; set; } = string.Empty; // YYYY-MM
        public int SequenceNumber { get; set; } = 1;       // Consecutivo de la planilla
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
        public List<SocialSecurityPayment> Payments { get; set; } = new();
    }

    public string Generate(PilaContext ctx)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append(BuildHeaderLine(ctx));
        decimal totalEmployees = 0, totalIbc = 0, totalEmployee = 0, totalEmployer = 0;
        foreach (var p in ctx.Payments)
        {
            sb.Append(BuildDetailLine(p, ctx));
            totalEmployees += 1;
            totalIbc += p.CapIbc;
            totalEmployee += p.EmployeeContributionTotal;
            totalEmployer += p.EmployerContributionTotal;
        }
        sb.Append(BuildTotalLine(ctx, totalEmployees, totalIbc, totalEmployee, totalEmployer));
        return sb.ToString();
    }

    private static string BuildHeaderLine(PilaContext ctx)
    {
        // Tipo 01: 195 caracteres
        var line = new System.Text.StringBuilder();
        line.Append("01");                                                    // 1-2  Tipo registro
        line.Append(Pad(ctx.CompanyNit, 16, '0'));                            // 3-18 NIT empresa
        line.Append(Pad(ctx.CompanyDv, 1, '0'));                              // 19   DV
        line.Append(Pad("", 8, ' '));                                         // 20-27 Número planilla operador (lo asigna operador)
        line.Append(Pad(ctx.Period.Replace("-", ""), 6, ' '));                 // 28-33 Período YYYYMM
        line.Append(Pad(ctx.SequenceNumber.ToString(), 8, '0'));              // 34-41 Consecutivo
        line.Append(Pad("", 16, ' '));                                        // 42-57 Nombre empresa (lo completa operador)
        line.Append(Pad(ctx.PaymentDate.ToString("yyyyMMdd"), 8, '0'));       // 58-65 Fecha pago
        line.Append(Pad("01", 2, '0'));                                       // 66-67 Modalidad (01=Planilla manual)
        line.Append(Pad("", 128, ' '));                                       // 68-195 relleno
        return Pad(line.ToString(), LineLength, ' ', rightPad: false) + Crlf;
    }

    private static string BuildDetailLine(SocialSecurityPayment p, PilaContext ctx)
    {
        var line = new System.Text.StringBuilder();
        line.Append("02");                                                    // 1-2  Tipo registro
        line.Append(Pad(ctx.CompanyNit, 16, '0'));                            // 3-18 NIT empresa
        line.Append(Pad(ctx.CompanyDv, 1, '0'));                              // 19   DV
        line.Append(Pad(p.TaxId ?? "", 16, ' '));                             // 20-35 NIT cotizante
        line.Append(Pad("", 1, ' '));                                         // 36   DV cotizante (no obligatorio para personas)
        line.Append(Pad(p.CotizanteTipo ?? "01", 2, '0'));                    // 37-38 Tipo cotizante
        line.Append(Pad(p.CotizanteSubtipo ?? "00", 2, '0'));                 // 39-40 Subtipo cotizante
        line.Append(Pad(p.NovedadTipo ?? "N", 1, ' '));                        // 41   Novedad
        line.Append(Pad(p.NovedadFechaInicio?.ToString("yyyyMMdd") ?? "", 8, ' ')); // 42-49 Fecha inicio novedad
        line.Append(Pad(p.NovedadFechaFin?.ToString("yyyyMMdd") ?? "", 8, ' '));    // 50-57 Fecha fin novedad
        line.Append(Pad(p.OperatorEps ?? "", 6, ' '));                        // 58-63 EPS
        line.Append(Pad(p.OperatorPension ?? "", 6, ' '));                    // 64-69 AFP
        line.Append(Pad(p.OperatorArl ?? "", 6, ' '));                        // 70-75 ARL
        line.Append(Pad(p.OperatorCcf ?? "", 6, ' '));                        // 76-81 Caja
        line.Append(Pad(Money(p.BaseSalary), 9, '0'));                        // 82-90 Salario
        line.Append(Pad(Money(p.CapIbc), 9, '0'));                            // 91-99 IBC
        line.Append(Pad(Money(p.EmployeeHealthContribution), 9, '0'));       // 100-108 Aporte salud empleado
        line.Append(Pad(Money(p.EmployerHealthContribution), 9, '0'));       // 109-117 Aporte salud empleador
        line.Append(Pad(Money(p.EmployeePensionContribution), 9, '0'));      // 118-126 Aporte pensión empleado
        line.Append(Pad(Money(p.EmployerPensionContribution), 9, '0'));      // 127-135 Aporte pensión empleador
        line.Append(Pad(Money(p.SolidarityFundContribution), 9, '0'));        // 136-144 Fondo solidaridad
        line.Append(Pad(Money(p.ArlContribution), 9, '0'));                   // 145-153 ARL
        line.Append(Pad(Money(p.CompensationFundContribution), 9, '0'));      // 154-162 Caja
        line.Append(Pad(Money(p.SenaContribution), 9, '0'));                  // 163-171 SENA
        line.Append(Pad(Money(p.IcbfContribution), 9, '0'));                  // 172-180 ICBF
        line.Append(Pad(Money(p.EmployeeContributionTotal + p.EmployerContributionTotal), 9, '0')); // 181-189 Total
        line.Append(Pad("", 6, ' '));                                         // 190-195 relleno
        return Pad(line.ToString(), LineLength, ' ', rightPad: false) + Crlf;
    }

    private static string BuildTotalLine(PilaContext ctx, decimal n, decimal ibc, decimal emp, decimal er)
    {
        var line = new System.Text.StringBuilder();
        line.Append("03");                                                    // 1-2 Tipo
        line.Append(Pad(ctx.CompanyNit, 16, '0'));                            // 3-18 NIT
        line.Append(Pad(ctx.CompanyDv, 1, '0'));                              // 19 DV
        line.Append(Pad(n.ToString("F0"), 5, '0'));                           // 20-24 # cotizantes
        line.Append(Pad(Money(ibc), 15, '0'));                                // 25-39 Total IBC
        line.Append(Pad(Money(emp), 15, '0'));                                // 40-54 Aportes empleado
        line.Append(Pad(Money(er), 15, '0'));                                 // 55-69 Aportes empleador
        line.Append(Pad(Money(emp + er), 15, '0'));                           // 70-84 Total
        line.Append(Pad("", 111, ' '));                                       // 85-195 relleno
        return Pad(line.ToString(), LineLength, ' ', rightPad: false) + Crlf;
    }

    /// <summary>Padding determinístico (izquierda o derecha) de una cadena a longitud fija.</summary>
    private static string Pad(string s, int len, char fill, bool rightPad = true)
    {
        s ??= "";
        if (s.Length == len) return s;
        if (s.Length > len) return s[..len];
        return rightPad ? s.PadRight(len, fill) : s.PadLeft(len, fill);
    }

    /// <summary>Convierte decimal a "NNNNNNNNN" (sin separadores) redondeado a entero.</summary>
    private static string Money(decimal v) => ((long)Math.Round(v, 0, MidpointRounding.AwayFromZero)).ToString();
}
