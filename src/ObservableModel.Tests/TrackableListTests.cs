using System.Linq;
using Xunit;

namespace ObservableModel
{
    public class TrackableListTests
    {
        [Fact]
        public void RemoveAndAddShouldHaveNoChanges()
        {
            var list = new TrackableList<string>();

            list.BeginInit();
            list.Add( "1" );
            list.Add( "2" );
            list.EndInit();

            Assert.False( list.IsChanged );

            list.Remove( "1" );
            Assert.True( list.IsChanged );

            list.Insert( 0, "1" );
            Assert.False( list.IsChanged );
        }

        [Fact]
        public void InitializingWithChangedItemShouldChangeTheCollection()
        {
            var list = new TrackableList<Person>();
            var person = Person.Create();

            person.Name = "Ivan";

            Assert.False( list.IsChanged );
            Assert.True( person.IsChanged );

            list.BeginInit();
            list.Add( person );
            list.EndInit();

            Assert.True( list.IsChanged );

            var changes = list.GetChangedItems().ToList();
            Assert.Single( changes );
            Assert.Equal( TrackableListChangeType.Change, changes[ 0 ].Type );
        }

        [Fact]
        public void RejectChanges()
        {
            var list = new TrackableList<string>
            {
                "1"
            };
            list.RejectChanges();

            Assert.Empty( list );

            list.Add( "1" );
            list.RejectChanges();

            Assert.Empty( list );
        }

        [Fact]
        public void MoveItemShouldReturnChange()
        {
            var list = new TrackableList<string>( [ "1", "2", "3" ] );

            list.Move( 0, 1 );
            Assert.True( list.IsChanged );

            var changes = list.GetChangedItems().ToArray();

            Assert.Equal( 2, changes.Length );
            Assert.Equal( TrackableListChangeType.Change, changes[ 0 ].Type );
            Assert.Equal( "2", changes[ 0 ].Item );
            Assert.Equal( "2", changes[ 0 ].OriginalItem );
            Assert.Equal( TrackableListChangeType.Change, changes[ 1 ].Type );
            Assert.Equal( "1", changes[ 1 ].Item );
            Assert.Equal( "1", changes[ 1 ].OriginalItem );
        }
    }
}
