using NLog;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using static Bullseye.Targets;

namespace Build
{
    public abstract class IntegrateButtonBase<TOptions>
    {
        #region Properties and Options

        protected string[] Args { get; private set; }

        protected TOptions Options { get; private set; }

        protected ILogger Logger { get; private set; }

        #endregion Properties and Options

        #region Scaffolding

        public IntegrateButtonBase(string[] args, TOptions options, ILogger logger)
        {
            Args = args;
            Logger = logger;

            Options = SetupOptions(options);
            SetupTargets();
        }

        protected abstract TOptions SetupOptions(TOptions options);

        protected virtual void SetupTargets()
        {
            AddTargetsByAttribute();
        }

        private void AddTargetsByAttribute()
        {
            var thisType = this.GetType();

            // Iterate over all this instance's methods
            foreach (var method in thisType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                AddTargetsForMethod(method);
        }

        private void AddTargetsForMethod(MethodInfo method)
        {
            var attributes = (TargetAttribute[])Attribute.GetCustomAttributes(method, typeof(TargetAttribute));
            if (attributes?.Length == 0)
                return;

            if (method.GetParameters().Any(p => !p.IsOptional))
                throw new InvalidOperationException("Target actions must be parameterless.");

            foreach (var attribute in attributes)
                AddTargetForAttribute(method, attribute);
        }

        private void AddTargetForAttribute(MethodInfo method, TargetAttribute attribute)
        {
            var targetName = attribute.Name ?? method.Name;
            var dependsOn = attribute.DependsOn ?? new string[0];

            Target(targetName, dependsOn, () => method.Invoke(this, parameters: default));
        }

        #endregion Scaffolding

        #region Targets

        /// <summary>
        /// The main entry point for running the build.
        /// </summary>
        public virtual void Press()
        {
            RunTargetsAndExit(Args);
        }

        /// <summary>
        /// The default target.
        /// </summary>
        public abstract void Default();

        #endregion Targets

        #region Helpers

        protected static void CopyDirectory(DirectoryInfo sourceDir, DirectoryInfo destDir, bool recursive = false)
        {
            if (!sourceDir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDir.FullName);
            }

            DirectoryInfo[] dirs = sourceDir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDir.FullName))
            {
                Directory.CreateDirectory(destDir.FullName);
                destDir = new DirectoryInfo(destDir.FullName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = sourceDir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDir.FullName, file.Name);
                file.CopyTo(temppath, false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (recursive)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    var nextDir = new DirectoryInfo(Path.Combine(destDir.FullName, subdir.Name));
                    CopyDirectory(subdir, nextDir, recursive);
                }
            }
        }

        #endregion Helpers
    }
}