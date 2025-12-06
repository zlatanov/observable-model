using System.Collections.Generic;
using System.Linq;
using ObservableModel.Subjects;
using Xunit;

namespace ObservableModel.Linq
{
    public class ObservableTests
    {
        [Fact]
        public void CreateShouldProduce()
        {
            var created = false;
            var observable = Observable.Create<int>( x =>
            {
                created = true;

                x.OnNext( 1 );
                x.OnNext( 3 );
                x.OnCompleted();

                return Disposable.Empty;
            } );
            Assert.False( created );

            var sum = 0;
            var completed = false;

            observable.Subscribe( x =>
            {
                sum += x;
            }, completed: () => completed = true );

            Assert.True( created );
            Assert.True( completed );
            Assert.Equal( 4, sum );
        }

        [Fact]
        public void ObserveNewItems()
        {
            var collection = new ObservableList<string>();
            var changes = new List<string[]>();

            collection.ObserveNewItems().Subscribe( x => changes.Add( x.ToArray() ) );
            collection.Add( "1" );
            AssertEqual( "1" );

            collection.RemoveAt( 0 );
            Assert.Empty( changes );

            collection.Reset( [ "a", "b", "c" ] );
            AssertEqual( "a", "b", "c" );

            collection[ 1 ] = "d";
            AssertEqual( "d" );

            collection.Insert( 3, "e" );
            AssertEqual( "e" );

            collection.SortBy( x => x.Descending() );
            Assert.Empty( changes );

            collection.Clear();
            Assert.Empty( changes );

            void AssertEqual( params string[] item )
            {
                Assert.Single( changes );
                Assert.Equal( item, changes[ 0 ] );

                changes.Clear();
            }
        }

        [Fact]
        public void ObserveNewItemsWithInitializingTrackable()
        {
            var collection = new TrackableList<string>();
            var changesNotInitializing = new List<string[]>();

            collection.ObserveNewItems().Subscribe( x =>
            {
                if ( !x.IsInitializing )
                {
                    changesNotInitializing.Add( x.ToArray() );
                }
            } );

            collection.Reset( [ "a", "b", "c" ], initialize: false );
            AssertEqual( "a", "b", "c" );

            collection.Reset( [ "a", "b", "c" ], initialize: true );
            Assert.Empty( changesNotInitializing );

            collection.Add( "1" );
            AssertEqual( "1" );

            collection.BeginInit();
            collection.Add( "2" );
            collection.EndInit();
            Assert.Empty( changesNotInitializing );

            collection.Add( "3" );
            AssertEqual( "3" );

            void AssertEqual( params string[] item )
            {
                Assert.Single( changesNotInitializing );
                Assert.Equal( item, changesNotInitializing[ 0 ] );

                changesNotInitializing.Clear();
            }
        }

        [Fact]
        public void ObserveNewItemsWithInitializingNonTrackable()
        {
            var collection = new ObservableList<string>();
            var changesNotInitializing = new List<string[]>();

            collection.ObserveNewItems().Subscribe( x =>
            {
                if ( !x.IsInitializing )
                {
                    changesNotInitializing.Add( x.ToArray() );
                }
            } );

            collection.Reset( [ "a", "b", "c" ] );
            Assert.Empty( changesNotInitializing );

            collection.Add( "1" );
            AssertEqual( "1" );

            void AssertEqual( params string[] item )
            {
                Assert.Single( changesNotInitializing );
                Assert.Equal( item, changesNotInitializing[ 0 ] );

                changesNotInitializing.Clear();
            }
        }

        [Fact]
        public void Map()
        {
            var list = new ObservableList<Person>();
            var ages = list.Map( x => x.Age );
            AssertEqual();

            list.Add( Person.Create( age: 1 ) );
            AssertEqual();

            list.Add( Person.Create( age: 2 ) );
            AssertEqual();

            list.SortBy( x => x.Age.Descending() );
            AssertEqual();

            list.RemoveAt( 0 );
            AssertEqual();

            list.Clear();
            AssertEqual();

            void AssertEqual() => Assert.Equal( list.Select( x => x.Age ), ages );
        }

        [Fact]
        public void CombineLatest()
        {
            BehaviorSubject<int> s1 = new( 1 );
            BehaviorSubject<int> s2 = new( 2 );

            int sum = 0;
            Observable.CombineLatest( s1, s2, ( x, y ) => x + y ).Subscribe( result =>
            {
                sum = result;
            } );

            Assert.Equal( 3, sum );

            s1.OnNext( 2 );
            Assert.Equal( 4, sum );

            s2.OnNext( 4 );
            Assert.Equal( 6, sum );
        }
    }
}
