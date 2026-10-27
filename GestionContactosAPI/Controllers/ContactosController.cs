using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GestionContactosAPI.Models;
using GestionContactosAPI.Services;

namespace GestionContactosAPI.Controllers;

[ApiController]
[Route("api/contacto")]
[Authorize]
public class ContactosController : ControllerBase
{
    private readonly ContactoService _contactoService;

    public ContactosController(ContactoService contactoService)
    {
        _contactoService = contactoService;
    }

    /// <summary>
    /// 1. Obtener la lista completa de contactos desde la base de datos.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Contacto>>> ObtenerTodos()
    {
        var contactos = await _contactoService.ObtenerTodosAsync();
        return Ok(contactos);
    }

    /// <summary>
    /// 2. Obtener un contacto por su Id (Clave Primaria).
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Contacto>> ObtenerPorId(int id)
    {
        var contacto = await _contactoService.ObtenerPorIdAsync(id);
        if (contacto == null)
        {
            return NotFound(new { mensaje = $"No se encontró el contacto con Id {id}" });
        }

        return Ok(contacto);
    }

    /// <summary>
    /// 3. Agregar un nuevo contacto a la base de datos.
    /// </summary>
    [HttpPost]
    [HttpPost("add")]
    public async Task<ActionResult<Contacto>> Agregar([FromBody] Contacto contacto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var nuevoContacto = await _contactoService.CrearAsync(contacto);
        return CreatedAtAction(nameof(ObtenerPorId), new { id = nuevoContacto.Id }, nuevoContacto);
    }

    /// <summary>
    /// 4. Editar un contacto existente por Id.
    /// </summary>
    [HttpPut("{id}")]
    [HttpPut("edit/{id}")]
    public async Task<ActionResult> Editar(int id, [FromBody] Contacto contacto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var editado = await _contactoService.EditarAsync(id, contacto);
        if (!editado)
        {
            return NotFound(new { mensaje = $"No se encontró el contacto con Id {id} para editar" });
        }

        return Ok(new { mensaje = "Contacto actualizado correctamente" });
    }
}
