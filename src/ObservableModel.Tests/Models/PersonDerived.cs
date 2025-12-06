using System;

namespace ObservableModel
{
    public abstract class PersonDerived : Person
    {
        public static new PersonDerived Create( int id = 1, string name = "John Doe", int age = 33, Action<Person> builder = null ) => Trackable<PersonDerived>.Create( id, x =>
       {
           x.Name = name;
           x.Age = age;

           builder?.Invoke( x );
       } );


        protected PersonDerived( int id ) : base( id ) { }


        [ObservableProperty]
        public override string Name { get; set; }
    }
}
