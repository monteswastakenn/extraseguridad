using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductosController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProductosController(AppDbContext context)
    {
        _context = context;
    }

    // =====================================================
    // 1. GET - Obtener todos los productos
    // GET: /api/productos
    // =====================================================
    [HttpGet]
    public async Task<IActionResult> GetProductos()
    {
        var productos = await _context.Productos.ToListAsync();

        return Ok(new
        {
            statusCode = 200,
            data = productos
        });
    }


    // =====================================================
    // 2. GET - Obtener un producto por ID
    // GET: /api/productos/1
    // =====================================================
    [HttpGet("{id}")]
    public async Task<IActionResult> GetProducto(int id)
    {
        var producto = await _context.Productos.FindAsync(id);

        if (producto == null)
        {
            return NotFound(new
            {
                statusCode = 404,
                data = "Producto no encontrado"
            });
        }

        return Ok(new
        {
            statusCode = 200,
            data = producto
        });
    }


    // =====================================================
    // 3. POST - Crear un producto
    // POST: /api/productos
    // =====================================================
    [HttpPost]
    public async Task<IActionResult> CrearProducto(Producto producto)
    {
        _context.Productos.Add(producto);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            statusCode = 200,
            data = producto
        });
    }


    // =====================================================
    // 4. PUT - Actualizar un producto
    // PUT: /api/productos/1
    // =====================================================
    [HttpPut("{id}")]
    public async Task<IActionResult> ActualizarProducto(
        int id,
        Producto producto)
    {
        var productoExistente = await _context.Productos.FindAsync(id);

        if (productoExistente == null)
        {
            return NotFound(new
            {
                statusCode = 404,
                data = "Producto no encontrado"
            });
        }

        productoExistente.Nombre = producto.Nombre;
        productoExistente.Precio = producto.Precio;
        productoExistente.Stock = producto.Stock;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            statusCode = 200,
            data = productoExistente
        });
    }


    // =====================================================
    // 5. DELETE - Eliminar un producto
    // DELETE: /api/productos/1
    // =====================================================
    [HttpDelete("{id}")]
    public async Task<IActionResult> EliminarProducto(int id)
    {
        var producto = await _context.Productos.FindAsync(id);

        if (producto == null)
        {
            return NotFound(new
            {
                statusCode = 404,
                data = "Producto no encontrado"
            });
        }

        _context.Productos.Remove(producto);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            statusCode = 200,
            data = "Producto eliminado correctamente"
        });
    }


    // =====================================================
    // 6. GET - Buscar productos por nombre
    // GET: /api/productos/buscar/Laptop
    // =====================================================
    [HttpGet("buscar/{nombre}")]
    public async Task<IActionResult> BuscarProducto(string nombre)
    {
        var productos = await _context.Productos
            .Where(p => p.Nombre.Contains(nombre))
            .ToListAsync();

        return Ok(new
        {
            statusCode = 200,
            data = productos
        });
    }


    // =====================================================
    // 7. GET - Obtener productos con stock
    // GET: /api/productos/stock
    // =====================================================
    [HttpGet("stock")]
    public async Task<IActionResult> ObtenerProductosConStock()
    {
        var productos = await _context.Productos
            .Where(p => p.Stock > 0)
            .ToListAsync();

        return Ok(new
        {
            statusCode = 200,
            data = productos
        });
    }


    // =====================================================
    // 8. DELETE - Vaciar la base de datos
    // DELETE: /api/productos/vaciar
    // =====================================================
    [HttpDelete("vaciar")]
    public async Task<IActionResult> VaciarBaseDatos()
    {
        var productos = await _context.Productos.ToListAsync();

        _context.Productos.RemoveRange(productos);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            statusCode = 200,
            data = "Base de datos vaciada correctamente"
        });
    }


    // =====================================================
    // 9. POST - Crear backup de la base de datos
    // POST: /api/productos/backup
    // =====================================================
    [HttpPost("backup")]
    public async Task<IActionResult> CrearBackup()
    {
        string origen = Path.Combine(
            Directory.GetCurrentDirectory(),
            "webapp.db"
        );

        string carpetaBackup = Path.Combine(
            Directory.GetCurrentDirectory(),
            "backups"
        );

        Directory.CreateDirectory(carpetaBackup);

        string nombreBackup =
            $"backup_{DateTime.Now:yyyyMMdd_HHmmss}.db";

        string destino = Path.Combine(
            carpetaBackup,
            nombreBackup
        );

        await using (var origenStream = new FileStream(
            origen,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read))
        await using (var destinoStream = new FileStream(
            destino,
            FileMode.Create,
            FileAccess.Write))
        {
            await origenStream.CopyToAsync(destinoStream);
        }

        return Ok(new
        {
            statusCode = 200,
            data = new
            {
                mensaje = "Backup creado correctamente",
                archivo = nombreBackup
            }
        });
    }


    // =====================================================
    // 10. GET - Obtener información de los backups
    // GET: /api/productos/backup
    // =====================================================
    [HttpGet("backup")]
    public IActionResult ObtenerBackups()
    {
        string carpetaBackup = Path.Combine(
            Directory.GetCurrentDirectory(),
            "backups"
        );

        if (!Directory.Exists(carpetaBackup))
        {
            return Ok(new
            {
                statusCode = 200,
                data = new List<string>()
            });
        }

        var backups = Directory
            .GetFiles(carpetaBackup, "*.db")
            .Select(Path.GetFileName)
            .ToList();

        return Ok(new
        {
            statusCode = 200,
            data = backups
        });
    }
}