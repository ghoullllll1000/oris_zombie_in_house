using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.Models
{
    [Table("monster_drops")]
    public class MonsterDrop
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("monster_id")]
        public int MonsterId { get; set; }

        [Required]
        [Column("item_id")]
        public int ItemId { get; set; }
    }
}