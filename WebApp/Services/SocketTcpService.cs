using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Services;

public class SocketTcpService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SocketTcpService> _logger;

    private const int Port = 6061;

    public SocketTcpService(
        IServiceScopeFactory scopeFactory,
        ILogger<SocketTcpService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var listener = new TcpListener(IPAddress.Any, Port);

        listener.Start();

        _logger.LogInformation(
            "Socket TCP iniciado correctamente en el puerto {Port}",
            Port);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var client = await listener.AcceptTcpClientAsync(stoppingToken);

                _ = Task.Run(
                    () => HandleClientAsync(client, stoppingToken),
                    stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // La aplicación se está cerrando.
        }
        finally
        {
            listener.Stop();
        }
    }

    private async Task HandleClientAsync(
        TcpClient client,
        CancellationToken cancellationToken)
    {
        using (client)
        {
            try
            {
                using NetworkStream stream = client.GetStream();

                byte[] buffer = new byte[8192];

                int bytesRead = await stream.ReadAsync(
                    buffer,
                    cancellationToken);

                if (bytesRead == 0)
                    return;

                string request = Encoding.UTF8
                    .GetString(buffer, 0, bytesRead)
                    .Trim();

                _logger.LogInformation(
                    "Solicitud TCP recibida: {Request}",
                    request);

                string response = await ProcessRequestAsync(
                    request);

                byte[] responseBytes = Encoding.UTF8.GetBytes(response);

                await stream.WriteAsync(
                    responseBytes,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error procesando conexión TCP.");
            }
        }
    }

    private async Task<string> ProcessRequestAsync(string request)
    {
        try
        {
            if (request.StartsWith("{insert:") &&
                request.EndsWith("}"))
            {
                string json = request.Substring(
                    8,
                    request.Length - 9);

                var producto = JsonSerializer.Deserialize<Producto>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                if (producto == null)
                {
                    return JsonSerializer.Serialize(new
                    {
                        statusCode = 400,
                        data = "Producto inválido"
                    });
                }

                using IServiceScope scope =
                    _scopeFactory.CreateScope();

                var context = scope.ServiceProvider
                    .GetRequiredService<AppDbContext>();

                context.Productos.Add(producto);

                await context.SaveChangesAsync();

                return JsonSerializer.Serialize(new
                {
                    statusCode = 200,
                    data = producto
                });
            }

            if (request.StartsWith("{get:") &&
                request.EndsWith("}"))
            {
                string element = request.Substring(
                    5,
                    request.Length - 6);

                if (!int.TryParse(element, out int id))
                {
                    return JsonSerializer.Serialize(new
                    {
                        statusCode = 400,
                        data = "El ID debe ser un número"
                    });
                }

                using IServiceScope scope =
                    _scopeFactory.CreateScope();

                var context = scope.ServiceProvider
                    .GetRequiredService<AppDbContext>();

                var producto = await context.Productos
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (producto == null)
                {
                    return JsonSerializer.Serialize(new
                    {
                        statusCode = 404,
                        data = "Producto no encontrado"
                    });
                }

                return JsonSerializer.Serialize(new
                {
                    statusCode = 200,
                    data = producto
                });
            }

            return JsonSerializer.Serialize(new
            {
                statusCode = 400,
                data = "Comando no reconocido"
            });
        }
        catch (JsonException)
        {
            return JsonSerializer.Serialize(new
            {
                statusCode = 400,
                data = "JSON inválido"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error procesando solicitud TCP.");

            return JsonSerializer.Serialize(new
            {
                statusCode = 500,
                data = "Error interno del servidor"
            });
        }
    }
}
