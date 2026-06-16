using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UGEL_BoletasWeb.Models.Entities
{
    [Table("UGEL_Boleta_Cabecera")] // Nombre formal en SQL Server
    public class BoletaCabecera : AuditableEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }

        [Required(ErrorMessage = "El DNI es un campo obligatorio.")]
        [StringLength(8, MinimumLength = 8, ErrorMessage = "El DNI debe tener exactamente 8 caracteres.")]
        [RegularExpression("^[0-9]*$", ErrorMessage = "El DNI solo debe contener números.")]
        [Column(TypeName = "varchar(8)")] // Evita que SQL reserve memoria innecesaria (NVARCHAR)
        public string DNI { get; private set; }

        [Required]
        [StringLength(150)]
        [Column(TypeName = "varchar(150)")]
        public string NombresApellidos { get; private set; }

        [Required]
        [StringLength(15)]
        [Column(TypeName = "varchar(15)")]
        public string CodigoModular { get; private set; }

        [Required]
        [StringLength(2)]
        [Column(TypeName = "char(2)")] // Mes exacto: "01", "12"
        public string Mes { get; private set; }

        [Required]
        [StringLength(4)]
        [Column(TypeName = "char(4)")] // Año exacto: "2026"
        public string Anio { get; private set; }

        // Protección financiera: Tipos exactos para moneda, evitando redondeos flotantes
        [Column(TypeName = "decimal(12,2)")]
        public decimal TotalIngresos { get; private set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal TotalDescuentos { get; private set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal MontoLiquido { get; private set; }

        // Relación fuertemente tipada y de solo lectura para evitar reasignaciones en memoria
        public virtual IReadOnlyCollection<BoletaDetalle> Detalles => _detalles.AsReadOnly();
        private readonly List<BoletaDetalle> _detalles = new List<BoletaDetalle>();

        // Constructor vacío requerido por Entity Framework Core (Protected para que no se instancie sin datos)
        protected BoletaCabecera() { }

        // Constructor de Dominio (Garantiza que el objeto nazca en un estado válido y no a medias)
        public BoletaCabecera(string dni, string nombresApellidos, string codigoModular,
                              string mes, string anio, decimal totalIngresos,
                              decimal totalDescuentos, decimal montoLiquido, string usuarioCreacion)
        {
            // Fail-Fast: Validación por código (C# 10+)
            ArgumentException.ThrowIfNullOrEmpty(dni);
            ArgumentException.ThrowIfNullOrEmpty(nombresApellidos);

            DNI = dni;
            NombresApellidos = nombresApellidos;
            CodigoModular = codigoModular;
            Mes = mes;
            Anio = anio;
            TotalIngresos = totalIngresos;
            TotalDescuentos = totalDescuentos;
            MontoLiquido = montoLiquido;

            InicializarAuditoria(usuarioCreacion); // Heredado
        }

        // Método de comportamiento (Cohesión Alta: La lógica de agregar detalles pertenece a la cabecera)
        public void AgregarDetalle(BoletaDetalle detalle)
        {
            if (detalle == null) throw new ArgumentNullException(nameof(detalle));
            _detalles.Add(detalle);
        }
    }
}