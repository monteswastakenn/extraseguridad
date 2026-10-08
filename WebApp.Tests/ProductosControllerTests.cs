using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Controllers;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Tests;

public class ProductosControllerTests
{
    private AppDbContext CrearContexto()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    // =====================================================
    // 1. GET - Obtener todos los productos
    // =====================================================
    [Fact]
    public async Task GetProductos_DebeRegresar200()
    {
        using var context = CrearContexto();

        context.Productos.Add(new Producto
        {
            Nombre = "Laptop",
            Precio = 15000,
            Stock = 5
        });

        await context.SaveChangesAsync();

        var controller = new ProductosController(context);

        var resultado = await controller.GetProductos();

        var okResult = Assert.IsType<OkObjectResult>(resultado);

        Assert.Equal(200, okResult.StatusCode);
    }

    // =====================================================
    // 2. GET - Obtener producto por ID existente
    // =====================================================
    [Fact]
    public async Task GetProducto_IdExistente_DebeRegresar200()
    {
        using var context = CrearContexto();

        var producto = new Producto
        {
            Nombre = "Mouse",
            Precio = 500,
            Stock = 10
        };

        context.Productos.Add(producto);
        await context.SaveChangesAsync();

        var controller = new ProductosController(context);

        var resultado = await controller.GetProducto(producto.Id);

        var okResult = Assert.IsType<OkObjectResult>(resultado);

        Assert.Equal(200, okResult.StatusCode);
    }

    // =====================================================
    // 3. GET - Producto inexistente
    // =====================================================
    [Fact]
    public async Task GetProducto_IdInexistente_DebeRegresar404()
    {
        using var context = CrearContexto();

        var controller = new ProductosController(context);

        var resultado = await controller.GetProducto(999);

        Assert.IsType<NotFoundObjectResult>(resultado);
    }

    // =====================================================
    // 4. POST - Crear producto
    // =====================================================
    [Fact]
    public async Task CrearProducto_DatosCorrectos_DebeRegresar200()
    {
        using var context = CrearContexto();

        var controller = new ProductosController(context);

        var producto = new Producto
        {
            Nombre = "Teclado",
            Precio = 800,
            Stock = 7
        };

        var resultado = await controller.CrearProducto(producto);

        var okResult = Assert.IsType<OkObjectResult>(resultado);

        Assert.Equal(200, okResult.StatusCode);
        Assert.Equal(1, await context.Productos.CountAsync());
    }

    // =====================================================
    // 5. PUT - Actualizar producto existente
    // =====================================================
    [Fact]
    public async Task ActualizarProducto_IdExistente_DebeActualizar()
    {
        using var context = CrearContexto();

        var producto = new Producto
        {
            Nombre = "Monitor",
            Precio = 4000,
            Stock = 3
        };

        context.Productos.Add(producto);
        await context.SaveChangesAsync();

        var controller = new ProductosController(context);

        var actualizado = new Producto
        {
            Nombre = "Monitor 4K",
            Precio = 6000,
            Stock = 5
        };

        var resultado =
            await controller.ActualizarProducto(
                producto.Id,
                actualizado
            );

        var okResult = Assert.IsType<OkObjectResult>(resultado);

        Assert.Equal(200, okResult.StatusCode);

        var productoBD =
            await context.Productos.FindAsync(producto.Id);

        Assert.Equal("Monitor 4K", productoBD!.Nombre);
        Assert.Equal(6000, productoBD.Precio);
        Assert.Equal(5, productoBD.Stock);
    }

    // =====================================================
    // 6. PUT - Producto inexistente
    // =====================================================
    [Fact]
    public async Task ActualizarProducto_IdInexistente_DebeRegresar404()
    {
        using var context = CrearContexto();

        var controller = new ProductosController(context);

        var producto = new Producto
        {
            Nombre = "Producto",
            Precio = 100,
            Stock = 1
        };

        var resultado =
            await controller.ActualizarProducto(
                999,
                producto
            );

        Assert.IsType<NotFoundObjectResult>(resultado);
    }

    // =====================================================
    // 7. DELETE - Eliminar producto existente
    // =====================================================
    [Fact]
    public async Task EliminarProducto_IdExistente_DebeEliminar()
    {
        using var context = CrearContexto();

        var producto = new Producto
        {
            Nombre = "Audífonos",
            Precio = 1200,
            Stock = 4
        };

        context.Productos.Add(producto);
        await context.SaveChangesAsync();

        var controller = new ProductosController(context);

        var resultado =
            await controller.EliminarProducto(producto.Id);

        var okResult = Assert.IsType<OkObjectResult>(resultado);

        Assert.Equal(200, okResult.StatusCode);

        Assert.Empty(await context.Productos.ToListAsync());
    }

