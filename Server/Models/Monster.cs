namespace Server.Models
{
    public class Monster
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Behavior {  get; set; } = string.Empty;
    }
}
