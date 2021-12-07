using System;

namespace Hreidmar.GUI
{
    public static class Program
    {
        
        
        [STAThread]
        public static void Main()
        {
            using var game = new MonoGameController();
            game.Run();
        }
    }
}