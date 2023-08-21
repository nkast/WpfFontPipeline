using System;
using Microsoft.Xna.Framework;

namespace DrawString
{
    public static class Program
    { 
        static void Main()
        {
            using (Game game = new Game1())
            {
                game.Run();
            }
        }
    }
}
