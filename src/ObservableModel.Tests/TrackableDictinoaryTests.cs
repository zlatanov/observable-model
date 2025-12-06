using System;
using Xunit;

namespace ObservableModel
{
    public class TrackableDictinoaryTests
    {
        [Fact]
        public void RemoveAndAddShouldHaveNoChanges()
        {
            var dictionary = new TrackableDictionary<int, Item>( x => x.Key );
            var item = Item.Create( 1, "a" );
            dictionary.BeginInit();
            dictionary.Reset( [ item, Item.Create( 2 ) ] );
            //dictionary.Add( item );
            //dictionary.Add( Item.Create( 2 ) );
            dictionary.EndInit();

            Assert.False( dictionary.IsChanged );

            item.Value = "b";
            Assert.True( dictionary.IsChanged );

            dictionary.RemoveKey( 1 );
            Assert.True( dictionary.IsChanged );

            dictionary.Add( Item.Create( 1, "c" ) );
            Assert.True( dictionary.IsChanged );

            dictionary.GetValue( 1 ).Value = "a";
            Assert.False( dictionary.IsChanged );
        }

        [Fact]
        public void RejectChangesEmpty()
        {
            var dictionary = new TrackableDictionary<int, string>( x => x.Length )
            {
                "Test",
                "Not Test"
            };

            Assert.True( dictionary.IsChanged );

            dictionary.RejectChanges();

            Assert.False( dictionary.IsChanged );
            Assert.Empty( dictionary );
        }

        [Fact]
        public void RejectChangesNotEmpty()
        {
            var dictionary = new TrackableDictionary<int, Person>( x => x.Id )
            {
                Person.Create( id: 1, name: "Person 1" ),
                Person.Create( id: 2, name: "Person 2" ),
            };
            dictionary.AcceptChanges();

            Assert.False( dictionary.IsChanged );

            dictionary[ 0 ].Name = "Person 1 Changed";
            dictionary[ 1 ].Name = "Person 2 Changed";

            dictionary.RemoveKey( 1 );
            Assert.True( dictionary.IsChanged );

            dictionary.RejectChanges();
            Assert.False( dictionary.IsChanged );
            Assert.Equal( 2, dictionary.Count );

            Assert.Equal( Person.Create( id: 1, name: "Person 1" ), dictionary[ 0 ], TrackableEqualityComparer<Person>.Default );
            Assert.Equal( Person.Create( id: 2, name: "Person 2" ), dictionary[ 1 ], TrackableEqualityComparer<Person>.Default );
        }

        [Fact]
        public void InitializingWithChangedItemShouldChangeTheCollection()
        {
            var dictionary = new TrackableDictionary<int, Item>( x => x.Key );
            var item = Item.Create( 1 );

            item.Value = "Test";
            Assert.True( item.IsChanged );

            dictionary.BeginInit();
            dictionary.Add( item );
            dictionary.EndInit();

            Assert.True( dictionary.IsChanged );

            dictionary.RemoveKey( 1 );
            Assert.True( dictionary.IsChanged );

            dictionary.Add( Item.Create( 1 ) );
            Assert.False( dictionary.IsChanged );
        }

        [Fact]
        public void ChangingItemShouldChangeTheCollection()
        {
            var collection = new TrackableDictionary<int, Item>( x => x.Key );

            collection.BeginInit();
            collection.Add( Item.Create( 5, "Test" ) );
            collection.EndInit();

            Assert.False( collection.IsChanged );

            collection[ 0 ].Value = "Changed";
            Assert.True( collection.IsChanged );

            collection[ 0 ].AcceptChanges();
            Assert.False( collection.IsChanged );

            collection[ 0 ].Items.Add( Item.Create( 1 ) );
            Assert.True( collection.IsChanged );

            collection[ 0 ].Items.AcceptChanges();
            Assert.False( collection.IsChanged );
        }

        [Fact]
        public void GetChangedItems()
        {
            var collection = new TrackableDictionary<int, Item>( x => x.Key );

            collection.BeginInit();
            collection.Add( Item.Create( 5, "Test" ) );
            collection.EndInit();

            collection.RemoveKey( 5 );
            collection.Add( Item.Create( 5, "Test 123" ) );

            var item = collection.GetValue( 5 );
            item.Value = "Test";

            Assert.False( collection.IsChanged );
            Assert.Empty( collection.GetChangedItems() );

            item.Number = 10;

            Assert.True( collection.IsChanged );

            var changedItem = Assert.Single( collection.GetChangedItems() );

            Assert.True( changedItem.IsPropertyChanged( "Number" ) );
            Assert.False( changedItem.IsPropertyChanged( "Value" ) );
        }

        [Fact]
        public void InitializeWithCtor()
        {
            var dictionary = new TrackableDictionary<int, Item>( x => x.Key, values:
            [
                Item.Create( 1 ),
                Item.Create( 2 )
            ] );

            Assert.False( dictionary.IsChanged );
            Assert.True( dictionary.ContainsKey( 1 ) );
            Assert.True( dictionary.ContainsKey( 2 ) );
            Assert.Contains( Item.Create( 1 ), dictionary, TrackableEqualityComparer<Item>.Default );
        }

