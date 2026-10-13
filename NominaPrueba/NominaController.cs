using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using NominaPrueba;

namespace NominaDecrypt;

[ApiController]
[Route("nomina")]
[RequestSizeLimit(10 * 1024 * 1024)]
public class NominaController(NominaCipher cipher) : ControllerBase
{
    [HttpPost("decrypt")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Decrypt([FromForm] IFormFile? file, CancellationToken cancellationToken)
    {
        var form = await Request.ReadFormAsync(cancellationToken);
        if (form.Files.Count != 1 || file is null || file.Length == 0)
            return BadRequest("Envia un solo archivo no vacio en el campo file.");
        if (file.Length % 8 != 0)
            return BadRequest("Archivo cifrado incompleto o formato incorrecto: el tamano debe ser multiplo de 8 bytes.");

        using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, cancellationToken);
        try
        {
            return Content(cipher.Decrypt(buffer.ToArray()), "application/xml");
        }
        catch (CryptographicException)
        {
            return BadRequest("No se pudo descifrar: verifica el archivo original y la clave configurada.");
        }
    }

    [HttpPost("encrypt")]
    [Consumes("application/json")]
    public IActionResult Encrypt([FromBody] string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return BadRequest("El contenido no puede estar vacio.");
        return File(cipher.Encrypt(content), "application/octet-stream", "Control_de_nomina.enc");
    }
}
