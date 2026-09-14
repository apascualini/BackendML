using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Bogus; 

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// // 1. Agregar DbContext en memoria
// builder.Services.AddDbContext<AppDbContext>(opt => opt.UseInMemoryDatabase("ProductosDb"));
builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlite("Data Source=productos.db"));

// 2. Configurar CORS para React (Vite)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173")// URL por defecto de Vite
              .WithOrigins("http://localhost:5174")// URL por defecto de React
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Interfaz gráfica visual (estilo Swagger)
    app.MapScalarApiReference(); 
}

// 3. Activar la política de CORS
app.UseCors("AllowReactApp");

app.UseAuthorization();
app.MapControllers();


using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    if (!context.Productos.Any())
    {        
        var categorias = new[] { "Bebidas", "Bazar", "Almacén", "Electro", "Hogar" };

        var faker = new Faker<Producto>("es") // Configurado en español
            .RuleFor(p => p.Nombre, f => f.Commerce.ProductName())
            .RuleFor(p => p.Precio, f => decimal.Parse(f.Commerce.Price(1000, 100000, 2)))
            .RuleFor(p => p.Categoria, f => f.PickRandom(categorias));

        var productosFalsos = faker.Generate(100); // Genera 100 registros al instante

        context.Productos.AddRange(productosFalsos);
        await context.SaveChangesAsync();
    }
}



app.Run();



