using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ObservableModel
{
    public class DescendingTests
    {
        [Fact]
        public void NullsShouldAlwaysBeLast()
        {
            var collection = new List<string>
            {
                null,
                "1",
                "2",
                null,
                "3",
                null
            };

            Assert.Equal( [ "3", "2", "1", null, null, null ], collection.OrderBy( x => x.Descending() ) );
        }
    }
}
