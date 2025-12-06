using System.Linq;
using Xunit;

namespace ObservableModel
{
    public class ObservableListTests
    {
        [Fact]
        public void SortBy()
        {
            var list = new ObservableList<Person>
            {
                Person.Create( id: 1, age: 2 ),
                Person.Create( id: 1, age: 3 ),
                Person.Create( id: 1, age: 1 )
            };

            list.SortBy( x => (x.Id, x.Age.Descending()), persist: true );

            Assert.Equal( 3, list[ 0 ].Age );
            Assert.Equal( 2, list[ 1 ].Age );
            Assert.Equal( 1, list[ 2 ].Age );

            Person p1, p2;

            list.Add( p1 = Person.Create( id: 0, age: 33 ) );
            list.Add( p2 = Person.Create( id: 1, age: 33 ) );

            Assert.Equal( p1, list[ 0 ] );
            Assert.Equal( p2, list[ 1 ] );
        }

        [Fact]
        public void SortByShouldBeStable()
        {
            var list = new ObservableList<Person>();
            list.SortBy( x => ( x.Age % 2 ) == 0, persist: true );
            list.AddRange( Enumerable.Range( 0, 1000 ).Select( x => Person.Create( age: x ) ) );


            Assert.Equal( Enumerable.Range( 0, 1000 ).OrderBy( x => x % 2 == 0 ), list.Select( x => x.Age ) );
        }

        [Fact]
        public void Combine()
        {
            var list1 = new ObservableList<string>() { "1", "2", "3" };
            var list2 = new ObservableList<string>() { "4", "5", "6" };
            var combined = ObservableList.Combine( list1, list2 );
            AssertEqual();

            list1.SortBy( x => x.Descending() );
            AssertEqual();

            list1.RemoveAt( 2 );
            list2.RemoveAt( 1 );
            AssertEqual();

            list1.Clear();
            AssertEqual();

            list1.AddRange( [ "1", "2", "3" ] );
            AssertEqual();

            list2.AddRange( [ "x", "y", "z" ] );
            AssertEqual();

            list2.SortBy( x => x.Descending() );
            AssertEqual();

            void AssertEqual() => Assert.Equal( list1.Concat( list2 ), combined );
        }

        [Fact]
        public void UpdateSortedPosition()
        {
            var list = new ObservableList<Person>
            {
                Person.Create( id: 1, age: 12 ),
                Person.Create( id: 2, age: 15 ),
                Person.Create( id: 3, age: 0 )
            };

            list.SortBy( x => x.Age.Descending(), persist: true );

            var element = list[ 2 ];
            element.Age = 13;
            list.UpdateSortPosition( element );
            Assert.Equal( 1, list.IndexOf( element ) );
        }

        [Fact]
        public void UpdateSortPositionShouldIgnoreSelf()
        {
            var list = new ObservableList<Person>();

            list.SortBy( x => x.Age, persist: true );
            list.Add( Person.Create( 1, age: 1 ) );
            list.Add( Person.Create( 2, age: 2 ) );
            list.Add( Person.Create( 3, age: 3 ) );
            list.Add( Person.Create( 4, age: 4 ) );

            list[ 2 ].Age = 6;
            list.UpdateSortPosition( list[ 2 ] );

            Assert.Equal( 6, list.Last.Age );
        }
    }
}
