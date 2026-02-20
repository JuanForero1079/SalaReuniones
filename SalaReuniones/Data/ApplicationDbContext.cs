// Importa Identity con Entity Framework
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

// Importa Entity Framework Core
using Microsoft.EntityFrameworkCore;

// Importa los modelos del proyecto
using SalaReuniones.Models;

namespace SalaReuniones.Data
{
    /// <summary>
    /// Contexto principal de la aplicación.
    /// Hereda de IdentityDbContext<IdentityUser> para soportar:
    /// - Usuarios
    /// - Roles
    /// - Claims
    /// - Logins
    /// Además mantiene nuestras tablas personalizadas.
    /// </summary>
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        // Constructor que recibe las opciones de configuración
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // =========================
        // TABLAS PERSONALIZADAS
        // =========================

        // Tabla de Reservas
        public DbSet<Reserva> Reservas { get; set; } = null!;

        // Tabla de Salas
        public DbSet<Sala> Salas { get; set; } = null!;

        /// <summary>
        /// Método que permite configurar el modelo y agregar datos iniciales (Seed Data)
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // IMPORTANTE: siempre llamar primero a base para Identity
            base.OnModelCreating(modelBuilder);

            // =========================
            // SEED DATA - SALAS
            // =========================
            modelBuilder.Entity<Sala>().HasData(
                new Sala { Id = 1, Nombre = "Sala Principal" },
                new Sala { Id = 2, Nombre = "Sala Secundaria" }
            );

            // =========================
            // SEED DATA - ROLES (GUID para evitar conflictos)
            // =========================
            modelBuilder.Entity<IdentityRole>().HasData(
                new IdentityRole { Id = "a1b2c3d4-e5f6-4711-aaaa-bbbbcccc0001", Name = "Administrador", NormalizedName = "ADMINISTRADOR" },
                new IdentityRole { Id = "a1b2c3d4-e5f6-4711-aaaa-bbbbcccc0002", Name = "Usuario", NormalizedName = "USUARIO" },
                new IdentityRole { Id = "a1b2c3d4-e5f6-4711-aaaa-bbbbcccc0003", Name = "Visualizador", NormalizedName = "VISUALIZADOR" }
            );
        }
    }
}