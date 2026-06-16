using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UGEL_BoletasWeb.Models.Entities
{
    [Table("UGEL_Boleta_Detalle")]
    public class BoletaDetalle : AuditableEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }

        // Llave foránea fuerte
        [Required]
        public int BoletaCabeceraId { get; private set; }

        // Propiedad de navegación (Entity Framework Core la usará para los JOINs)
        [ForeignKey(nameof(BoletaCabeceraId))]
        public virtual BoletaCabecera BoletaCabecera { get; private set; }

        [Required]
        [StringLength(10)]
        [Column(TypeName = "varchar(10)")] // Ej: "0100", "0255"
        public string CodigoConcepto { get; private set; }

        [Required]
        [StringLength(100)]
        [Column(TypeName = "varchar(100)")] // Ej: "REMUNERACION BASICA", "QUINTA CATEGORIA"
        public string Descripcion { get; private set; }

        [Required]
        [StringLength(1)]
        [Column(TypeName = "char(1)")]
        [RegularExpression("^[ID]$", ErrorMessage = "El tipo solo puede ser 'I' (Ingreso) o 'D' (Descuento).")]
        public string TipoConcepto { get; private set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")] // Manejo estricto de dinero
        public decimal Monto { get; private set; }

        // Constructor vacío para EF Core
        protected BoletaDetalle() { }

        // Constructor de Dominio
        public BoletaDetalle(string codigoConcepto, string descripcion, string tipoConcepto, decimal monto, string usuarioCreacion)
        {
            ArgumentException.ThrowIfNullOrEmpty(codigoConcepto);
            ArgumentException.ThrowIfNullOrEmpty(descripcion);

            if (tipoConcepto != "I" && tipoConcepto != "D")
                throw new ArgumentException("El tipo de concepto es inválido.");

            if (monto < 0)
                throw new ArgumentException("El monto no puede ser negativo en la lectura bruta del LIS.");

            CodigoConcepto = codigoConcepto;
            Descripcion = descripcion;
            TipoConcepto = tipoConcepto;
            Monto = monto;

            InicializarAuditoria(usuarioCreacion);
        }
    }
}