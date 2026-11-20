namespace Server.Models
{
    public class Weapon
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Effect {  get; set; } = string.Empty;
    }
}
