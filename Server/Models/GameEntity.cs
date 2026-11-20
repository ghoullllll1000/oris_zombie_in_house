namespace Server.Models
{
    public class GameEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public EntityType Type { get; set; }
        public string Features { get; set; } = string.Empty;
    }
}
