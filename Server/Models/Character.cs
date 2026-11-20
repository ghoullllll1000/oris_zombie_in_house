namespace Server.Models
{
    public class Character
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Health { get; set; }
        public string Feature { get; set; } = string.Empty;
    }
}
