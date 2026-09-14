using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;

[ApiController]
[Route("api/[controller]")]
public class ProductoController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProductoController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Producto>>> GetProductos(
        [FromQuery] string? buscar = null,
        [FromQuery] string? categoria = null,
        [FromQuery] int pagina = 1, 
        [FromQuery] int tamanioPagina = 5,
        [FromQuery] string? ordenarPor = null,
        [FromQuery] bool ascendente = true)
    {
        var query = _context.Productos.AsQueryable();

        // 1. Filtrado por término de búsqueda (Nombre)
        if (!string.IsNullOrWhiteSpace(buscar))
        {
            query = query.Where(p => p.Nombre.ToLower().Contains(buscar.ToLower()));
        }

        // 2. Filtrado por Categoría
        if (!string.IsNullOrWhiteSpace(categoria))
        {
            query = query.Where(p => p.Categoria.ToLower() == categoria.ToLower());
        }

        if(!string.IsNullOrEmpty(ordenarPor))
        {
            // Consulta dinámica con Where using System.Linq.Dynamic.Core;
            query = ascendente ? query.OrderBy($"{ordenarPor}") : query.OrderBy($"{ordenarPor} descending");
        }

        // 3. Obtener Total de registros antes de paginar
        var totalRegistros = await query.CountAsync();
        var totalPaginas = (int)Math.Ceiling((double)totalRegistros / tamanioPagina);

        // 4. Paginado con LINQ (Skip & Take)
        var items = await query
            .Skip((pagina - 1) * tamanioPagina)
            .Take(tamanioPagina)
            .ToListAsync();

        // Retornamos un objeto con la metadata para el frontend
        return Ok(new
        {
            totalRegistros,
            totalPaginas,
            paginaActual = pagina,
            tamanioPagina,
            items
        });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Producto>> GetProducto(int id)
    {
        var producto = await _context.Productos.FindAsync(id);
        if (producto == null)
        {
            return NotFound();
        }
        return producto;
    }

    [HttpPost]
    public async Task<ActionResult<Producto>> CreateProducto(Producto producto)
    {
        _context.Productos.Add(producto);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetProducto), new { id = producto.Id }, producto);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProducto(int id, Producto producto)
    {
        if (id != producto.Id)
        {
            return BadRequest();
        }

        _context.Entry(producto).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProducto(int id)
    {
        int filasAfectadas = await _context.Productos
            .Where(p => p.Id == id)
            .ExecuteDeleteAsync();

        if (filasAfectadas == 0)
        {
            return NotFound();
        }

        return NoContent();
    }
}