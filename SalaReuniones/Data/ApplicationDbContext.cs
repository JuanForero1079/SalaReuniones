// Importa Entity Framework Core para poder trabajar con bases de datos
using Microsoft.EntityFrameworkCore;

// Importa los modelos del proyecto (como la clase Reserva)
using SalaReuniones.Models;

namespace SalaReuniones.Data
{
    /// <summary>
    /// DbContext principal de la aplicación.
    /// Se encarga de gestionar la conexión con la base de datos
    /// y mapear las clases del modelo a tablas en SQL Server.
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        /// <summary>
        /// Constructor que recibe las opciones de configuración
        /// (cadena de conexión, tipo de base de datos, etc.)
        /// y las pasa a la clase base DbContext.
        /// </summary>
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// Representa la tabla "Reservas" en la base de datos.
        /// Permite realizar operaciones CRUD sobre las reservas.
        /// </summary>
        public DbSet<Reserva> Reservas { get; set; }
    }
}
