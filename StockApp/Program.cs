using System;

namespace StockApp
{

    public class Program
    {
        private static App _app;

        static void Main(string[] args)
        {
            _app = new App();
            _app.MakeMonies();
        }
    }
}
