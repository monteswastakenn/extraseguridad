using Microsoft.AspNetCore.Mvc;
using WebApp.Controllers;

namespace WebApp.Tests;

public class HealthControllerTests
{
    // =====================================================
    // GET - Estado de la API
    // =====================================================
    [Fact]
    public void GetHealth_DebeRegresar200()
    {
        var controller = new HealthController();

        var resultado = controller.GetHealth();

        var okResult = Assert.IsType<OkObjectResult>(resultado);

        Assert.Equal(200, okResult.StatusCode);
    }

    [Fact]
    public void GetHealth_DebeIncluirMensaje()
    {
        var controller = new HealthController();

        var resultado = controller.GetHealth();

        var okResult = Assert.IsType<OkObjectResult>(resultado);
        var json = System.Text.Json.JsonSerializer.Serialize(okResult.Value);

        Assert.Contains("\"estado\":\"OK\"", json);
        Assert.Contains(HealthController.Mensaje, json);
    }
}
