using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using GestionContactosAPI.DTOs;
using GestionContactosAPI.Models;

namespace GestionContactosAPI.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly DbA358b2Pam3Context _context;
    private readonly PasswordHasher<Usuario> _passwordHasher = new();

    public AuthController(IConfiguration configuration, DbA358b2Pam3Context context)
    {
        _configuration = configuration;
        _context = context;
    }

    /// <summary>
    /// Registra un usuario nuevo. Responde 409 si el email ya existe.
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await _context.Usuarios.AnyAsync(u => u.Email == email))
        {
            return Conflict(new { mensaje = "Ya existe un usuario registrado con ese email" });
        }

        var usuario = new Usuario
        {
            Nombre = request.Nombre,
            Email = email
        };
        usuario.Password = _passwordHasher.HashPassword(usuario, request.Password);

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, new
        {
            mensaje = "Usuario registrado correctamente",
            id = usuario.Id,
            email = usuario.Email
        });
    }

    /// <summary>
    /// Valida las credenciales contra la base y devuelve un JWT.
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == email);

        if (usuario == null)
        {
            return Unauthorized(new { mensaje = "Credenciales inválidas" });
        }

        bool passwordValida = false;
        try
        {
            var result = _passwordHasher.VerifyHashedPassword(usuario, usuario.Password, request.Password);
            passwordValida = (result != PasswordVerificationResult.Failed);
        }
        catch
        {
            passwordValida = false;
        }

        // Compatibilidad: permite login si la contraseña en la base de datos estaba en texto plano
        if (!passwordValida && usuario.Password == request.Password)
        {
            passwordValida = true;
        }

        if (!passwordValida)
        {
            return Unauthorized(new { mensaje = "Credenciales inválidas" });
        }

        return Ok(new
        {
            mensaje = "Autenticación exitosa",
            token = GenerarTokenJwt(usuario)
        });
    }

    private string GenerarTokenJwt(Usuario usuario)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var secretKey = jwtSettings["Key"]!;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Email, usuario.Email),
            new Claim(ClaimTypes.Name, usuario.Nombre ?? usuario.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
