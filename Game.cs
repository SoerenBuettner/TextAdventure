using System.Numerics;
using System.Text;
using TextAbenteuer.Models;
using TextAbenteuer.Services;

namespace TextAbenteuer
{
    public class Game
    {
        private readonly ConfigService _config = new();
        private readonly SaveService _save = new();
        private EncounterService _encounters = default!;

        private World _world = default!;
        private Player _player = default!;
        private Random _rng = default!;
        private string _difficulty = "mittel";
        private readonly Queue<string> _log = new();

        private EncounterState _pending = new(); // aktives Ereignis

        public void Run()
        {
            Console.OutputEncoding = Encoding.UTF8;
            PrintTitle();
            InitNewGame();
            GameLoop();
        }

        private void PrintTitle()
        {
            Console.Clear();
            Console.WriteLine("=== TEXT-ABENTEUER: DUNGEON & RUNENSTEINE ===");
            Console.WriteLine("Fantasy-Dungeon mit Runensteinen (Speicherpunkte), Encountern und Endboss.");
            Console.WriteLine("Hinweis: Tippe 'hilfe' oder 'legende' für alle Befehle.\n");
        }

        private void InitNewGame()
        {
            Console.Write("Schwierigkeit (leicht/mittel/schwer) [Enter = mittel]: ");
            var diff = Console.ReadLine()?.Trim().ToLowerInvariant();
            if (!string.IsNullOrWhiteSpace(diff) && _config.Difficulties.ContainsKey(diff))
                _difficulty = diff;

            Console.Write("Seed (leer = zufällig): ");
            var seedInput = Console.ReadLine();
            int seed = int.TryParse(seedInput, out var parsed) ? parsed : Environment.TickCount;

            _rng = new Random(seed);
            _encounters = new EncounterService(_config, _rng);

            var d = _config.Difficulties[_difficulty];
            int minR = d.Runensteine.Min;
            int maxR = d.Runensteine.Max;

            _world = new World(d.Width, d.Height, seed, _rng, minR, maxR);
            _player = new Player("Held", _world.Start, hp: 10);

            _world.Reveal(_player.Position);
            Log($"Neues Spiel gestartet (Diff: {_difficulty}, Seed: {seed}).");
            Log("WASD: W=Norden, A=Westen, S=Süden, D=Osten.");
        }

        private void GameLoop()
        {
            while (true)
            {
                DrawUI();
                Console.Write("> ");
                var input = (Console.ReadLine() ?? "").Trim();
                if (string.IsNullOrWhiteSpace(input)) continue;
                var cmd = input.ToLowerInvariant();

                // Encounter-spezifische Befehle zuerst
                if (HandleEncounterCommands(cmd)) continue;

                if (HandleSystem(cmd)) continue;
                if (HandleInfo(cmd)) continue;
                if (HandleMove(cmd)) continue;

                Console.WriteLine("Unbekannter Befehl. Tippe 'hilfe'.");
            }
        }

