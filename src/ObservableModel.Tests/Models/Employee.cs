using System;

namespace ObservableModel
{
    public abstract class Employee : Trackable
    {
        public static Employee Create( Person person, string phone, Action<Employee> builder = null ) => Trackable<Employee>.Create( new PersonKey( person, phone ), x =>
        {
            builder?.Invoke( x );
        } );


        protected Employee()
        {
        }


        protected Employee( PersonKey key ) => Key = key;


        public PersonKey Key { get; }


        [TrackableProperty]
        public virtual decimal Salary { get; set; }
    }
}
