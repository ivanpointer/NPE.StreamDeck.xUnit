using CommandLine;

namespace Build
{
    /// <summary>
    /// The options for the <see cref="IntegrateButton"/>.
    /// </summary>
    internal class Options
    {
        [Option('t', "tempdir", Required = false, HelpText = "Override the temp (build) directory.")]
        public string TempDir { get; set; }

        [Option('d', "disttool", Required = false, HelpText = "The path to the Elgato Stream Deck distribution tool.  If not found via the STREAMDECK_DISTTOOL_HOME environment variable, you will need to pass it in as an argument.")]
        public string StreamDeckDistributionTool { get; set; }

        [Option('s', "streamdeck", Required = false, HelpText = "The path to the Elgato Stream Deck executable.  If not found via \"Program Files\", you will need to pass it in as an argument.")]
        public string StreamDeckExecutable { get; set; }

        [Option('i', "pluginbindir", Required = false, HelpText = "The bath to the directory containing the binaries of the plugin.  The build tool will try to find this based on the known directory structure.")]
        public string PluginBinDir { get; set; }

        [Option('u', "uuid", Required = false, HelpText = "An optional override to the plugin's UUID.")]
        public string PluginUuid { get; set; }

        [Option('p', "pluginproject", Required = false, HelpText = "The name of the plugin project we're building.")]
        public string PluginProjectName { get; set; }
    }
}