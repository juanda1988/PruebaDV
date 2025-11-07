using System;
using System.ComponentModel.DataAnnotations;

namespace PruebaDV.Models
{
    public class Ticket
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Usuario { get; set; } = string.Empty;

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        public DateTime FechaActualizacion { get; set; } = DateTime.UtcNow;

        [Required]
        public string Estatus { get; set; } = string.Empty;
    }
}