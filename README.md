# Actividad Obligatoria Integradora Nº 2 - Programación de Aplicaciones Móviles III
## TUDAI / IUA - Tecnicatura Universitaria en Desarrollo de Aplicaciones Informáticas

---

### Trabajo Grupal: Persistencia con EF Core y Autenticación JWT
**Repositorio Oficial:** [https://github.com/lucainfodev/PAM3-ACT2](https://github.com/lucainfodev/PAM3-ACT2)  

---

## 📌 Guía Paso a Paso y Material de Defensa

Este documento contiene la explicación exhaustiva de la arquitectura, configuración y desarrollo implementado para la **Actividad Obligatoria 2**, cubriendo tanto la persistencia con **Entity Framework Core (Database First)** como la **Autenticación con JWT**.

---

### 1. Instalación de Herramientas y Paquetes NuGet de EF Core

#### Herramienta Global CLI:
Para realizar operaciones de ingeniería inversa (Scaffolding) y gestión de Entity Framework Core desde la consola, se instaló la herramienta global `dotnet-ef`:
```bash
dotnet tool install --global dotnet-ef
```

#### Paquetes NuGet Requeridos:
En el archivo `GestionContactosAPI.csproj` se incorporaron los paquetes oficiales de Entity Framework Core y autenticación:
1. **`Microsoft.EntityFrameworkCore.SqlServer`**: El proveedor de base de datos específico para Microsoft SQL Server. Se encarga de traducir las consultas LINQ a sentencias T-SQL nativas y comunicarse con el motor relacional.
2. **`Microsoft.EntityFrameworkCore.Design`**: Provee los componentes de diseño necesarios para ejecutar comandos de scaffolding, migraciones y generación de código en tiempo de desarrollo.
3. **`Microsoft.AspNetCore.Authentication.JwtBearer`**: Middleware oficial para validar tokens JWT en cada petición HTTP autenticada.

Comandos ejecutados:
```bash
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
```

---

### 2. Scaffold y Modelado de Datos (Contacto, Usuario y DbContext)

#### Comando de Scaffolding:
El comando para realizar ingeniería inversa sobre la base de datos SQL Server remota es:
```bash
dotnet ef dbcontext scaffold "Server=SQL8020.site4now.net;Database=db_a358b2_pam3;User Id=db_a358b2_pam3_admin;Password=TudaiPam32548-;TrustServerCertificate=True;" Microsoft.EntityFrameworkCore.SqlServer -o Models -f
```
* **`-o Models`**: Especifica que las entidades generadas y el contexto se ubiquen en el directorio `Models`.
* **`-f` (force)**: Fuerza la sobrescritura de archivos existentes.
* **`TrustServerCertificate=True`**: Permite la conexión segura TLS/SSL con el servidor remoto de hosting.

#### Modelos de Dominio Creados:

##### `Models/Contacto.cs`
Representa la tabla de contactos en la base de datos:
- `Id` (`int`, Clave primaria auto-incremental / Identity).
- `Nombre` (`string`, obligatorio, longitud máxima 100).
- `Apellido` (`string`, obligatorio, longitud máxima 100).
- `Telefono` (`string`, opcional, longitud máxima 50).
- `Email` (`string`, opcional, validado con formato de correo, longitud máxima 150).

##### `Models/Usuario.cs`
Representa la tabla de usuarios del sistema (utilizada por el módulo de autenticación JWT):
- `Id` (`int`, Clave primaria auto-incremental).
- `Nombre` (`string`, opcional).
- `Email` (`string`, obligatorio, formato email).
- `Password` (`string`, obligatorio, almacena la credencial de acceso).

##### `Models/DbA358b2Pam3Context.cs` (DbContext)
Es la clase central que hereda de `DbContext`. Representa la sesión con la base de datos y expone los `DbSet`:
- `public virtual DbSet<Contacto> Contactos { get; set; }`
- `public virtual DbSet<Usuario> Usuarios { get; set; }`
- Define mediante **Fluent API** en el método `OnModelCreating` el mapeo exacto de tablas (`ToTable`), claves primarias (`HasKey`), propiedades obligatorias (`IsRequired`), restricciones de tamaño (`HasMaxLength`) y generación de identificadores (`ValueGeneratedOnAdd`).

---

### 3. Configuración del DbContext y Cadena de Conexión

#### Cadena de Conexión en `appsettings.json`:
Por buenas prácticas de desarrollo y seguridad (evitando *hardcodear* credenciales en el código fuente), la conexión se centraliza en `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=SQL8020.site4now.net;Database=db_a358b2_pam3;User Id=db_a358b2_pam3_admin;Password=TudaiPam32548-;TrustServerCertificate=True;"
  }
}
```

#### Registro del Servicio en `Program.cs`:
Se vincula el DbContext al contenedor de inyección de dependencias de ASP.NET Core:
```csharp
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<DbA358b2Pam3Context>(options =>
    options.UseSqlServer(connectionString));
```

#### ⚠️ Concepto Crítico de Examen - Ciclo de Vida Scoped vs Singleton:
En la Actividad 1, `ContactoService` estaba registrado como **Singleton**:
```csharp
// ACT 1 (En memoria):
builder.Services.AddSingleton<ContactoService>();
```
En la Actividad 2, al utilizar Entity Framework Core, **DEBE registrarse como Scoped**:
```csharp
// ACT 2 (Con EF Core):
builder.Services.AddScoped<ContactoService>();
```
**¿Por qué?**
El `DbContext` de EF Core tiene por diseño un ciclo de vida **Scoped** (una instancia por cada petición HTTP). Si intentáramos registrar un servicio que consume `DbContext` como Singleton, ASP.NET Core lanzará una excepción en tiempo de arranque (*Captive Dependency*): un objeto Singleton viviría para siempre y mantendría un `DbContext` abierto indefinidamente, provocando problemas graves de concurrencia, fugas de memoria y bloqueos de hilos al acceder múltiples usuarios simultáneamente.

---

### 4. Reemplazo del Servicio en Memoria por EF Core

En `Services/ContactoService.cs`:
- Se eliminó la lista en memoria `List<Contacto> _contactos`.
- Se inyectó el contexto de base de datos `DbA358b2Pam3Context` vía constructor.
- Se implementaron métodos completamente **asíncronos** (`async`/`await`), optimizando el uso del Thread Pool del servidor web.
- Se utilizó `.AsNoTracking()` en las consultas de lectura (`ObtenerTodosAsync`), lo que desactiva el seguimiento de cambios en memoria de EF Core, reduciendo drásticamente el consumo de CPU y memoria RAM.

Métodos principales:
1. `ObtenerTodosAsync()`: `await _context.Contactos.AsNoTracking().ToListAsync();`
2. `ObtenerPorIdAsync(int id)`: `await _context.Contactos.FindAsync(id);`
3. `CrearAsync(Contacto contacto)`: `_context.Contactos.Add(contacto); await _context.SaveChangesAsync(); return contacto;`
4. `EditarAsync(int id, Contacto contacto)`: recupera la entidad existente, actualiza sus campos y persiste con `await _context.SaveChangesAsync();`
5. `EliminarAsync(int id)`: busca la entidad, la remueve y persiste los cambios.

---

### 5. ContactosController con los 4 Endpoints Requeridos

En `Controllers/ContactosController.cs`:
El controlador está protegido con `[Authorize]` y expone los 4 endpoints solicitados:

| Operación | Método HTTP | Ruta | Descripción | Código de Respuesta |
| :--- | :--- | :--- | :--- | :--- |
| **Listar todos** | `GET` | `/api/contacto` | Retorna el listado completo de contactos desde SQL Server. | `200 OK` |
| **Obtener por ID** | `GET` | `/api/contacto/{id}` | Busca un contacto por su clave primaria. | `200 OK` / `404 Not Found` |
| **Agregar nuevo** | `POST` | `/api/contacto` | Crea un nuevo contacto y lo persiste en la BD. | `201 CreatedAtAction` / `400 Bad Request` |
| **Editar contacto** | `PUT` | `/api/contacto/{id}` | Modifica los datos de un contacto existente. | `200 OK` / `404 Not Found` / `400 Bad Request` |

*(Se mantuvieron además las rutas secundarias `add` y `edit/{id}` para garantizar 100% de compatibilidad con versiones previas).*

---

### 6. Autenticación de Usuarios con JWT (Register y Login)

En `Controllers/AuthController.cs`:

1. **DTOs Utilizados (`DTOs/`):**
   - `LoginRequest`: Contiene `Email` y `Password` con validaciones de atributos `[Required]` y `[EmailAddress]`.
   - `RegisterRequest`: Contiene `Nombre`, `Email` y `Password` (con requerimiento de longitud mínima).

2. **Endpoints de Autenticación:**
   - `POST /api/auth/register`:
     - Normaliza el correo electrónico (`Trim().ToLowerInvariant()`).
     - Valida en base de datos si el email ya se encuentra en uso (`AnyAsync`), respondiendo `409 Conflict` en caso duplicado.
     - Aplica un algoritmo seguro de derivación de claves mediante `PasswordHasher<Usuario>` de ASP.NET Core Identity.
     - Persiste el nuevo usuario y responde con `201 Created`.
   - `POST /api/auth/login`:
     - Valida la existencia del usuario en la base de datos por email.
     - Verifica la clave con `PasswordHasher` (con fallback de texto plano para compatibilidad con usuarios precargados de prueba).
     - Genera un token JWT firmado con la clave simétrica (`HmacSha256`) que incluye claims de `NameIdentifier`, `Email`, `Name` y `Jti`.

---

## 🎓 Preguntas Frecuentes para la Defensa Oral

### 1. ¿Qué es el enfoque Database First y en qué se diferencia de Code First?
- **Database First**: Se parte de una base de datos ya existente (creada con scripts o en el gestor SQL). A través de herramientas como `dotnet ef dbcontext scaffold`, se realiza ingeniería inversa para generar automáticamente las clases de entidad en C# y el `DbContext`.
- **Code First**: Se escriben primero las clases de C# y mediante el sistema de migraciones (`dotnet ef migrations add`), EF Core se encarga de crear o actualizar las tablas en el motor de base de datos.

### 2. ¿Qué función cumple el `DbContext`?
El `DbContext` es el corazón de Entity Framework Core. Actúa como una combinación de los patrones de diseño **Unit of Work** (Unidad de Trabajo) y **Repository** (Repositorio). Administra la conexión a la base de datos, mapea los objetos en memoria a las tablas relacionales, rastrea los cambios que sufren las entidades (*Change Tracker*) y ejecuta las transacciones contra la base de datos mediante `SaveChangesAsync()`.

### 3. ¿Por qué se utiliza `AsNoTracking()` en las consultas de solo lectura?
Por defecto, cuando EF Core consulta entidades, las almacena en su memoria interna (*Change Tracker*) para vigilar si alguna propiedad es modificada. En operaciones de sólo lectura (como listar contactos para enviarlos al cliente JSON), no se necesita modificar esos datos. Usar `.AsNoTracking()` le indica a EF Core que descarte el seguimiento, ahorrando memoria y acelerando la respuesta de la API.

### 4. ¿Por qué usamos `SaveChangesAsync()` en lugar de `SaveChanges()`?
Porque `SaveChangesAsync()` es no bloqueante. En un servidor web como Kestrel en ASP.NET Core, cuando se ejecuta una consulta I/O contra la base de datos externa, la ejecución asíncrona libera el hilo de ejecución para que pueda atender otras solicitudes HTTP entrantes mientras la base de datos procesa la consulta. Cuando la BD responde, el hilo retoma la ejecución. Esto aumenta la concurrencia y escalabilidad del sistema.

### 5. ¿Qué significa el código de estado `201 CreatedAtAction` en el POST?
Es la buena práctica según la especificación REST:
1. Indica que el recurso fue creado exitosamente en el servidor (`201 Created`).
2. Devuelve en el encabezado `Location` la URL exacta donde se puede consultar el recurso recién creado (usando la ruta del método `ObtenerPorId`).
3. Devuelve en el cuerpo de la respuesta el objeto recién creado incluyendo el `Id` asignado por la base de datos.