        private bool HandleEncounterCommands(string cmd)
        {
            if (_pending.Type == EncounterType.None) return false;

            switch (_pending.Type)
            {
                case EncounterType.Enemy:
                    if (cmd == "kaempfen" || cmd == "kämpfen")
                    {
                        bool success = _encounters.ResolveFight(_player, _difficulty);
                        if (success) { Log("Du besiegst das Monster."); _pending = new(); }
                        else { _player.Hp = Math.Max(0, _player.Hp - 1); Log("Der Hieb verfehlt – du kassierst −1 HP."); }
                        return true;
                    }
                    if (cmd == "fliehen")
                    {
                        Log("Du fliehst erfolgreich.");
                        _pending = new();
                        return true;
                    }
                    Console.WriteLine("Befehle im Kampf: 'kaempfen'/'kämpfen', 'fliehen'.");
                    return true;

                case EncounterType.Trap:
                    if (cmd == "entschaerfe" || cmd == "entschärfe")
                    {
                        bool success = _encounters.ResolveDisarm(_player, _difficulty);
                        if (success) { Log("Du entschärfst die Falle."); _pending = new(); }
                        else { _player.Hp = Math.Max(0, _player.Hp - 1); Log("Die Falle schnappt zu – −1 HP."); _pending = new(); }
                        return true;
                    }
                    if (cmd == "umgehen")
                    {
                        Log("Du umgehst die Falle vorsichtig.");
                        _pending = new();
                        return true;
                    }
                    if (cmd == "ignorieren")
                    {
                        _player.Hp = Math.Max(0, _player.Hp - 1);
                        Log("Du ignorierst die Warnung – du nimmst −1 HP Schaden.");
                        _pending = new();
                        return true;
                    }
                    Console.WriteLine("Falle: 'entschaerfe', 'umgehen' oder 'ignorieren'.");
                    return true;

                case EncounterType.Loot:
                    if (cmd == "nehmen")
                    {
                        var item = _pending.Detail ?? "Schatz";
                        _player.Inventory.Add(item);
                        Log($"Du nimmst: {item}.");
                        _pending = new();
                        return true;
                    }
                    if (cmd == "lassen")
                    {
                        Log("Du lässt den Fund liegen.");
                        _pending = new();
                        return true;
                    }
                    Console.WriteLine("Loot: 'nehmen' oder 'lassen'.");
                    return true;

                case EncounterType.Npc:
                    if (cmd == "rede")
                    {
                        Log("Ein Magier flüstert: 'Bewahre dich vor den Fallen. Suche Runensteine!'");
                        _pending = new();
                        return true;
                    }
                    Console.WriteLine("NPC: 'rede'.");
                    return true;

                case EncounterType.Boss:
                    if (cmd == "kaempfen" || cmd == "kämpfen")
                    {
                        bool success = _encounters.ResolveFight(_player, _difficulty);
                        if (success) { _pending.BossSuccess++; Log($"Boss: Treffer! ({_pending.BossSuccess}/3)"); }
                        else { _pending.BossFailures++; _player.Hp = Math.Max(0, _player.Hp - 1); Log($"Boss: Du wirst getroffen (−1 HP). Fehlversuche: {_pending.BossFailures}/3"); }
                        if (_pending.BossSuccess >= 3)
                        {
                            Console.WriteLine("⚔️  Der Endboss fällt! Du hast gewonnen!");
                            Environment.Exit(0);
                        }
                        if (_pending.BossFailures >= 3 || _player.Hp <= 0)
                        {
                            Console.WriteLine("☠️  Du wurdest besiegt…");
                            Environment.Exit(0);
                        }
                        return true;
                    }
                    if (cmd == "fliehen")
                    {
                        Log("Du ziehst dich zurück. Der Boss lauert weiter.");
                        _pending = new();
                        return true;
                    }
                    Console.WriteLine("Bosskampf: 'kaempfen'/'kämpfen' (3 Erfolge) oder 'fliehen'.");
                    return true;
            }
            return false;
        }

        private bool HandleSystem(string cmd)
        {
            switch (cmd)
            {
                case "beenden":
                    Console.WriteLine("Spiel wird beendet. Bis bald!");
                    Environment.Exit(0);
                    return true;
                case "speichern":
                    if (_world.IsRunenstein(_player.Position))
                    {
                        Directory.CreateDirectory("saves");
                        var ok = _save.Save(Path.Combine("saves", "slot1.json"), _player, _world, _difficulty);
                        Console.WriteLine(ok ? "Gespeichert am Runenstein (Slot 1)." : "Speichern fehlgeschlagen.");
                    }
                    else
                    {
                        Console.WriteLine("Hier kannst du nicht speichern. Suche einen Runenstein (R).");
                    }
                    return true;
                case "laden":
                    var path = Path.Combine("saves", "slot1.json");
                    if (File.Exists(path))
                    {
                        var loaded = _save.Load(path);
                        if (loaded != null)
                        {
                            _player = loaded.Player;
                            _world = loaded.World;
                            _difficulty = loaded.Difficulty;
                            _rng = new Random(_world.Seed);
                            _encounters = new EncounterService(_config, _rng);
                            Console.WriteLine("Spielstand geladen (Slot 1).");
                        }
                        else Console.WriteLine("Speicherstand beschädigt.");
                    }
                    else Console.WriteLine("Kein Speicherstand vorhanden.");
                    return true;
                default:
                    return false;
            }
        }

        private bool HandleInfo(string cmd)
        {
            switch (cmd)
            {
                case "hilfe":
                case "legende":
                    PrintFullLegend();
                    return true;
                case "karte":
                    Console.WriteLine(_world.RenderAscii(_player.Position));
                    return true;
                case "stats":
                    Console.WriteLine($"HP: {_player.Hp}/10 · Position: {_player.Position.X},{_player.Position.Y}");
                    if (_world.IsRunenstein(_player.Position))
                        Console.WriteLine("Du stehst bei einem Runenstein: Hier kannst du speichern.");
                    if (_pending.Type != EncounterType.None)
                        Console.WriteLine($"Aktives Ereignis: {_pending.Type}");
                    return true;
                case "inventar":
                    Console.WriteLine(_player.Inventory.Count == 0 ? "Inventar ist leer." :
                        "Inventar: " + string.Join(", ", _player.Inventory));
                    return true;
                case "log":
                    Console.WriteLine(_log.Count == 0 ? "Kein Verlauf." : string.Join(" | ", _log));
                    return true;
                default:
                    return false;
            }
        }