        [Fact]
        public void ResetWithInitialize()
        {
            var dictionary = new TrackableDictionary<int, Item>( x => x.Key, values:
            [
                Item.Create( 1 ),
                Item.Create( 2 )
            ] );

            dictionary.Clear();
            Assert.True( dictionary.IsChanged );

            dictionary.Reset( [ Item.Create( 3 ) ], initialize: true );

            Assert.False( dictionary.IsChanged );
            Assert.Equal( [ Item.Create( 3 ) ], dictionary, TrackableEqualityComparer<Item>.Default );

            dictionary.Clear();
            Assert.True( dictionary.IsChanged );

            dictionary.RejectChanges();
            Assert.Equal( [ Item.Create( 3 ) ], dictionary, TrackableEqualityComparer<Item>.Default );
        }

        [Fact]
        public void ValueChanged()
        {
            var dictionary = new TrackableDictionary<int, Item>( x => x.Key );

            dictionary.BeginInit();
            dictionary.Add( Item.Create( key: 1 ) );
            dictionary.Add( Item.Create( key: 2 ) );
            dictionary.Add( Item.Create( key: 3 ) );
            dictionary.EndInit();

            Assert.False( dictionary.IsValueChanged( key: 1 ) );
            Assert.False( dictionary.IsValueChanged( key: 2 ) );

            dictionary.RemoveKey( key: 1 );
            Assert.True( dictionary.IsValueChanged( key: 1 ) );
            Assert.False( dictionary.IsValueChanged( key: 2 ) );
            dictionary.AcceptChanges();

            dictionary.GetValue( key: 2 ).Number = 13;
            Assert.True( dictionary.IsValueChanged( key: 2 ) );
            Assert.False( dictionary.IsValueChanged( key: 3 ) );
        }

        [Fact]
        public void AddOrUpdateShouldUpdateOriginalItem()
        {
            var dictionary = new TrackableDictionary<int, Item>( x => x.Key )
            {
                Item.Create( 1, "Test" )
            };
            dictionary.AcceptChanges();

            dictionary.AddOrUpdate( Item.Create( 1, "Test", x => x.NonTrackableString = "Hello" ) );
            Assert.False( dictionary.IsChanged );

            dictionary.RemoveKey( 1 );
            Assert.True( dictionary.IsChanged );

            Assert.True( dictionary.TryGetChange( 1, out var change ) );
            Assert.Equal( "Hello", change.Item.NonTrackableString );
        }

        [Fact]
        public void AddOrUpdateOriginalShouldMarkCollectionAsUnchanged()
        {
            var dictionary = new TrackableDictionary<int, Item>( x => x.Key )
            {
                Item.Create( 1, "Test" )
            };
            Assert.True( dictionary.IsChanged );

            dictionary.AddOrUpdateOriginal( Item.Create( 1, "Test" ) );
            Assert.False( dictionary.IsChanged );
        }

        [Fact]
        public void RemoveDuringInitializeShouldNotBeConsiderAsChange()
        {
            var dictionary = new TrackableDictionary<int, Item>( x => x.Key );

            dictionary.BeginInit();
            dictionary.Add( Item.Create( 1 ) );
            dictionary.EndInit();

            Assert.False( dictionary.IsChanged );

            dictionary.Add( Item.Create( 2 ) );
            Assert.True( dictionary.IsChanged );

            dictionary.BeginInit();
            dictionary.RemoveKey( 1 );
            dictionary.EndInit();

            Assert.False( dictionary.TryGetChange( 1, out var _ ) );
        }

        [Fact]
        public void SuppressItemTracking()
        {
            var dictionary = new TrackableDictionary<int, Item>( x => x.Key, supressItemTracking: true );

            dictionary.BeginInit();
            dictionary.Add( Item.Create( 1 ) );
            dictionary.EndInit();

            Assert.False( dictionary.IsChanged );

            dictionary[ 0 ].Number = 888;
            Assert.True( dictionary[ 0 ].IsChanged );
            Assert.False( dictionary.IsChanged );

            Assert.Empty( dictionary.GetChangedItems() );
        }

        [Fact]
        public void ResetWithDuplicateItemsShouldThrow()
        {
            var dictionary = new TrackableDictionary<int, string>( x => x.Length );

            Assert.Throws<ArgumentException>( () =>
            {
                dictionary.Reset( [ "1", "1", "2" ] );
            } );
        }

        public abstract class Item : Trackable
        {
            public static Item Create( int key, string value = null, Action<Item> builder = null )
                => Trackable<Item>.Create( (key, value), builder );

            public Item( int key, string value )
            {
                Key = key;
                Value = value;
            }

            public int Key { get; }

            [TrackableProperty]
            public virtual string Value { get; set; }

            [TrackableProperty]
            public virtual int Number { get; set; }

            [TrackableProperty]
            public TrackableDictionary<int, Item> Items { get; } = new TrackableDictionary<int, Item>( x => x.Key );

            [TrackableProperty]
            protected virtual int ProtectedNumber { get; set; }

            public string NonTrackableString { get; set; }
        }
    }
}
