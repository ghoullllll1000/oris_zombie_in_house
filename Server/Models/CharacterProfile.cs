using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.Models
{
    [Table("character_profiles")]
    public class CharacterProfile
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("character_id")]
        public int CharacterId { get; set; }

        [Required]
        [Column("biography")]
        public string Biography { get; set; } = string.Empty;
    }
}