using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    // Mensaje que se cambia durante la demostración en vivo del pipeline
    public const string Mensaje = "API REST funcionando - version 1";

    // =====================================================
    // 11. GET - Estado de la API
    // GET: /api/health
    // =====================================================
    [HttpGet]
    public IActionResult GetHealth()
    {
        return Ok(new
        {
            statusCode = 200,
            data = new
            {
                estado = "OK",
                mensaje = Mensaje,
                fecha = DateTime.UtcNow
            }
        });
    }
}
