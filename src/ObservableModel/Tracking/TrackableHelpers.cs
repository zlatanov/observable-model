using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;

namespace ObservableModel
{
    internal static class TrackableHelpers
    {
        public class Property
        {
            public Property( PropertyInfo info )
            {
                Info = info;
                BaseSetMethod = info.GetSetMethod( nonPublic: true );
                BaseGetMethod = info.GetGetMethod( nonPublic: true ) ?? throw new InvalidOperationException( $"The property {info.Name} doesn't have a getter." );
            }

            public PropertyInfo Info { get; }
            public string Name => Info.Name;
            public Type PropertyType => Info.PropertyType;
            public MethodInfo? BaseSetMethod { get; }
            public MethodInfo BaseGetMethod { get; }
            public bool IsReadOnly => BaseSetMethod is null;
        }


        public sealed class TrackableProperty : Property
        {
            public TrackableProperty( PropertyInfo info, TrackablePropertyAttribute attr ) : base( info )
            {
                var reference = attr.ReferenceOnly;

                ReferenceOnly = reference;
                IsTrackable = !reference && typeof( ITrackable ).IsAssignableFrom( info.PropertyType );
            }

            public bool ReferenceOnly { get; }

            public bool IsTrackable { get; }

            [NotNull]
            public FieldBuilder? OriginalField { get; set; }

            public MethodBuilder? SetMethod { get; set; }

            [NotNull]
            public FieldBuilder? TrackablePropertyField { get; set; }

            [NotNull]
            public Type? TrackablePropertyFieldType { get; set; }

            [NotNull]
            public TypeBuilder? TrackablePropertyFieldTypeBuilder { get; set; }
        }
    }
}
