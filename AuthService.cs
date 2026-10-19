using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore; // ¡Importante para AnyAsync y FirstOrDefaultAsync!
using Microsoft.Extensions.Options;   // ¡Importante para IOptions!
using Microsoft.IdentityModel.Tokens;
using Models;

public interface IAuthService
{
    Task<Usuario?> Registrar(string email, string password);
    Task<string?> Login(string email, string password);
}

public class AuthService : IAuthService
{
    private readonly AppDbContext _context; 
    private readonly JwtOptions _jwtOptions; 

    // El constructor ahora recibe correctamente las opciones mapeadas de JWT
    public AuthService(AppDbContext context, IOptions<JwtOptions> jwtOptions)
    {
        _context = context;
        _jwtOptions = jwtOptions.Value; // Extrae los datos limpios
    }

    public async Task<Usuario?> Registrar(string email, string password)
    {
        // 1. Verificar si ya existe el correo
        if (await _context.Usuarios.AnyAsync(u => u.Email == email)) return null;

        // 2. Hashear contraseña de forma segura con BCrypt
        string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

        var nuevoUsuario = new Usuario { Email = email, PasswordHash = passwordHash };
        _context.Usuarios.Add(nuevoUsuario);
        await _context.SaveChangesAsync();

        return nuevoUsuario;
    }

    public async Task<string?> Login(string email, string password)
    {
        // 1. Buscar usuario por email
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == email);
        if (usuario == null) return null;

        // 2. Verificar contraseña hash vs texto plano
        if (!BCrypt.Net.BCrypt.Verify(password, usuario.PasswordHash)) return null;

        // 3. Si es válido, generas el JWT firmado
        string token = GenerarTokenJwt(usuario); 
        return token;
    }
    
    private string GenerarTokenJwt(Usuario usuario) 
    { 
        // 1. Definimos la información que viajará pública pero encriptada (Claims)
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Email, usuario.Email),
            new Claim(ClaimTypes.Role, usuario.Rol) 
        };

        // 2. Preparamos la clave secreta segura desde las opciones inyectadas
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // 3. Creamos la estructura completa del JWT
        var tokenDescriptor = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: DateTime.Now.AddHours(2), // Expira automáticamente en 2 horas
            signingCredentials: creds
        );

        // 4. Serializamos el objeto del token a una cadena de texto (String)
        return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
    }
}
