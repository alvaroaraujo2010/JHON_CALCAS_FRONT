using ContaNexo.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContaNexo.API.Controllers;

[ApiController]
[Route("api/validation")]
[Authorize]
public class ValidationController : ControllerBase
{
    /// <summary>
    /// Calcula el dígito de verificación de un NIT colombiano
    /// (módulo 11, algoritmo oficial DIAN).
    /// </summary>
    [HttpGet("nit/dv")]
    public ActionResult<object> CalculateDv([FromQuery] string nit)
    {
        if (string.IsNullOrWhiteSpace(nit))
            return BadRequest(new { message = "NIT requerido" });
        if (!NitValidator.IsValidNit(nit))
            return BadRequest(new { message = "NIT inválido. Debe tener entre 6 y 15 dígitos." });

        var dv = NitValidator.CalculateDv(nit);
        return Ok(new
        {
            nit = NitValidator.NormalizeNit(nit),
            dv,
            formatted = NitValidator.Format(nit, dv),
            valid = true
        });
    }

    /// <summary>
    /// Verifica que el NIT y su DV coincidan.
    /// </summary>
    [HttpGet("nit/verify")]
    public ActionResult<object> VerifyNit([FromQuery] string nit, [FromQuery] string dv)
    {
        var n = NitValidator.NormalizeNit(nit);
        var valid = NitValidator.Verify(n, dv);
        return Ok(new { nit = n, dv, valid, formatted = NitValidator.Format(n, dv) });
    }

    /// <summary>
    /// Lista los tipos de documento de identificación soportados
    /// (códigos DIAN Resolución 000019/2003).
    /// </summary>
    [HttpGet("document-types")]
    public ActionResult<object> GetDocumentTypes()
    {
        var types = Enum.GetValues<NitValidator.DocumentType>()
            .Select(t => new { code = (int)t, name = t.ToString() })
            .ToList();
        return Ok(types);
    }
}
