using System;
using Xunit;

namespace ObservableModel
{
    public class TrackableErrorTests
    {
        [Fact]
        public void NonVirtualPropertyError()
        {
            Assert.Throws<TypeLoadException>( () => Trackable<NonVirtualProperty>.Create() );
        }



        public class NonVirtualProperty : Trackable
        {
            [TrackableProperty]
            public int Test { get; set; }
        }
    }
}
