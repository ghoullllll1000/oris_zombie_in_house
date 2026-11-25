using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.Models
{
    [Table("weapons")]
    public class Weapon
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("category")]
        public string Category { get; set; } = string.Empty;

        [Column("effect")]
        public string Effect { get; set; } = string.Empty;
    }
}