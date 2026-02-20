using System.ComponentModel.DataAnnotations;

namespace SalaReuniones.Models
{
    public class Sala
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;

        public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
    }
}