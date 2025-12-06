using System.Linq;
using Xunit;

namespace ObservableModel
{
    public class ObservableAggregateTests
    {
        [Fact]
        public void ValueShouldUpdateOnChanges()
        {
            var list = new ObservableList<Person>();
            var aggregate = list.Aggregate<int>( ( acc, item ) => acc + item.Age );
            AssertValue();
            
            list.Add( Person.Create( age: 10 ) );
            AssertValue();

            list.Add( Person.Create( age: 12 ) );
            AssertValue();

            list[ 0 ].Age = 33;
            AssertValue();

            list.RemoveAt( 0 );
            AssertValue();

            list.Clear();
            AssertValue();

            void AssertValue() => Assert.Equal( list.Sum( x => x.Age ), aggregate.Value );
        }
    }
}
