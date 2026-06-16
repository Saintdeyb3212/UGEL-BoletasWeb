using System.ComponentModel.DataAnnotations;

namespace UGEL_BoletasWeb.Models.Entities
{
    public abstract class AuditableEntity
    {
        [Required]
        public DateTime FechaCreacion { get; protected set; }

        [StringLength(50)]
        public string? UsuarioCreacion { get; protected set; }

        // --- NUEVOS CAMPOS DE AUDITORÍA ---
        public DateTime? FechaModificacion { get; protected set; }

        [StringLength(50)]
        public string? UsuarioModificacion { get; protected set; }

        public bool EstadoActivo { get; protected set; }

        protected void InicializarAuditoria(string usuario)
        {
            FechaCreacion = DateTime.UtcNow;
            UsuarioCreacion = usuario;
            EstadoActivo = true;
        }

        // --- NUEVO COMPORTAMIENTO ---
        protected void RegistrarModificacion(string usuario)
        {
            FechaModificacion = DateTime.UtcNow;
            UsuarioModificacion = usuario;
        }
    }
}