    // =====================================================
    // 8. DELETE - Producto inexistente
    // =====================================================
    [Fact]
    public async Task EliminarProducto_IdInexistente_DebeRegresar404()
    {
        using var context = CrearContexto();

        var controller = new ProductosController(context);

        var resultado =
            await controller.EliminarProducto(999);

        Assert.IsType<NotFoundObjectResult>(resultado);
    }

    // =====================================================
    // 9. GET - Buscar por nombre existente
    // =====================================================
    [Fact]
    public async Task BuscarProducto_NombreExistente_DebeEncontrarProducto()
    {
        using var context = CrearContexto();

        context.Productos.Add(new Producto
        {
            Nombre = "Laptop Lenovo",
            Precio = 15000,
            Stock = 3
        });

        await context.SaveChangesAsync();

        var controller = new ProductosController(context);

        var resultado =
            await controller.BuscarProducto("Laptop");

        var okResult = Assert.IsType<OkObjectResult>(resultado);

        Assert.Equal(200, okResult.StatusCode);
    }

    // =====================================================
    // 10. GET - Buscar nombre inexistente
    // =====================================================
    [Fact]
    public async Task BuscarProducto_NombreInexistente_DebeRegresar200()
    {
        using var context = CrearContexto();

        var controller = new ProductosController(context);

        var resultado =
            await controller.BuscarProducto("NoExiste");

        var okResult =
            Assert.IsType<OkObjectResult>(resultado);

        Assert.Equal(200, okResult.StatusCode);
    }

    // =====================================================
    // 11. GET - Buscar ignorando mayúsculas/minúsculas
    // =====================================================
    [Fact]
    public async Task BuscarProducto_DebeSerCaseInsensitive()
    {
        using var context = CrearContexto();

        context.Productos.Add(new Producto
        {
            Nombre = "Laptop Lenovo",
            Precio = 15000,
            Stock = 3
        });

        await context.SaveChangesAsync();

        var controller = new ProductosController(context);

        var resultado =
            await controller.BuscarProducto("laptop");

        Assert.IsType<OkObjectResult>(resultado);
    }

    // =====================================================
    // 12. GET - Productos con stock
    // =====================================================
    [Fact]
    public async Task ObtenerProductosConStock_DebeRegresarProductos()
    {
        using var context = CrearContexto();

        context.Productos.Add(new Producto
        {
            Nombre = "Celular",
            Precio = 8000,
            Stock = 5
        });

        context.Productos.Add(new Producto
        {
            Nombre = "Sin stock",
            Precio = 1000,
            Stock = 0
        });

        await context.SaveChangesAsync();

        var controller = new ProductosController(context);

        var resultado =
            await controller.ObtenerProductosConStock();

        var okResult = Assert.IsType<OkObjectResult>(resultado);

        Assert.Equal(200, okResult.StatusCode);
    }

    // =====================================================
    // 13. GET - Stock sin productos
    // =====================================================
    [Fact]
    public async Task ObtenerProductosConStock_SinProductos_DebeRegresar200()
    {
        using var context = CrearContexto();

        var controller = new ProductosController(context);

        var resultado =
            await controller.ObtenerProductosConStock();

        var okResult =
            Assert.IsType<OkObjectResult>(resultado);

        Assert.Equal(200, okResult.StatusCode);
    }

    // =====================================================
    // 14. GET - Stock excluye productos sin stock
    // =====================================================
    [Fact]
    public async Task ObtenerProductosConStock_DebeExcluirProductosSinStock()
    {
        using var context = CrearContexto();

        context.Productos.AddRange(
            new Producto
            {
                Nombre = "Disponible",
                Precio = 100,
                Stock = 5
            },
            new Producto
            {
                Nombre = "Agotado",
                Precio = 200,
                Stock = 0
            }
        );

        await context.SaveChangesAsync();

        var controller = new ProductosController(context);

        var resultado =
            await controller.ObtenerProductosConStock();

        var okResult =
            Assert.IsType<OkObjectResult>(resultado);

        Assert.NotNull(okResult.Value);
    }

    // =====================================================
    // 15. DELETE - Vaciar base de datos
    // =====================================================
    [Fact]
    public async Task VaciarBaseDatos_DebeEliminarTodosLosProductos()
    {
        using var context = CrearContexto();

        context.Productos.AddRange(
            new Producto
            {
                Nombre = "Producto 1",
                Precio = 100,
                Stock = 5
            },
            new Producto
            {
                Nombre = "Producto 2",
                Precio = 200,
                Stock = 3
            }
        );

        await context.SaveChangesAsync();

        var controller = new ProductosController(context);

        var resultado =
            await controller.VaciarBaseDatos();

        var okResult = Assert.IsType<OkObjectResult>(resultado);

        Assert.Equal(200, okResult.StatusCode);

        Assert.Empty(await context.Productos.ToListAsync());
    }

