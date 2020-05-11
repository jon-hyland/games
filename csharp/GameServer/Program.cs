using System.Threading;

namespace GameServer
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            Main main = new Main();
            main.Start();

            while (true)
            {
                Thread.Sleep(1000);
            }
        }
    }
}
