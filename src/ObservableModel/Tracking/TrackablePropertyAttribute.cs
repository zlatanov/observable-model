using System;

namespace ObservableModel
{
    [AttributeUsage( AttributeTargets.Property, AllowMultiple = false, Inherited = false )]
    public class TrackablePropertyAttribute : ObservablePropertyAttribute
    {
        /// <summary>
        /// Instructs the tracker to compare references of the current property values.<br />
        /// It will ignore value equatability and nested change tracking.
        /// </summary>
        public bool ReferenceOnly { get; set; }
    }
}
