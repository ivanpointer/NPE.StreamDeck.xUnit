using CommandLine;
using NLog;

namespace Build
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            // Parse the command line arguments, instantiate our integrate button, and press it.
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(o =>
                {
                    var logger = LogManager.GetLogger(typeof(IntegrateButton).FullName);
                    var integrateButton = new IntegrateButton(args, o, logger);
                    integrateButton.Press();
                });
        }
    }
}