using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UGEL_BoletasWeb.Models.Entities
{
    [Table("UGEL_Boleta_Cabecera")]
    public class BoletaCabecera : AuditableEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }

        [Required]
        [StringLength(8)]
        [Column(TypeName = "varchar(8)")]
        public string DNI { get; private set; }

        [Required]
        [StringLength(100)]
        public string Apellidos { get; private set; }

        [Required]
        [StringLength(100)]
        public string Nombres { get; private set; }

        // --- 🚀 NUEVOS CAMPOS OFICIALES AÑADIDOS ---
        [StringLength(20)]
        public string FechaNacimiento { get; private set; }

        [StringLength(100)]
        public string Cargo { get; private set; }

        [StringLength(50)]
        public string TipoPensionista { get; private set; }

        [StringLength(100)]
        public string TipoPension { get; private set; }

        [StringLength(50)]
        public string NivelMagisterial { get; private set; }

        [StringLength(20)]
        public string TiempoServicio { get; private set; }

        [StringLength(50)]
        public string CodigoEsSalud { get; private set; }

        [StringLength(100)]
        public string FechasRegistro { get; private set; }

        [StringLength(50)]
        public string CuentaBancaria { get; private set; }

        [StringLength(255)]
        public string LeyendaPermanente { get; private set; }

        [StringLength(255)]
        public string LeyendaMensual { get; private set; }
        // ---------------------------------------------

        [Required]
        [StringLength(2)]
        [Column(TypeName = "char(2)")]
        public string Mes { get; private set; }

        [Required]
        [StringLength(4)]
        [Column(TypeName = "char(4)")]
        public string Anio { get; private set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal MontoImponible { get; private set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal TotalIngresos { get; private set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal TotalDescuentos { get; private set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal MontoLiquido { get; private set; }

        public virtual IReadOnlyCollection<BoletaDetalle> Detalles => _detalles.AsReadOnly();
        private readonly List<BoletaDetalle> _detalles = new List<BoletaDetalle>();

        protected BoletaCabecera() { }

        // Constructor actualizado con los nuevos campos
        public BoletaCabecera(string dni, string apellidos, string nombres, string fechaNacimiento, string cargo,
                              string tipoPensionista, string tipoPension, string nivelMagisterial, string tiempoServicio,
                              string codigoEsSalud, string fechasRegistro, string cuentaBancaria, string leyendaPermanente,
                              string leyendaMensual, string mes, string anio, decimal montoImponible,
                              decimal totalIngresos, decimal totalDescuentos, decimal montoLiquido, string usuarioCreacion)
        {
            DNI = dni;
            Apellidos = apellidos;
            Nombres = nombres;
            FechaNacimiento = fechaNacimiento ?? "";
            Cargo = cargo ?? "";
            TipoPensionista = tipoPensionista ?? "";
            TipoPension = tipoPension ?? "";
            NivelMagisterial = nivelMagisterial ?? "";
            TiempoServicio = tiempoServicio ?? "";
            CodigoEsSalud = codigoEsSalud ?? "";
            FechasRegistro = fechasRegistro ?? "";
            CuentaBancaria = cuentaBancaria ?? "";
            LeyendaPermanente = leyendaPermanente ?? "";
            LeyendaMensual = leyendaMensual ?? "";
            Mes = mes;
            Anio = anio;
            MontoImponible = montoImponible;
            TotalIngresos = totalIngresos;
            TotalDescuentos = totalDescuentos;
            MontoLiquido = montoLiquido;

            InicializarAuditoria(usuarioCreacion);
        }

        public void AgregarDetalle(BoletaDetalle detalle)
        {
            _detalles.Add(detalle);
        }
    }
}