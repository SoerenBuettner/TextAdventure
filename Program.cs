using System;
using TextAbenteuer;

namespace TextAbenteuerApp
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            var game = new Game();
            game.Run();
        }
    }
}
