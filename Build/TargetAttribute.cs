using System;

namespace Build
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class TargetAttribute : Attribute
    {
        /// <summary>
        /// An optional override to the target name. If not provided, will default to the reflected.
        /// method name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The targets the named target depends on.
        /// </summary>
        public string[] DependsOn { get; set; }

        /// <summary>
        /// Marks the method as a build target.
        /// </summary>
        /// <param name="dependsOn">The list of targets that this target depends on.</param>
        public TargetAttribute(params string[] dependsOn)
        {
            DependsOn = dependsOn ?? new string[0];
        }
    }
}