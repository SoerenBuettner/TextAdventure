namespace TextAbenteuer.Services
{
    /// <summary>
    /// Eingebettete Konfiguration (keine externen Dateien nötig).
    /// </summary>
    public class ConfigService
    {
        public Dictionary<string, Difficulty> Difficulties { get; } = new();

        public ConfigService()
        {
            Difficulties["leicht"] = new Difficulty
            {
                Width = 8,
                Height = 8,
                Runensteine = new RunenRange { Min = 2, Max = 4 },
                Encounter = new Encounter
                {
                    Enemy = 20,
                    Trap = 10,
                    Loot = 25,
                    Npc = 10,
                    CombatThreshold = 4,
                    TrapThreshold = 3
                }
            };
            Difficulties["mittel"] = new Difficulty
            {
                Width = 12,
                Height = 12,
                Runensteine = new RunenRange { Min = 2, Max = 3 },
                Encounter = new Encounter
                {
                    Enemy = 28,
                    Trap = 15,
                    Loot = 20,
                    Npc = 8,
                    CombatThreshold = 4,
                    TrapThreshold = 4
                }
            };
            Difficulties["schwer"] = new Difficulty
            {
                Width = 16,
                Height = 16,
                Runensteine = new RunenRange { Min = 1, Max = 2 },
                Encounter = new Encounter
                {
                    Enemy = 35,
                    Trap = 20,
                    Loot = 15,
                    Npc = 6,
                    CombatThreshold = 5,
                    TrapThreshold = 4
                }
            };
        }
    }

    public class Difficulty
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public RunenRange Runensteine { get; set; } = new();
        public Encounter Encounter { get; set; } = new();
    }

    public class RunenRange
    {
        public int Min { get; set; } = 1;
        public int Max { get; set; } = 2;
    }

    public class Encounter
    {
        public int Enemy { get; set; } = 20; // Prozent
        public int Trap { get; set; } = 10; // Prozent
        public int Loot { get; set; } = 20; // Prozent
        public int Npc { get; set; } = 10; // Prozent
        public int CombatThreshold { get; set; } = 4; // W6 >= Threshold => Erfolg
        public int TrapThreshold { get; set; } = 3;
    }
}
