using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.Models
{
    [Table("monsters")]
    public class Monster
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("type")]
        public string Type { get; set; } = string.Empty;

        [Column("behavior")]
        public string Behavior { get; set; } = string.Empty;
    }
}