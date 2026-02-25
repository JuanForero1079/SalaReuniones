using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SalaReuniones.Models
{
    /// <summary>
    /// Representa una reserva en el sistema.
    /// Esta clase se convierte en una tabla en la base de datos.
    /// </summary>
    public class Reserva
    {
        // ==============================
        // CLAVE PRIMARIA
        // ==============================
        public int Id { get; set; }

        // ==============================
        // FECHA Y HORAS
        // ==============================

        // 🔹 Fecha obligatoria
        [Required(ErrorMessage = "La fecha es obligatoria.")]
        [DataType(DataType.Date)]
        public DateTime Fecha { get; set; }

        // 🔹 Hora de inicio obligatoria
        [Required(ErrorMessage = "La hora de inicio es obligatoria.")]
        [DataType(DataType.Time)]
        public TimeSpan HoraInicio { get; set; }

        // 🔹 Hora de fin obligatoria
        [Required(ErrorMessage = "La hora de fin es obligatoria.")]
        [DataType(DataType.Time)]
        public TimeSpan HoraFin { get; set; }

        // ==============================
        // INFORMACIÓN ADICIONAL
        // ==============================

        // 🔹 Motivo opcional (máximo 250 caracteres)
        [MaxLength(250)]
        public string Motivo { get; set; } = string.Empty;

        /*
            ❗ IMPORTANTE:
            Quitamos [Required] porque el Estado se asigna automáticamente
            en el controlador.
        */
        public EstadoReserva Estado { get; set; } = EstadoReserva.Activa;

        // ==============================
        // RELACIÓN CON SALA
        // ==============================

        // 🔹 La sala sí es obligatoria (se selecciona en el formulario)
        [Required(ErrorMessage = "Debe seleccionar una sala.")]
        public int SalaId { get; set; }

        public Sala? Sala { get; set; }

        // ==============================
        // RELACIÓN CON USUARIO (Identity)
        // ==============================

        /*
            ❗ MUY IMPORTANTE:

            Quitamos [Required] porque UsuarioId
            NO viene del formulario.

            Se asigna automáticamente en el controlador:
            reserva.UsuarioId = userId;

            Si dejamos [Required], el ModelState falla.
        */
        public string? UsuarioId { get; set; }

        [ForeignKey(nameof(UsuarioId))]
        public IdentityUser? Usuario { get; set; }

        // ==============================
        // VALIDACIÓN PERSONALIZADA
        // ==============================

        /// <summary>
        /// Verifica que la hora de fin sea mayor que la hora de inicio.
        /// </summary>
        public bool HorarioValido()
        {
            return HoraFin > HoraInicio;
        }
    }

    // ==================================
    // ENUM PARA ESTADO DE LA RESERVA
    // ==================================
    public enum EstadoReserva
    {
        Activa,
        Cancelada,
        Finalizada
    }
}