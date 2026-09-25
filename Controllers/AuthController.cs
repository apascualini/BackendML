using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore; // ¡Importante para AnyAsync y FirstOrDefaultAsync!
using Microsoft.Extensions.Options;   // ¡Importante para IOptions!
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Mvc; 
using Models;
using Microsoft.AspNetCore.Authorization; 

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    public AuthController(IAuthService authService) => _authService = authService;

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] UserRegisterDto dto)
    {
        var user = await _authService.Registrar(dto.Email, dto.Password);
        if (user == null) return BadRequest(new { mensaje = "El usuario ya existe." });
        return Ok(new { mensaje = "Usuario creado con éxito" });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] UserLoginDto dto)
    {
        var token = await _authService.Login(dto.Email, dto.Password);
        if (token == null) return Unauthorized(new { mensaje = "Credenciales inválidas." });

        // Configuración segura de la Cookie HttpOnly
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,         // Protege contra robo por código JavaScript (XSS)
            Secure = true,           // Fuerza el envío exclusivo mediante HTTPS en producción
            SameSite = SameSiteMode.Lax, // Mitiga ataques CSRF
            Expires = DateTime.UtcNow.AddHours(2) // Tiempo coincidente con la expiración del JWT
        };

        // Añadir la cookie a la respuesta HTTP
        Response.Cookies.Append("X-Access-Token", token, cookieOptions);

        return Ok(new { user = dto.Email });
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        try
        {
            // Configuración de la cookie para eliminarla
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTime.UtcNow.AddDays(-1), // Fecha pasada para eliminar la cookie
                Path = "/"
            };

            Response.Cookies.Delete("X-Access-Token", cookieOptions);
            return Ok(new { mensaje = "Sesión cerrada correctamente." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { mensaje = "Error al cerrar sesión.", detalle = ex.Message });
        }
    }

    // GET: api/auth/status
    [Authorize] // Bloquea automáticamente si el JWT de la cookie es inválido o expiró
    [HttpGet("status")]
    public IActionResult GetAuthStatus()
    {
        // Extraemos de forma segura el ID y el Email almacenados dentro del token
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;

        return Ok(new
        {
            autenticado = true,
            usuario = new { id = userId, email = userEmail }
        });
    }
}
