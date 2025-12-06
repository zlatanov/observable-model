using System.Collections.Generic;
using Xunit;

namespace ObservableModel
{
    public class ObservableDefaultInterfaceProperties
    {
        [Fact]
        public void Test()
        {
            var person = Person.Create( age: 10 );
            var feature = (IAgeFeature)person;

            var propertyChanges = new List<string>();
            person.PropertyChanges.Subscribe( x => propertyChanges.Add( x.PropertyName ) );

            Assert.Empty( propertyChanges );
            Assert.Equal( 10, feature.Age );
            Assert.True( feature.IsAgeEven );

            feature.Age = 11;
            Assert.False( feature.IsAgeEven );
            Assert.Equal( new[] { "IsChanged", "Age", "IsAgeEven" }, propertyChanges );
        }
    }
}