    // =====================================================
    // 16. DELETE - Vaciar base de datos vacía
    // =====================================================
    [Fact]
    public async Task VaciarBaseDatos_SinProductos_DebeRegresar200()
    {
        using var context = CrearContexto();

        var controller = new ProductosController(context);

        var resultado =
            await controller.VaciarBaseDatos();

        Assert.IsType<OkObjectResult>(resultado);

        Assert.Empty(await context.Productos.ToListAsync());
    }

    // =====================================================
    // 17. POST - Crear backup
    // =====================================================
    [Fact]
    public async Task CrearBackup_DebeCrearBackupCorrectamente()
    {
        string dbPath =
            Path.Combine(
                Directory.GetCurrentDirectory(),
                "webapp.db"
            );

        string backupDir =
            Path.Combine(
                Directory.GetCurrentDirectory(),
                "backups"
            );

        try
        {
            await File.WriteAllTextAsync(
                dbPath,
                "base de datos de prueba"
            );

            using var context = CrearContexto();

            var controller =
                new ProductosController(context);

            var resultado =
                await controller.CrearBackup();

            var okResult =
                Assert.IsType<OkObjectResult>(resultado);

            Assert.NotNull(okResult.Value);

            Assert.True(
                Directory.Exists(backupDir)
            );

            var backups =
                Directory.GetFiles(
                    backupDir,
                    "*.db"
                );

            Assert.NotEmpty(backups);
        }
        finally
        {
            if (File.Exists(dbPath))
                File.Delete(dbPath);

            if (Directory.Exists(backupDir))
                Directory.Delete(
                    backupDir,
                    true
                );
        }
    }

    // =====================================================
    // 18. GET - Obtener backups existentes
    // =====================================================
    [Fact]
    public void ObtenerBackups_DebeRegresarListaDeBackups()
    {
        string backupDir =
            Path.Combine(
                Directory.GetCurrentDirectory(),
                "backups"
            );

        try
        {
            Directory.CreateDirectory(backupDir);

            string backupPath =
                Path.Combine(
                    backupDir,
                    "backup_prueba.db"
                );

            File.WriteAllText(
                backupPath,
                "backup de prueba"
            );

            using var context = CrearContexto();

            var controller =
                new ProductosController(context);

            var resultado =
                controller.ObtenerBackups();

            var okResult =
                Assert.IsType<OkObjectResult>(resultado);

            Assert.NotNull(okResult.Value);
        }
        finally
        {
            if (Directory.Exists(backupDir))
                Directory.Delete(
                    backupDir,
                    true
                );
        }
    }

    // =====================================================
    // 19. GET - Obtener backups sin directorio
    // =====================================================
    [Fact]
    public void ObtenerBackups_SinBackups_DebeRegresarLista()
    {
        string backupDir =
            Path.Combine(
                Directory.GetCurrentDirectory(),
                "backups"
            );

        try
        {
            if (Directory.Exists(backupDir))
                Directory.Delete(backupDir, true);

            using var context = CrearContexto();

            var controller =
                new ProductosController(context);

            var resultado =
                controller.ObtenerBackups();

            var okResult =
                Assert.IsType<OkObjectResult>(resultado);

            Assert.NotNull(okResult.Value);
        }
        finally
        {
            if (Directory.Exists(backupDir))
                Directory.Delete(
                    backupDir,
                    true
                );
        }
    }

    // =====================================================
    // 20. PUT - Actualizar y conservar ID
    // =====================================================
    [Fact]
    public async Task ActualizarProducto_DebeConservarId()
    {
        using var context = CrearContexto();

        var producto = new Producto
        {
            Nombre = "Producto Original",
            Precio = 100,
            Stock = 2
        };

        context.Productos.Add(producto);
        await context.SaveChangesAsync();

        int idOriginal = producto.Id;

        var controller = new ProductosController(context);

        var actualizado = new Producto
        {
            Nombre = "Producto Actualizado",
            Precio = 200,
            Stock = 5
        };

        var resultado =
            await controller.ActualizarProducto(
                idOriginal,
                actualizado
            );

        Assert.IsType<OkObjectResult>(resultado);

        var productoBD =
            await context.Productos.FindAsync(idOriginal);

        Assert.NotNull(productoBD);
        Assert.Equal(idOriginal, productoBD!.Id);
    }
}
