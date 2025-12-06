using System;

namespace ObservableModel
{
    [AttributeUsage( AttributeTargets.Property, AllowMultiple = false, Inherited = false )]
    public class ObservablePropertyAttribute : Attribute
    {
    }
}
