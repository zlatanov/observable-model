using Xunit;

namespace ObservableModel
{
    public class ObjectConstructorTests
    {
        [Fact]
        public void NoArgs()
        {
            var instance = ObjectConstructor.Create( typeof( ClassNoArgs ) );

            Assert.IsType<ClassNoArgs>( instance );
        }



        [Fact]
        public void OneArg()
        {
            var instance = Assert.IsType<Class1Arg<string>>( ObjectConstructor.Create( typeof( Class1Arg<string> ), "Testing" ) );

            Assert.Equal( "Testing", instance.Arg1 );
        }


        [Fact]
        public void EightArgs()
        {
            Assert.IsType<Class1Arg<string>>( ObjectConstructor.Create( typeof( Class1Arg<string> ), ("1", 2M, 3, 4, 5, 6, 7, (int?)8) ) );
        }


        class ClassNoArgs
        {
        }

        class Class1Arg<T>
        {
            public Class1Arg( T arg1 )
            {
                Arg1 = arg1;
            }


            public Class1Arg( string a1, decimal a2, int a3, int a4, int a5, int a6, int a7, int? a8 )
            {
            }


            public T Arg1 { get; }
        }
    }
}
