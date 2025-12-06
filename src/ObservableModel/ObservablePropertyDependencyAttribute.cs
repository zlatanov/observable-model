using System;

namespace ObservableModel
{
    [AttributeUsage( AttributeTargets.Property, AllowMultiple = false )]
    public sealed class ObservablePropertyDependencyAttribute : Attribute
    {
        public ObservablePropertyDependencyAttribute( params string[] propertyNames )
        {
            PropertyNames = propertyNames;
        }

        public string[] PropertyNames { get; }
    }
}
