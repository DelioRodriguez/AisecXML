using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SistemaGestionHumanaGR;

namespace NominaDecrypt
{
    [ApiController]
    [Route("nomina")]
    public class NominaController : ControllerBase
    {
        // Se invocan los métodos privados originales del cliente (cls_codificacion) por reflexión,
        // para no modificar su archivo. La clave se toma de App.config -> AppSettings["encryptor"].
        private static readonly MethodInfo DecryptFileMethod =
            typeof(cls_codificacion).GetMethod("DecryptFile", BindingFlags.NonPublic | BindingFlags.Static)!;
        private static readonly MethodInfo EncryptFileMethod =
            typeof(cls_codificacion).GetMethod("EncryptFile", BindingFlags.NonPublic | BindingFlags.Static)!;

        // Descifra el archivo .enc recibido usando el mismo cls_codificacion del cliente.
        [HttpPost("decrypt")]
        public async Task<IActionResult> Decrypt(IFormFile file)
        {
            if (file is null || file.Length == 0)
                return BadRequest("Archivo vacío.");

            var tempPath = Path.GetTempFileName();
            try
            {
                await using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write))
                {
                    await file.CopyToAsync(fs);
                }

                object?[] args = { tempPath, null };
                var result = DecryptFileMethod.Invoke(null, args);
                if (result is not StreamReader sr)
                    return StatusCode(500, "Error interno al descifrar el archivo: " + (args[1] as string));

                string xml;
                using (sr)
                {
                    xml = await sr.ReadToEndAsync();
                }
                return Content(xml, "application/xml");
            }
            catch (TargetInvocationException ex)
            {
                return StatusCode(500, "Error interno al descifrar el archivo: " + (ex.InnerException?.Message ?? ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error interno al descifrar el archivo: " + ex.Message);
            }
            finally
            {
                if (System.IO.File.Exists(tempPath))
                    System.IO.File.Delete(tempPath);
            }
        }

        // Cifra un XML (para probar ida y vuelta) usando el mismo cls_codificacion del cliente.
        [HttpPost("encrypt")]
        public async Task<IActionResult> Encrypt([FromBody] string content)
        {
            var tempPath = Path.GetTempFileName();
            try
            {
                await System.IO.File.WriteAllTextAsync(tempPath, content ?? string.Empty);

                object?[] args = { tempPath, null };
                var ok = (bool)EncryptFileMethod.Invoke(null, args)!;
                if (!ok)
                    return StatusCode(500, "Error interno al cifrar el archivo: " + (args[1] as string));

                var bytes = await System.IO.File.ReadAllBytesAsync(tempPath);
                return File(bytes, "application/octet-stream", "Control_de_nomina.enc");
            }
            catch (TargetInvocationException ex)
            {
                return StatusCode(500, "Error interno al cifrar el archivo: " + (ex.InnerException?.Message ?? ex.Message));
            }
            finally
            {
                if (System.IO.File.Exists(tempPath))
                    System.IO.File.Delete(tempPath);
            }
        }
    }
}
