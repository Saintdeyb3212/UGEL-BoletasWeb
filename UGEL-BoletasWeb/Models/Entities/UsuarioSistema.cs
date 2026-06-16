using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UGEL_BoletasWeb.Models.Entities
{
    [Table("UGEL_Usuario_Sistema")]
    public class UsuarioSistema : AuditableEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }

        [Required]
        [StringLength(50)]
        [Column(TypeName = "varchar(50)")]
        public string Username { get; private set; }

        // ¡NUNCA EN TEXTO PLANO! Guardaremos el Hash en Base64
        [Required]
        [StringLength(255)]
        [Column(TypeName = "varchar(255)")]
        public string PasswordHash { get; private set; }

        [Required]
        [StringLength(20)]
        [Column(TypeName = "varchar(20)")]
        public string Rol { get; private set; } // Ej: "Admin", "Pagaduria"

        [Required]
        [StringLength(100)]
        [Column(TypeName = "varchar(100)")]
        public string NombreCompleto { get; private set; }

        protected UsuarioSistema() { }

        public UsuarioSistema(string username, string passwordHash, string rol, string nombreCompleto, string usuarioCreacion)
        {
            ArgumentException.ThrowIfNullOrEmpty(username);
            ArgumentException.ThrowIfNullOrEmpty(passwordHash);
            ArgumentException.ThrowIfNullOrEmpty(rol);

            Username = username;
            PasswordHash = passwordHash;
            Rol = rol;
            NombreCompleto = nombreCompleto ?? string.Empty;

            InicializarAuditoria(usuarioCreacion);
        }

        // Comportamiento del Dominio: Desactivar una cuenta sin borrarla
        public void DesactivarCuenta(string usuarioQueModifica)
        {
            // Validamos que el sistema siempre nos obligue a decir QUIÉN hace el cambio
            ArgumentException.ThrowIfNullOrEmpty(usuarioQueModifica, nameof(usuarioQueModifica));

            this.EstadoActivo = false;

            // Usamos el nuevo método de la clase base para sellar la auditoría
            this.RegistrarModificacion(usuarioQueModifica);
        }
    }
}