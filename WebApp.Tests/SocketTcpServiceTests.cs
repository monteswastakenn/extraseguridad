using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WebApp.Data;
using WebApp.Models;
using WebApp.Services;

namespace WebApp.Tests;

public class SocketTcpServiceTests
{
    private static SocketTcpService CrearServicio(
        string databaseName,
        out ServiceProvider provider)
    {
        var services = new ServiceCollection();

        services.AddLogging();

        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(databaseName));

        provider = services.BuildServiceProvider();

        var scopeFactory =
            provider.GetRequiredService<IServiceScopeFactory>();

        var logger =
            provider.GetRequiredService<ILogger<SocketTcpService>>();

        return new SocketTcpService(
            scopeFactory,
            logger);
    }

    private static async Task<string> ProcesarSolicitud(
        SocketTcpService service,
        string request)
    {
        var metodo = typeof(SocketTcpService)
            .GetMethod(
                "ProcessRequestAsync",
                BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(metodo);

        var tarea =
            (Task<string>)metodo!.Invoke(
                service,
                new object[] { request })!;

        return await tarea;
    }

    [Fact]
    public async Task InsertarProducto_DatosCorrectos_DebeRegresar200()
    {
        var databaseName = Guid.NewGuid().ToString();

        var service = CrearServicio(
            databaseName,
            out var provider);

        using (provider)
        {
            var respuesta = await ProcesarSolicitud(
                service,
                "{insert:{\"nombre\":\"Producto TCP\",\"precio\":150.50,\"stock\":10}}");

            Assert.Contains("\"statusCode\":200", respuesta);
            Assert.Contains("Producto TCP", respuesta);
        }
    }

    [Fact]
    public async Task InsertarProducto_JsonInvalido_DebeRegresar400()
    {
        var databaseName = Guid.NewGuid().ToString();

        var service = CrearServicio(
            databaseName,
            out var provider);

        using (provider)
        {
            var respuesta = await ProcesarSolicitud(
                service,
                "{insert:{json-invalido}}");

            Assert.Contains("\"statusCode\":400", respuesta);
            Assert.Contains("JSON inv", respuesta);
        }
    }

    [Fact]
    public async Task InsertarProducto_Vacio_DebeRegresar400()
    {
        var databaseName = Guid.NewGuid().ToString();

        var service = CrearServicio(
            databaseName,
            out var provider);

        using (provider)
        {
            var respuesta = await ProcesarSolicitud(
                service,
                "{insert:null}");

            Assert.Contains("\"statusCode\":400", respuesta);
        }
    }

    [Fact]
    public async Task ObtenerProducto_IdExistente_DebeRegresar200()
    {
        var databaseName = Guid.NewGuid().ToString();

        var service = CrearServicio(
            databaseName,
            out var provider);

        using (provider)
        {
            int id;

            using (var scope = provider.CreateScope())
            {
                var context =
                    scope.ServiceProvider
                        .GetRequiredService<AppDbContext>();

                var producto = new Producto
                {
                    Nombre = "Producto TCP",
                    Precio = 200,
                    Stock = 5
                };

                context.Productos.Add(producto);

                await context.SaveChangesAsync();

                id = producto.Id;
            }

            var respuesta = await ProcesarSolicitud(
                service,
                $"{{get:{id}}}");

            Assert.Contains("\"statusCode\":200", respuesta);
            Assert.Contains("Producto TCP", respuesta);
        }
    }

    [Fact]
    public async Task ObtenerProducto_IdInexistente_DebeRegresar404()
    {
        var databaseName = Guid.NewGuid().ToString();

        var service = CrearServicio(
            databaseName,
            out var provider);

        using (provider)
        {
            var respuesta = await ProcesarSolicitud(
                service,
                "{get:999}");

            Assert.Contains("\"statusCode\":404", respuesta);
            Assert.Contains("Producto no encontrado", respuesta);
        }
    }

    [Fact]
    public async Task ObtenerProducto_IdInvalido_DebeRegresar400()
    {
        var databaseName = Guid.NewGuid().ToString();

        var service = CrearServicio(
            databaseName,
            out var provider);

        using (provider)
        {
            var respuesta = await ProcesarSolicitud(
                service,
                "{get:abc}");

            Assert.Contains("\"statusCode\":400", respuesta);
            Assert.Contains("El ID debe ser", respuesta);
        }
    }

    [Fact]
    public async Task ComandoDesconocido_DebeRegresar400()
    {
        var databaseName = Guid.NewGuid().ToString();

        var service = CrearServicio(
            databaseName,
            out var provider);

        using (provider)
        {
            var respuesta = await ProcesarSolicitud(
                service,
                "{delete:1}");

            Assert.Contains("\"statusCode\":400", respuesta);
            Assert.Contains("Comando no reconocido", respuesta);
        }
    }
}
