using Xunit;

namespace ObservableModel
{
    public class ObservableDictionaryTests
    {
        [Fact]
        public void Sorting()
        {
            var dictionary = new ObservableDictionary<char, string>( x => x[ 0 ] )
            {
                "a", "b", "c"
            };

            Assert.Equal( 0, dictionary.IndexOfKey( 'a' ) );
            Assert.Equal( 1, dictionary.IndexOfKey( 'b' ) );
            Assert.Equal( 2, dictionary.IndexOfKey( 'c' ) );

            dictionary.SortBy( x => x.Descending() );

            Assert.Equal( 0, dictionary.IndexOfKey( 'c' ) );
            Assert.Equal( 1, dictionary.IndexOfKey( 'b' ) );
            Assert.Equal( 2, dictionary.IndexOfKey( 'a' ) );
        }


        [Fact]
        public void Remove()
        {
            var dictionary = new ObservableDictionary<char, string>( x => x[ 0 ] )
            {
                "a", "b", "c", "d"
            };

            dictionary.RemoveKey( 'c' );

            Assert.DoesNotContain( "c", dictionary );
            Assert.Equal( 2, dictionary.IndexOfKey( 'd' ) );
            Assert.Equal( 1, dictionary.IndexOfKey( 'b' ) );
            Assert.Equal( 0, dictionary.IndexOfKey( 'a' ) );
        }


        [Fact]
        public void Insert()
        {
            var dictionary = new ObservableDictionary<char, string>( x => x[ 0 ] )
            {
                "a", "c", "d"
            };
            dictionary.Insert( 1, "b" );

            Assert.Equal( 0, dictionary.IndexOfKey( 'a' ) );
            Assert.Equal( 1, dictionary.IndexOfKey( 'b' ) );
            Assert.Equal( 2, dictionary.IndexOfKey( 'c' ) );
            Assert.Equal( 3, dictionary.IndexOfKey( 'd' ) );
        }


        [Fact]
        public void AddOrUpdate()
        {
            var dictionary = new ObservableDictionary<char, string>( x => x[ 0 ] )
            {
                "albinos", "barracuda", "curry"
            };

            dictionary.AddOrUpdate( "animal" );
            Assert.Equal( "animal", dictionary[ 0 ] );
            dictionary.AddOrUpdate( "animal" );

            dictionary.AddOrUpdate( "barry" );
            Assert.Equal( "barry", dictionary[ 1 ] );
            dictionary.AddOrUpdate( "barry" );

            dictionary.AddOrUpdate( "cinema" );
            Assert.Equal( "cinema", dictionary[ 2 ] );
            dictionary.AddOrUpdate( "cinema" );
        }
    }
}
