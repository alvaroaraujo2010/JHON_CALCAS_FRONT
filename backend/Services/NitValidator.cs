using System.Text.RegularExpressions;

namespace ContaNexo.API.Services;

/// <summary>
/// Validador y calculador del dígito de verificación (DV) del NIT colombiano
/// según el algoritmo oficial de la DIAN (módulo 11, Art. 5 Resolución
/// 0019 del 24 de enero de 2003, hoy compilado en el Art. 1.6.1.1.5
/// del DUR 1625/2016).
///
/// Pesos: 3, 7, 13, 17, 19, 23, 29, 37, 41, 43, 47, 53, 59, 67, 71
/// de derecha a izquierda sobre los dígitos del NIT (sin DV).
///
/// Resultado:
///   - Si residuo = 0 → DV = 0
///   - Si residuo = 1 → DV = 1
///   - Si residuo >= 2 → DV = 11 - residuo
/// </summary>
public static class NitValidator
{
    private static readonly int[] Weights =
        { 3, 7, 13, 17, 19, 23, 29, 37, 41, 43, 47, 53, 59, 67, 71 };

    /// <summary>Tipos de documento de identificación (DIAN, Resolución 000019/2003).</summary>
    public enum DocumentType
    {
        Cedula = 13,
        NIT = 31,
        CedulaExtranjeria = 22,
        Pasaporte = 41,
        TarjetaIdentidad = 12,
        RegistroCivil = 11,
        PEP = 47,
        NitExtranjeria = 33,
    }

    public static string NormalizeNit(string? nit)
    {
        if (string.IsNullOrWhiteSpace(nit)) return string.Empty;
        return Regex.Replace(nit, "[^0-9]", "");
    }

    public static bool IsValidNit(string? nit)
    {
        var n = NormalizeNit(nit);
        return n.Length >= 6 && n.Length <= 15 && n.All(char.IsDigit);
    }

    /// <summary>Calcula el dígito de verificación. Devuelve '0' si NIT vacío.</summary>
    public static string CalculateDv(string? nit)
    {
        var n = NormalizeNit(nit);
        if (n.Length == 0) return "0";
        if (!n.All(char.IsDigit) || n.Length > 15)
            throw new ArgumentException($"NIT inválido: '{nit}'");

        // Aplicar pesos de derecha a izquierda.
        var digits = n.Select(c => int.Parse(c.ToString())).ToArray();
        var weightIndex = 0;
        int sum = 0;
        for (int i = digits.Length - 1; i >= 0; i--)
        {
            sum += digits[i] * Weights[weightIndex % Weights.Length];
            weightIndex++;
        }
        var mod = sum % 11;
        var dv = mod < 2 ? mod : 11 - mod;
        return dv.ToString();
    }

    /// <summary>Valida que el DV calculado coincida con el DV proporcionado.</summary>
    public static bool Verify(string? nit, string? dv)
    {
        if (string.IsNullOrWhiteSpace(nit) || string.IsNullOrWhiteSpace(dv)) return false;
        var calculated = CalculateDv(nit);
        return string.Equals(calculated, dv.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Devuelve el NIT formateado con puntos cada 3 dígitos desde la derecha: 900.123.456-7</summary>
    public static string Format(string? nit, string? dv)
    {
        var n = NormalizeNit(nit);
        if (n.Length == 0) return string.Empty;
        if (n.Length <= 3) return n + (string.IsNullOrEmpty(dv) ? "" : $"-{dv}");

        // Formato colombiano: bloques de 3 dígitos desde la derecha.
        // 800251957  → 800.251.957
        // 9001234567 → 9.001.234.567
        var withDots = "";
        var count = 0;
        for (int i = n.Length - 1; i >= 0; i--)
        {
            withDots = n[i] + withDots;
            count++;
            if (count == 3 && i > 0)
            {
                withDots = "." + withDots;
                count = 0;
            }
        }
        return string.IsNullOrEmpty(dv) ? withDots : $"{withDots}-{dv}";
    }
}