        private bool HandleMove(string cmd)
        {
            // WASD (primär) + n/s/o/w (Sekundär)
            Position? target = cmd switch
            {
                "w" or "n" => _player.Position with { Y = Math.Max(0, _player.Position.Y - 1) },
                "s" => _player.Position with { Y = Math.Min(_world.Height - 1, _player.Position.Y + 1) },
                "d" or "o" => _player.Position with { X = Math.Min(_world.Width - 1, _player.Position.X + 1) },
                "a" => _player.Position with { X = Math.Max(0, _player.Position.X - 1) },
                _ => null
            };
            if (target == null) return false;

            if (!_world.IsWalkable(target.Value))
            {
                Console.WriteLine("Eine Wand versperrt den Weg.");
                return true;
            }

            _player.Position = target.Value;
            _world.Reveal(_player.Position);

            // Encounter würfeln
            _pending = _encounters.Roll(_difficulty, _world, _player, _player.Position);

            switch (_pending.Type)
            {
                case EncounterType.None:
                    Log("Du bewegst dich weiter und erkundest den Dungeon.");
                    break;
                case EncounterType.Enemy:
                    Console.WriteLine("Ein Monster greift an! Befehle: 'kaempfen'/'kämpfen', 'fliehen'.");
                    break;
                case EncounterType.Trap:
                    Console.WriteLine("Du bemerkst eine Falle! Befehle: 'entschaerfe', 'umgehen', 'ignorieren'.");
                    break;
                case EncounterType.Loot:
                    Console.WriteLine($"Du findest etwas: '{_pending.Detail}'. Befehle: 'nehmen' oder 'lassen'.");
                    break;
                case EncounterType.Npc:
                    Console.WriteLine("Ein geheimnisvoller Magier steht vor dir. Befehl: 'rede'.");
                    break;
                case EncounterType.Boss:
                    Console.WriteLine("⚔️  Der Endboss steht vor dir! 'kaempfen'/'kämpfen' (3 Erfolge) oder 'fliehen'.");
                    break;
            }

            if (_world.IsRunenstein(_player.Position))
                Console.WriteLine("Ein Runenstein leuchtet sanft. Hier kannst du 'speichern'.");

            if (_player.Hp <= 0)
            {
                Console.WriteLine("☠️  Du bist deinen Verletzungen erlegen.");
                Environment.Exit(0);
            }

            return true;
        }

        private void DrawUI()
        {
            Console.WriteLine();
            Console.WriteLine("-----");
            Console.WriteLine(_world.RenderAsciiMini(_player.Position, radius: 2));
            Console.WriteLine($"HP: {_player.Hp}/10   Pos: {_player.Position.X},{_player.Position.Y}   Schwierigkeit: {_difficulty}");
            PrintMiniLegend();
        }

        private void PrintMiniLegend()
        {
            var extra = _pending.Type switch
            {
                EncounterType.Enemy => " | Kampf: kaempfen/kämpfen, fliehen",
                EncounterType.Trap => " | Falle: entschaerfe, umgehen, ignorieren",
                EncounterType.Loot => " | Loot: nehmen, lassen",
                EncounterType.Npc => " | NPC: rede",
                EncounterType.Boss => " | Boss: kaempfen/kämpfen (3), fliehen",
                _ => ""
            };
            Console.WriteLine("[WASD: W=Norden, A=Westen, S=Süden, D=Osten]  [Info: karte, stats, inventar, log]  [System: speichern (nur bei R), laden, beenden]  (hilfe)" + extra);
        }

        private void PrintFullLegend()
        {
            Console.WriteLine("\n=== LEGENDE / HILFE ===");
            Console.WriteLine("Bewegen: W (Norden), A (Westen), S (Süden), D (Osten) – alternativ n/s/o/w");
            Console.WriteLine("Information: karte, stats, inventar, log, hilfe/legende");
            Console.WriteLine("System: speichern (nur auf Runenstein 'R'), laden, beenden");
            Console.WriteLine("Kampf: kaempfen/kämpfen (W6-Probe), fliehen");
            Console.WriteLine("Falle: entschaerfe, umgehen, ignorieren");
            Console.WriteLine("Loot: nehmen, lassen");
            Console.WriteLine("NPC: rede");
            Console.WriteLine("Boss: 3 Kampferfolge → Sieg; max −1 HP pro Aktion.");
            Console.WriteLine();
        }

        private void Log(string msg)
        {
            _log.Enqueue(msg);
            while (_log.Count > 5) _log.Dequeue();
        }
    }
}
