// Importa funcionalidades básicas del sistema (DateTime, etc.)
using System;

// Permite usar anotaciones como [Required], [MaxLength], etc.
using System.ComponentModel.DataAnnotations;

// Permite definir claves foráneas
using System.ComponentModel.DataAnnotations.Schema;

namespace SalaReuniones.Models
{
    /// <summary>
    /// Representa una reserva en el sistema.
    /// Esta clase se convierte en una tabla en la base de datos.
    /// </summary>
    public class Reserva
    {
        // Clave primaria de la tabla
        public int Id { get; set; }

        // Nombre de la persona que realiza la reserva
        [Required]
        [MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;

        // Fecha de la reserva
        [Required]
        public DateTime Fecha { get; set; }

        // Hora de inicio
        [Required]
        public TimeSpan HoraInicio { get; set; }

        // Hora de finalización
        [Required]
        public TimeSpan HoraFin { get; set; }

        // Motivo de la reunión
        [MaxLength(250)]
        public string Motivo { get; set; } = string.Empty;

        // Estado de la reserva
        // Por defecto será "Activa"
        [Required]
        [MaxLength(50)]
        public string Estado { get; set; } = "Activa";

        // 🔹 CLAVE FORÁNEA
        // Indica a qué sala pertenece esta reserva
        [ForeignKey(nameof(Sala))]
        public int SalaId { get; set; }

        // 🔹 Propiedad de navegación
        // Permite acceder a los datos de la sala relacionada
        public Sala? Sala { get; set; } = null!;

    }
}