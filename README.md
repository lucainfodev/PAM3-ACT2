# Actividad Obligatoria Integradora Nº 2 - Programación de Aplicaciones Móviles III
## TUDAI / IUA - Tecnicatura Universitaria en Desarrollo de Aplicaciones Informáticas

---

### Trabajo Grupal: Persistencia con Entity Framework Core y Autenticación JWT
* **Repositorio Oficial:** [https://github.com/lucainfodev/PAM3-ACT2](https://github.com/lucainfodev/PAM3-ACT2)  
* **Asignatura:** Programación de Aplicaciones Móviles III  
* **Tecnología:** .NET / ASP.NET Core Web API, Entity Framework Core (Database First), SQL Server, JWT Bearer, Swagger UI  

---

## 📌 Documentación Técnica de Implementación

Este documento detalla la arquitectura, configuración e implementación de la **Actividad Obligatoria 2**, integrando acceso a datos relacionales con **Entity Framework Core (Database First)** y un sistema de **Autenticación y Autorización basado en tokens JWT**.

---

### 1. Instalación de Herramientas y Paquetes NuGet

#### Herramienta Global CLI:
Para la ejecución de tareas de ingeniería inversa (Scaffolding) y gestión de Entity Framework Core desde la consola, se instaló la herramienta global `dotnet-ef`:
```bash
dotnet tool install --global dotnet-ef
```

#### Paquetes NuGet Incorporados:
En el archivo `GestionContactosAPI.csproj` se agregaron los siguientes paquetes:
1. **`Microsoft.EntityFrameworkCore.SqlServer`**: Proveedor relacional para Microsoft SQL Server encargado de traducir consultas LINQ a T-SQL y gestionar la conexión con la base de datos.
2. **`Microsoft.EntityFrameworkCore.Design`**: Componentes de diseño para soporte de scaffolding e ingeniería inversa en tiempo de desarrollo.
3. **`Microsoft.AspNetCore.Authentication.JwtBearer`**: Middleware de autenticación para la validación de tokens JWT en solicitudes protegidas.
4. **`Swashbuckle.AspNetCore`**: Generación automática de especificación OpenAPI y documentación interactiva mediante Swagger UI.

---

### 2. Scaffold y Modelado de Datos (Contacto, Usuario y DbContext)

#### Ingeniería Inversa (Database First):
Se ejecutó el comando de scaffolding contra el servidor SQL Server provisto por la cátedra:
```bash
dotnet ef dbcontext scaffold "Server=SQL8002.site4now.net;Database=db_a358b2_pam3;User Id=db_a358b2_pam3_admin;Password=TudaiPam32548-;TrustServerCertificate=True;" Microsoft.EntityFrameworkCore.SqlServer -o Models -t Contacto -t Usuario -f
```

#### Entidades de Dominio:

##### `Models/Contacto.cs`
Representa la entidad `Contacto` mapeada a la tabla correspondiente:
* `Id` (`int`, Clave primaria auto-incremental / Identity).
* `Nombre` (`string`, obligatorio, longitud máxima 100).
* `Apellido` (`string`, obligatorio, longitud máxima 100).
* `Telefono` (`string`, opcional, longitud máxima 50).
* `Email` (`string`, opcional, validado con formato de correo electrónico).

##### `Models/Usuario.cs`
Representa la entidad `Usuario` utilizada para la autenticación:
* `Id` (`int`, Clave primaria auto-incremental).
* `Nombre` (`string`, opcional, longitud máxima 100).
* `Email` (`string`, obligatorio, validado con formato email).
* `Password` (`string`, obligatorio, almacena la credencial de acceso).

##### `Models/DbA358b2Pam3Context.cs` (DbContext)
Contexto principal de Entity Framework Core que hereda de `DbContext` y expone los `DbSet`:
* `public virtual DbSet<Contacto> Contactos { get; set; }`
* `public virtual DbSet<Usuario> Usuarios { get; set; }`
* Configuración de restricciones, claves primarias y mapeos de columnas mediante **Fluent API** en el método `OnModelCreating`.

---

### 3. Configuración del DbContext y Cadena de Conexión

#### Cadena de Conexión en `appsettings.json`:
Las credenciales del servidor SQL Server provisto se encuentran centralizadas en `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=SQL8002.site4now.net;Database=db_a358b2_pam3;User Id=db_a358b2_pam3_admin;Password=TudaiPam32548-;TrustServerCertificate=True;"
  }
}
```

#### Registro en el Contenedor de Dependencias (`Program.cs`):
```csharp
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<DbA358b2Pam3Context>(options =>
    options.UseSqlServer(connectionString));

// Inyección del servicio con ciclo de vida Scoped
builder.Services.AddScoped<ContactoService>();
```

*Nota técnica:* `ContactoService` se registra con ciclo de vida **Scoped** para coincidir con el ciclo de vida del `DbContext`, garantizando aislamiento de instancias por petición HTTP y evitando problemas de concurrencia o retención indebida de conexiones.

---

### 4. Servicio de Acceso a Datos (`Services/ContactoService.cs`)

Se reemplazó el almacenamiento en memoria por operaciones asíncronas con Entity Framework Core:
* **`ObtenerTodosAsync()`**: Consulta de lectura optimizada con `.AsNoTracking()` para minimizar el consumo de memoria del Change Tracker.
* **`ObtenerPorIdAsync(int id)`**: Búsqueda por clave primaria mediante `FindAsync(id)`.
* **`CrearAsync(Contacto contacto)`**: Alta y persistencia de entidad mediante `Add()` y `SaveChangesAsync()`.
* **`EditarAsync(int id, Contacto contacto)`**: Actualización de campos de contacto existente y persistencia transaccional.
* **`EliminarAsync(int id)`**: Remoción de entidad por identificador.

---

### 5. Controlador de Contactos (`Controllers/ContactosController.cs`)

Controlador protegido mediante el atributo `[Authorize]` que expone los endpoints requeridos por la consigna:

| Operación | Método HTTP | Ruta | Descripción | Código de Respuesta |
| :--- | :--- | :--- | :--- | :--- |
| **Listar todos** | `GET` | `/api/contacto` | Obtiene la lista completa de contactos desde la base de datos. | `200 OK` |
| **Obtener por ID** | `GET` | `/api/contacto/{id}` | Retorna el contacto correspondiente al ID solicitado. | `200 OK` / `404 Not Found` |
| **Agregar nuevo** | `POST` | `/api/contacto` | Inserta un nuevo contacto validando el modelo. | `201 CreatedAtAction` / `400 Bad Request` |
| **Editar contacto** | `PUT` | `/api/contacto/{id}` | Actualiza un contacto existente. | `200 OK` / `404 Not Found` / `400 Bad Request` |

*(Se mantiene compatibilidad de rutas con los alias `/add` y `/edit/{id}`).*

---

### 6. Autenticación y Autorización con JWT (`Controllers/AuthController.cs`)

En `Controllers/AuthController.cs`:

1. **DTOs Utilizados (`DTOs/`):**
   - `LoginRequest`: Contiene `Email` y `Password` con validaciones de atributos `[Required]` y `[EmailAddress]`.
   - `Register` recibe directamente el modelo `Usuario` (`Nombre`, `Email`, `Password`), como indica la consigna: `Register(Usuario usuario)`.

2. **Endpoints de Autenticación:**
   * **`POST /api/auth/register`**:
     * Valida que el correo no esté previamente registrado (`AnyAsync`), respondiendo `409 Conflict` en caso de duplicidad.
     * Hashea la contraseña mediante `PasswordHasher<Usuario>` de ASP.NET Core Identity.
     * Persiste el usuario en SQL Server y responde con código `201 Created`.
   * **`POST /api/auth/login`**:
     * Valida las credenciales recibidas contra la base de datos.
     * En caso de éxito, emite un token JWT firmado mediante algoritmo `HmacSha256`, incluyendo claims de identidad (`NameIdentifier`, `Email`, `Name`, `Jti`) y tiempo de expiración.

---

### 7. Ejecución y Pruebas con Swagger UI

1. Clonar el repositorio y acceder a la carpeta del proyecto:
   ```bash
   git clone https://github.com/lucainfodev/PAM3-ACT2.git
   cd PAM3-ACT2/GestionContactosAPI
   ```
2. Ejecutar la Web API:
   ```bash
   dotnet run --launch-profile "http"
   ```
3. Acceder a Swagger UI desde el navegador:
   ```text
   http://localhost:5197/swagger
   ```
4. **Flujo de prueba autenticado:**
   * Ejecutar `POST /api/auth/login` (o registrar un usuario nuevo en `POST /api/auth/register`).
   * Copiar el token JWT devuelto en la respuesta.
   * Hacer clic en el botón **Authorize** de Swagger, ingresar el token y presionar **Authorize**.
   * Consumir los endpoints de `/api/contacto` para verificar la lectura, inserción y edición de registros en SQL Server.
