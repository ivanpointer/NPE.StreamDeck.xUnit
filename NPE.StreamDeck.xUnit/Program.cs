using BarRaider.SdTools;

namespace NPE.StreamDeck.xUnit
{
    internal class Program
    {
        public const string PLUGIN_UUID = "NPE.StreamDeck.xUnit";

        private static void Main(string[] args)
        {
            // Uncomment this line of code to allow for debugging
            //while (!System.Diagnostics.Debugger.IsAttached) { System.Threading.Thread.Sleep(100); }

            SDWrapper.Run(args);
        }
    }
}