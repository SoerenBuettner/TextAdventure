using System.Text;

namespace TextAbenteuer.Models
{
    /// <summary>
    /// Weltkarte als Dungeon (Räume & Korridore) mit Fog-of-War.
    /// Tiles: '.' Boden, '#' Wand, 'R' Runenstein, 'B' Boss.
    /// </summary>
    public class World
    {
        public int Width { get; }
        public int Height { get; }
        public int Seed { get; }
        public Position Start { get; private set; }
        public Position Boss { get; private set; }

        private readonly bool[,] _explored;
        private readonly char[,] _tiles;

        public World(int width, int height, int seed, Random rng, int minRunen, int maxRunen)
        {
            Width = width;
            Height = height;
            Seed = seed;
            _explored = new bool[width, height];
            _tiles = new char[width, height];

            // Alles zunächst Wand
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    _tiles[x, y] = '#';

            // Einfache Rooms-&-Corridors-Generierung
            int rooms = Math.Max(4, (width * height) / 32);
            var centers = new List<Position>();

            for (int i = 0; i < rooms; i++)
            {
                int rw = rng.Next(3, Math.Max(4, width / 4));
                int rh = rng.Next(3, Math.Max(4, height / 4));
                int rx = rng.Next(1, Math.Max(2, width - rw - 1));
                int ry = rng.Next(1, Math.Max(2, height - rh - 1));

                for (int y = ry; y < ry + rh; y++)
                    for (int x = rx; x < rx + rw; x++)
                        _tiles[x, y] = '.';

                centers.Add(new Position(rx + rw / 2, ry + rh / 2));
            }

            // Räume verbinden (L-förmig)
            centers.Sort((a, b) => (a.X + a.Y).CompareTo(b.X + b.Y));
            for (int i = 1; i < centers.Count; i++) CarveCorridor(centers[i - 1], centers[i]);

            // Start & Boss
            Start = centers.Count > 0 ? centers[0] : new Position(1, 1);
            _tiles[Start.X, Start.Y] = '.';

            Boss = FarthestWalkableFrom(Start);
            _tiles[Boss.X, Boss.Y] = 'B';

            // Runensteine verteilen
            int runen = new Random(Seed).Next(minRunen, maxRunen + 1); // deterministisch aus Seed
            PlaceRunestones(new Random(Seed + 1), runen, new HashSet<(int, int)> { (Start.X, Start.Y), (Boss.X, Boss.Y) });
        }

        private void CarveCorridor(Position a, Position b)
        {
            int x = a.X, y = a.Y;
            while (x != b.X) { _tiles[x, y] = '.'; x += (b.X > x) ? 1 : -1; }
            while (y != b.Y) { _tiles[x, y] = '.'; y += (b.Y > y) ? 1 : -1; }
            _tiles[b.X, b.Y] = '.';
        }

        private Position FarthestWalkableFrom(Position start)
        {
            var q = new Queue<Position>();
            var dist = new Dictionary<(int, int), int>();
            q.Enqueue(start);
            dist[(start.X, start.Y)] = 0;
            Position far = start;
            int[] dx = { 1, -1, 0, 0 };
            int[] dy = { 0, 0, 1, -1 };

            while (q.Count > 0)
            {
                var p = q.Dequeue();
                int d = dist[(p.X, p.Y)];
                if (d > dist[(far.X, far.Y)]) far = p;
                for (int i = 0; i < 4; i++)
                {
                    int nx = p.X + dx[i], ny = p.Y + dy[i];
                    if (nx < 0 || ny < 0 || nx >= Width || ny >= Height) continue;
                    if (_tiles[nx, ny] == '#') continue;
                    var key = (nx, ny);
                    if (!dist.ContainsKey(key)) { dist[key] = d + 1; q.Enqueue(new Position(nx, ny)); }
                }
            }
            return far;
        }

        private void PlaceRunestones(Random rng, int count, HashSet<(int, int)> forbidden)
        {
            int placed = 0, attempts = 0;
            while (placed < count && attempts < Width * Height * 2)
            {
                attempts++;
                int x = rng.Next(0, Width), y = rng.Next(0, Height);
                if (_tiles[x, y] != '.') continue;
                if (forbidden.Contains((x, y))) continue;
                _tiles[x, y] = 'R';
                forbidden.Add((x, y));
                placed++;
            }
        }

        public void Reveal(Position p) => _explored[p.X, p.Y] = true;
        public bool IsWalkable(Position p) => _tiles[p.X, p.Y] != '#';
        public char GetTile(Position p) => _tiles[p.X, p.Y];
        public bool IsRunenstein(Position p) => _tiles[p.X, p.Y] == 'R';
        public bool IsBoss(Position p) => _tiles[p.X, p.Y] == 'B';

        public string RenderAscii(Position playerPos)
        {
            var sb = new StringBuilder();
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (playerPos.X == x && playerPos.Y == y) { sb.Append('S'); continue; }
                    if (!_explored[x, y]) { sb.Append(' '); continue; }
                    sb.Append(_tiles[x, y]);
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }

        public string RenderAsciiMini(Position playerPos, int radius)
        {
            var sb = new StringBuilder();
            int minY = Math.Max(0, playerPos.Y - radius);
            int maxY = Math.Min(Height - 1, playerPos.Y + radius);
            int minX = Math.Max(0, playerPos.X - radius);
            int maxX = Math.Min(Width - 1, playerPos.X + radius);

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    if (playerPos.X == x && playerPos.Y == y) { sb.Append('S'); continue; }
                    if (!_explored[x, y]) { sb.Append(' '); continue; }
                    sb.Append(_tiles[x, y]);
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }
}
