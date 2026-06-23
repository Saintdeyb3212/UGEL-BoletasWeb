using System;
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

        [Required]
        [StringLength(255)]
        [Column(TypeName = "varchar(255)")]
        public string PasswordHash { get; private set; }

        [Required]
        [StringLength(20)]
        [Column(TypeName = "varchar(20)")]
        public string Rol { get; private set; }

        // 🚀 DATO FALTANTE CRÍTICO: Mapeo explícito del estado de la cuenta para auditorías
        [Required]
        public bool EstadoActivo { get; private set; } = true;

        protected UsuarioSistema() { }

        public UsuarioSistema(
            string username,
            string passwordHash,
            string rol,
            string usuarioCreacion)
        {
            if (string.IsNullOrWhiteSpace(username)) throw new ArgumentException("El nombre de usuario no puede estar vacío.");
            if (string.IsNullOrWhiteSpace(passwordHash)) throw new ArgumentException("El hash de la contraseña es obligatorio.");
            if (string.IsNullOrWhiteSpace(rol)) throw new ArgumentException("El rol del sistema es obligatorio.");
            if (string.IsNullOrWhiteSpace(usuarioCreacion)) throw new ArgumentException("El usuario creador es obligatorio.");

            Username = username.Trim();
            PasswordHash = passwordHash;
            Rol = rol.Trim();
            EstadoActivo = true; // Toda cuenta nueva nace activa

            InicializarAuditoria(usuarioCreacion.Trim());
        }

        // Comportamiento de dominio: Desactivación lógica (Borrado seguro)
        public void DesactivarCuenta(string usuarioQueModifica)
        {
            if (string.IsNullOrWhiteSpace(usuarioQueModifica)) throw new ArgumentException("El usuario modificador es obligatorio.");

            EstadoActivo = false;
            RegistrarModificacion(usuarioQueModifica.Trim());
        }
    }
}