using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Bogus;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var issuer = builder.Configuration["JwtSettings:Issuer"];
var audience = builder.Configuration["JwtSettings:Audience"];
var secretKey = builder.Configuration["JwtSettings:SecretKey"]; //se obtiene del secret del project

if (string.IsNullOrEmpty(secretKey))
{
    throw new InvalidOperationException("La clave secreta 'JwtSettings:SecretKey' no está configurada en tus User Secrets.");
}

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("JwtSettings"));

builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };

    // EVENTO CLAVE: Extraer el token de la cookie "X-Access-Token"
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            context.Token = context.Request.Cookies["X-Access-Token"];
            return Task.CompletedTask;
        }
    };
});


builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlite("Data Source=productos.db"));

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

// Activar la política de CORS
app.UseCors(policy => policy
    .WithOrigins("https://localhost:5173", "http://localhost:5173")
    .AllowAnyHeader().AllowAnyMethod().AllowCredentials()); 

app.UseAuthentication(); // Debe ir antes de Authorization
app.UseAuthorization();

app.MapControllers();


using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        context.Database.Migrate(); // <- Esto hace el "database update" automáticamente
    }

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



