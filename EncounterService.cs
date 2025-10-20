using TextAbenteuer.Models;

namespace TextAbenteuer.Services
{
    public enum EncounterType { None, Enemy, Trap, Loot, Npc, Boss }

    public class EncounterState
    {
        public EncounterType Type { get; set; } = EncounterType.None;
        public string? Detail { get; set; } // z. B. Loot-Name
        public int BossSuccess { get; set; } = 0;
        public int BossFailures { get; set; } = 0;
    }

    /// <summary>
    /// Einfache Encounter-Logik: abhängig von der Schwierigkeit werden Begegnungen gewürfelt.
    /// Prüfungen erfolgen mit W6 und Schwellwerten aus der Konfiguration.
    /// </summary>
    public class EncounterService
    {
        private readonly ConfigService _config;
        private readonly Random _rng;

        public EncounterService(ConfigService cfg, Random rng)
        {
            _config = cfg;
            _rng = rng;
        }

        public EncounterState Roll(string difficulty, World world, Player player, Position pos)
        {
            var state = new EncounterState();

            if (world.IsBoss(pos))
            {
                state.Type = EncounterType.Boss;
                return state;
            }

            var e = _config.Difficulties[difficulty].Encounter;
            int roll = _rng.Next(100); // 0..99

            if (roll < e.Enemy) state.Type = EncounterType.Enemy;
            else if (roll < e.Enemy + e.Trap) state.Type = EncounterType.Trap;
            else if (roll < e.Enemy + e.Trap + e.Loot)
            {
                state.Type = EncounterType.Loot;
                state.Detail = RandomLoot();
            }
            else if (roll < e.Enemy + e.Trap + e.Loot + e.Npc)
            {
                state.Type = EncounterType.Npc;
            }
            else state.Type = EncounterType.None;

            return state;
        }

        public bool ResolveFight(Player player, string difficulty)
        {
            int d6 = _rng.Next(1, 7);
            int thr = _config.Difficulties[difficulty].Encounter.CombatThreshold;
            return d6 >= thr; // Erfolg?
        }

        public bool ResolveDisarm(Player player, string difficulty)
        {
            int d6 = _rng.Next(1, 7);
            int thr = _config.Difficulties[difficulty].Encounter.TrapThreshold;
            return d6 >= thr;
        }

        private string RandomLoot()
        {
            var table = new[] { "Heilkraut", "Fackel", "Dietrich", "Seil", "Schatz" };
            return table[_rng.Next(table.Length)];
        }
    }
}
