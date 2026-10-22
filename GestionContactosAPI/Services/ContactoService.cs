using Microsoft.EntityFrameworkCore;
using GestionContactosAPI.Models;

namespace GestionContactosAPI.Services;

public class ContactoService
{
    private readonly DbA358b2Pam3Context _context;

    public ContactoService(DbA358b2Pam3Context context)
    {
        _context = context;
    }

    /// <summary>
    /// Obtiene la lista completa de contactos desde la base de datos con Entity Framework Core.
    /// </summary>
    public async Task<List<Contacto>> ObtenerTodosAsync()
    {
        return await _context.Contactos
            .AsNoTracking()
            .ToListAsync();
    }

    /// <summary>
    /// Busca un contacto por su Id (Clave Primaria).
    /// </summary>
    public async Task<Contacto?> ObtenerPorIdAsync(int id)
    {
        return await _context.Contactos.FindAsync(id);
    }

    /// <summary>
    /// Agrega un nuevo contacto a la base de datos y guarda los cambios.
    /// </summary>
    public async Task<Contacto> CrearAsync(Contacto contacto)
    {
        _context.Contactos.Add(contacto);
        await _context.SaveChangesAsync();
        return contacto;
    }

    /// <summary>
    /// Modifica los datos de un contacto existente en la base de datos.
    /// </summary>
    public async Task<bool> EditarAsync(int id, Contacto contacto)
    {
        var existente = await _context.Contactos.FindAsync(id);
        if (existente == null)
        {
            return false;
        }

        existente.Nombre = contacto.Nombre;
        existente.Apellido = contacto.Apellido;
        existente.Telefono = contacto.Telefono;
        existente.Email = contacto.Email;

        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Elimina un contacto por su Id.
    /// </summary>
    public async Task<bool> EliminarAsync(int id)
    {
        var existente = await _context.Contactos.FindAsync(id);
        if (existente == null)
        {
            return false;
        }

        _context.Contactos.Remove(existente);
        await _context.SaveChangesAsync();
        return true;
    }
}
