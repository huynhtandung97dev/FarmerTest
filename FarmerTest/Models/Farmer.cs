using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmerTest.Models
{
    [Table("Farmer")]
    public class Farmer
    {
        [Key]
        [Column("FarmerID")]
        public int FarmerID { get; set; }

        [Required, StringLength(20)]
        public string FarmerCode { get; set; } = default!;

        [Required, StringLength(100)]
        public string FarmerName { get; set; } = default!;

        [StringLength(100)]
        public string? FarmerNameEN { get; set; }

        [StringLength(200)]
        public string? Address { get; set; }

        [StringLength(15)]
        public string? Phone1 { get; set; }

        [StringLength(15)]
        public string? Phone2 { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime CreatedAt { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime? UpdatedAt { get; set; }
    }
}