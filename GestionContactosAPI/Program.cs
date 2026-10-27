using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using GestionContactosAPI.Models;
using GestionContactosAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Controladores
builder.Services.AddControllers();

// Configuración de Entity Framework Core (SQL Server para producción / SQLite para testing local)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<DbA358b2Pam3Context>(options =>
{
    if (!string.IsNullOrEmpty(connectionString) && connectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlite(connectionString);
    }
    else
    {
        options.UseSqlServer(connectionString);
    }
});

// Inyección de dependencias (Scoped para convivir con el ciclo de vida del DbContext)
builder.Services.AddScoped<ContactoService>();

// Configuración de autenticación con JWT Bearer
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["Key"]!;

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
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };
});

builder.Services.AddAuthorization();

// Swagger con soporte para ingresar el token Bearer
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "API Gestión de Contactos - TUDAI",
        Version = "v1",
        Description = "Actividad Obligatoria Integradora Nº 2 - Persistencia con Entity Framework Core y JWT"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Pegar directamente el token JWT aquí (sin la palabra Bearer)",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", doc),
            new List<string>()
        }
    });
});

var app = builder.Build();

// Inicialización de base de datos en entorno de desarrollo
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<DbA358b2Pam3Context>();
    if (context.Database.IsSqlite())
    {
        context.Database.EnsureCreated();
    }
}

// Swagger disponible para testing
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Gestión Contactos API v1");
    c.RoutePrefix = "swagger";
});

// Redirigir la raíz directamente a Swagger
app.MapGet("/", () => Results.Redirect("/swagger"));

app.UseHttpsRedirection();

// Middlewares de seguridad
app.UseAuthentication();
app.UseAuthorization();

// Minimal API contactos conectada a EF Core
app.MapGet("/minimal/contactos", async (ContactoService contactoService) =>
{
    return Results.Ok(await contactoService.ObtenerTodosAsync());
}).RequireAuthorization();

app.MapControllers();

app.Run();
