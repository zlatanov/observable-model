using System;
using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace ObservableModel
{
    public class ObserveTests
    {
        [Fact]
        public void ObserveShouldNotFailWhenDuplicatePropertiesExist()
        {
            var dog = Observable<Dog>.Create();
            var observable = dog.Observe( x => x.Name ).ToProperty();
            var name = "Barky";

            dog.Name = name;

            Assert.Equal( name, observable.Value );
        }


        [Fact]
        public void ObserveShouldNotBeRaisedWhenSubscribingDuringNotification()
        {
            var dog = Observable<Dog>.Create();
            var observable = dog.Observe( x => x.Name );
            var raised = 0;
            var subscribe = true;

            observable.Skip( 1 ).Subscribe( x =>
            {
                Interlocked.Increment( ref raised );

                if ( subscribe )
                {
                    subscribe = false;
                    observable.Skip( 1 ).Subscribe( _ =>
                    {
                        Interlocked.Increment( ref raised );
                    } );
                }
            } );

            dog.Name = "Sharky";
            Assert.Equal( 1, raised );

            raised = 0;
            dog.Name = "Barky";
            Assert.Equal( 2, raised );
        }


        [Fact]
        public void SubscribeUnsubscribeMultipleTimes()
        {
            var dog = Observable<Dog>.Create();
            var observable = dog.Observe( x => x.Name );
            var disposables = new List<IDisposable>();
            var raised = 0;
            var rng = new Random();

            for ( var times = 0; times < 10; ++times )
            {
                for ( var i = 0; i < 10; ++i )
                {
                    disposables.Add( observable.Skip( 1 ).Subscribe( x =>
                    {
                        Interlocked.Increment( ref raised );
                    } ) );
                }

                raised = 0;
                dog.Name = "Sharky";
                Assert.Equal( disposables.Count, raised );

                for ( int disposableIndex = 0; disposableIndex < disposables.Count; )
                {
                    if ( rng.Next( 2 ) == 1 )
                    {
                        disposables[ disposableIndex ].Dispose();
                        disposables.RemoveAt( disposableIndex );
                    }
                    else
                    {
                        disposableIndex += 1;
                    }
                }

                raised = 0;
                dog.Name = "Barky";
                Assert.Equal( disposables.Count, raised );
            }
        }


        [Fact]
        public void Defer()
        {
            var dog = Observable<Dog>.Create();
            var changeCount = 0;

            dog.PropertyChanges.Subscribe( x => changeCount += 1 );

            using ( dog.DeferPropertyChanges() )
            {
                dog.Name = "Johny";
                dog.Name = "Barky";
                dog.Name = "Sparky";

                Assert.Equal( 0, changeCount );
            }

            Assert.Equal( 1, changeCount );
        }
    }
}
