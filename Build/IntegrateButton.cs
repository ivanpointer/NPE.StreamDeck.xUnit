using NLog;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using static SimpleExec.Command;

namespace Build
{
    internal class IntegrateButton : IntegrateButtonBase<Options>
    {
        public const string DEFAULT_PLUGIN_PROJECT_NAME = "npe.streamdeck.xunit";
        public const string DEFAULT_PLUGIN_UUID = DEFAULT_PLUGIN_PROJECT_NAME;

        /// <summary>
        /// The temp directory we're working with.
        /// </summary>
        private DirectoryInfo TempDir => new DirectoryInfo(Options.TempDir);

        private FileInfo PluginFile => new FileInfo(Path.Combine(Options.TempDir, $"{Options.PluginUuid}.streamDeckPlugin"));

        private DirectoryInfo PluginBinDir => new DirectoryInfo(Options.PluginBinDir);

        private DirectoryInfo PluginOutputDir => new DirectoryInfo(Path.Combine(Options.TempDir, $"{Options.PluginUuid}.sdPlugin"));

        public IntegrateButton(string[] args, Options options, ILogger logger)
            : base(args, options, logger)
        {
        }

        #region Setup Options

        protected override Options SetupOptions(Options options)
        {
            options.TempDir ??= ".build_temp";
            options.PluginProjectName ??= DEFAULT_PLUGIN_PROJECT_NAME;
            options.PluginUuid ??= DEFAULT_PLUGIN_UUID;

            options.StreamDeckDistributionTool = LocateStreamDeckDistTool(options);
            options.StreamDeckExecutable = LocateStreamDeckExecutable(options);
            options.PluginBinDir = LocatePluginBinDir(options);

            return options;
        }

        private string LocateStreamDeckDistTool(Options options)
        {
            var path = options.StreamDeckDistributionTool
                ?? Path.Combine(Environment.GetEnvironmentVariable("STREAMDECK_DISTTOOL_HOME"), "DistributionTool.exe");

            if (!File.Exists(path))
                throw new ArgumentException("Stream Deck Distribution Tool not found, either pass it in as an option, or set the \"STREAMDECK_DISTTOOL_HOME\" environment variable to the directory containing \"DistributionTool.exe\"");

            return path;
        }

        private string LocateStreamDeckExecutable(Options options)
        {
            var path = options.StreamDeckExecutable;
            if (string.IsNullOrEmpty(path))
            {
                var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                path = Path.Combine(programFiles, @"Elgato\StreamDeck\StreamDeck.exe");
                if (File.Exists(path))
                    return path;

                programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
                path = Path.Combine(programFiles, @"Elgato\StreamDeck\StreamDeck.exe");
            }

            if (!File.Exists(path))
                throw new ArgumentException("StreamDeck.exe was not found, specify it on the command line.");

            return path;
        }

        private string LocatePluginBinDir(Options options)
        {
            var path = options.PluginBinDir;
            if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
            {
                if (File.Exists(Path.Combine(path, $"{options.PluginProjectName}.exe")))
                    return path;

                throw new ArgumentException($"\"{options.PluginProjectName}.exe\" not found in \"{path}\"; please specify a valid location.");
            }

            try
            {
                var currentDir = new DirectoryInfo(Directory.GetCurrentDirectory());
                path = currentDir.FullName;

                while (!Directory.Exists(Path.Combine(path, options.PluginProjectName)))
                {
                    currentDir = currentDir.Parent;
                    var newPath = currentDir.FullName;
                    if (string.Equals(newPath, path, StringComparison.InvariantCultureIgnoreCase))
                        break;
                    path = newPath;
                }
            }
            catch (Exception cause)
            {
                Logger.Warn(cause, "Failed to find the project bin directory for the plugin.");
            }

            if (Directory.Exists(Path.Combine(path, options.PluginProjectName)))
                path = Path.Combine(path, options.PluginProjectName, @"bin\Debug", $"{options.PluginProjectName}.sdPlugin");

            if (!File.Exists(Path.Combine(path, $"{options.PluginProjectName}.exe")))
                throw new ArgumentException("Failed to find the plugin bin directory.  Please specify it.");

            return path;
        }

        #endregion Setup Options

        #region Targets

        [Target(
            nameof(Clean),
            nameof(Setup),
            nameof(KillStreamDeckProcesses),
            nameof(KillPluginProcesses),
            nameof(CopyPluginToTempDir),
            nameof(PackageStreamDeckPlugin),
            nameof(RemoveOldPlugin),
            nameof(InstallPlugin)
        )]
        public override void Default()
        {
            Logger.Info("The default target has no functionality, it is only used to chain the other targets together for a standard build.");
        }

        [Target]
        public void Clean()
        {
            if (TempDir.Exists)
                TempDir.Delete(true);
        }

        [Target]
        public void Setup()
        {
            if (!TempDir.Exists)
                TempDir.Create();
        }

        [Target]
        public void KillStreamDeckProcesses()
        {
            foreach (var process in Process.GetProcesses(".")
                .Where(p => string.Equals("streamdeck.exe", p.ProcessName, StringComparison.InvariantCultureIgnoreCase)))
                process.Kill();
            Thread.Sleep(TimeSpan.FromSeconds(1));
        }

        [Target]
        public void KillPluginProcesses()
        {
            foreach (var process in Process.GetProcesses(".")
                .Where(p => string.Equals($"{Options.PluginUuid}.exe", p.ProcessName, StringComparison.InvariantCultureIgnoreCase)))
                process.Kill();
            Thread.Sleep(TimeSpan.FromSeconds(1));
        }

        [Target]
        public void CopyPluginToTempDir()
        {
            CopyDirectory(PluginBinDir, PluginOutputDir, true);
        }

        [Target]
        public void PackageStreamDeckPlugin()
        {
            Run(Options.StreamDeckDistributionTool, $"-b -i \"{PluginOutputDir.FullName}\" -o \"{TempDir.FullName}\"");
        }

        [Target]
        public void RemoveOldPlugin()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var pluginPath = Path.Combine(appDataPath, @"Elgato\StreamDeck\Plugins", $"{Options.PluginUuid}.sdPlugin");
            if (Directory.Exists(pluginPath))
                Directory.Delete(pluginPath, true);
        }

        [Target]
        public void RestartStreamDeck()
        {
            Process.Start(Options.StreamDeckExecutable);
            var sleepSpan = TimeSpan.FromSeconds(7);
            Logger.Debug($"Started Stream Deck, waiting {sleepSpan.TotalSeconds} seconds to let it start...");
            Thread.Sleep(TimeSpan.FromSeconds(7));
        }

        [Target]
        public void InstallPlugin()
        {
            var pluginFile = new FileInfo(Path.Combine(Options.TempDir, $"{Options.PluginUuid}.streamDeckPlugin"));

            if (!pluginFile.Exists)
                throw new Exception($"The Stream Deck Plugin was not found at \"{pluginFile.FullName}\"");

            try
            {
                Run("explorer", $"\"{pluginFile.FullName}\"");
            }
            catch (Exception cause)
            {
                Logger.Debug(cause, "Caught an exception installing the plugin.  Exceptions are expected here.");
            }
        }

        #endregion Targets
    }
}