using System.ComponentModel.DataAnnotations;

namespace SalaReuniones.Models
{
    public class Sala
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;

        // Nueva propiedad para color en el calendario
        [MaxLength(7)] // formato hexadecimal #RRGGBB
        public string? ColorHex { get; set; } = "#3788d8"; // azul por defecto

        public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
    }
}