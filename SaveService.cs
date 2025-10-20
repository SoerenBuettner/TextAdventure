using System.Text.Json;
using TextAbenteuer.Models;

namespace TextAbenteuer.Services
{
    /// <summary>
    /// Speichert und lädt Spielstände als JSON.
    /// </summary>
    public class SaveService
    {
        public bool Save(string path, Player player, World world, string difficulty)
        {
            try
            {
                var dto = new SaveData { Player = player, World = world, Difficulty = difficulty };
                var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
                return true;
            }
            catch { return false; }
        }

        public SaveData? Load(string path)
        {
            try
            {
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<SaveData>(json);
            }
            catch { return null; }
        }
    }

    public class SaveData
    {
        public Player Player { get; set; } = default!;
        public World World { get; set; } = default!;
        public string Difficulty { get; set; } = "mittel";
    }
}
