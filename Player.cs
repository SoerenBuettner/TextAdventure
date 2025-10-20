namespace TextAbenteuer.Models
{
    /// <summary>
    /// Spieler + Position/HP/Inventar.
    /// </summary>
    public record struct Position(int X, int Y);

    public class Player
    {
        public string Name { get; init; }
        public Position Position { get; set; }
        public int Hp { get; set; }
        public List<string> Inventory { get; } = new();

        public Player(string name, Position start, int hp = 10)
        {
            Name = name;
            Position = start;
            Hp = hp;
        }
    }
}
