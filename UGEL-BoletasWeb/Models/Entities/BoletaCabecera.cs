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

        // El .lis separa Apellidos y Nombres, lo respetaremos en la BD para ordenamiento alfabético
        [Required]
        [StringLength(100)]
        [Column(TypeName = "varchar(100)")]
        public string Apellidos { get; private set; }

        [Required]
        [StringLength(100)]
        [Column(TypeName = "varchar(100)")]
        public string Nombres { get; private set; }

        // Nuevos campos extraídos del .lis
        [StringLength(100)]
        [Column(TypeName = "varchar(100)")]
        public string Cargo { get; private set; } // Ej: PROF. DE AULA, PROF. POR HORA

        [StringLength(50)]
        [Column(TypeName = "varchar(50)")]
        public string TipoPensionista { get; private set; } // Ej: CESANTE, VIUDEZ

        [StringLength(100)]
        [Column(TypeName = "varchar(100)")]
        public string TipoPension { get; private set; } // Ej: CESANTE DOC. NIVELABLE

        [StringLength(50)]
        [Column(TypeName = "varchar(50)")]
        public string CuentaBancaria { get; private set; } // Ej: CTA- 4382300020

        [Required]
        [StringLength(2)]
        [Column(TypeName = "char(2)")]
        public string Mes { get; private set; }

        [Required]
        [StringLength(4)]
        [Column(TypeName = "char(4)")]
        public string Anio { get; private set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal TotalIngresos { get; private set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal TotalDescuentos { get; private set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal MontoLiquido { get; private set; }

        public virtual IReadOnlyCollection<BoletaDetalle> Detalles => _detalles.AsReadOnly();
        private readonly List<BoletaDetalle> _detalles = new List<BoletaDetalle>();

        protected BoletaCabecera() { }

        // Constructor actualizado
        public BoletaCabecera(string dni, string apellidos, string nombres, string cargo,
                              string tipoPensionista, string tipoPension, string cuentaBancaria,
                              string mes, string anio, decimal totalIngresos,
                              decimal totalDescuentos, decimal montoLiquido, string usuarioCreacion)
        {
            ArgumentException.ThrowIfNullOrEmpty(dni);
            ArgumentException.ThrowIfNullOrEmpty(apellidos);
            ArgumentException.ThrowIfNullOrEmpty(nombres);

            DNI = dni;
            Apellidos = apellidos;
            Nombres = nombres;
            Cargo = cargo ?? string.Empty;
            TipoPensionista = tipoPensionista ?? string.Empty;
            TipoPension = tipoPension ?? string.Empty;
            CuentaBancaria = cuentaBancaria ?? string.Empty;
            Mes = mes;
            Anio = anio;
            TotalIngresos = totalIngresos;
            TotalDescuentos = totalDescuentos;
            MontoLiquido = montoLiquido;

            InicializarAuditoria(usuarioCreacion);
        }

        public void AgregarDetalle(BoletaDetalle detalle)
        {
            if (detalle == null) throw new ArgumentNullException(nameof(detalle));
            _detalles.Add(detalle);
        }
    }
}