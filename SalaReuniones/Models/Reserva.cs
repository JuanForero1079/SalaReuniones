// Importa funcionalidades básicas del sistema (tipos como DateTime, etc.)
using System;

// Permite usar anotaciones como [Required] para validar campos
using System.ComponentModel.DataAnnotations;

// Define el espacio de nombres (organización del proyecto)
// Es como agrupar archivos por módulo
namespace SalaReuniones.Models
{
    // Clase que representa una reserva en el sistema
    // Esta clase será equivalente a una tabla en la base de datos
    public class Reserva
    {
        // Clave primaria de la tabla
        // EF Core la detecta automáticamente como ID
        public int Id { get; set; }

        // Indica que este campo es obligatorio
        // No se puede guardar vacío
        [Required]
        public string Nombre { get; set; }

        // Fecha de la reserva (solo fecha, sin hora)
        // Es obligatoria
        [Required]
        public DateTime Fecha { get; set; }

        // Hora de inicio de la reunión
        // Tipo TimeSpan representa una hora sin fecha
        [Required]
        public TimeSpan HoraInicio { get; set; }

        // Hora de finalización de la reunión
        // También obligatoria
        [Required]
        public TimeSpan HoraFin { get; set; }

        // Motivo de la reunión (opcional)
        // Puede quedar vacío
        public string Motivo { get; set; }

        // Estado de la reserva
        // Por defecto se crea como "Activa"
        // Si no se asigna nada, automáticamente tendrá ese valor
        public string Estado { get; set; } = "Activa";
    }
}
