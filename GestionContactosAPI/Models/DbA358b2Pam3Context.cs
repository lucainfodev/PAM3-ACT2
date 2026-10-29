using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace GestionContactosAPI.Models;

public partial class DbA358b2Pam3Context : DbContext
{
    public DbA358b2Pam3Context()
    {
    }

    public DbA358b2Pam3Context(DbContextOptions<DbA358b2Pam3Context> options)
        : base(options)
    {
    }

    public virtual DbSet<Contacto> Contactos { get; set; } = null!;

    public virtual DbSet<Usuario> Usuarios { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Contacto>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.ToTable("Contacto");

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.Nombre)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Apellido)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Telefono)
                .HasMaxLength(50);

            entity.Property(e => e.Email)
                .HasMaxLength(150);
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.ToTable("Usuario");

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.Nombre)
                .HasMaxLength(100);

            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(e => e.Password)
                .IsRequired()
                .HasMaxLength(255);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
