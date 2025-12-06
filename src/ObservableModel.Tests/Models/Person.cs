using System;

namespace ObservableModel
{
    public abstract class Person : Trackable, IAgeFeature
    {
        public static Person Create( int id = 1, string name = "John Doe", int age = 33, Action<Person> builder = null ) => Trackable<Person>.Create( id, x =>
        {
            x.Name = name;
            x.Age = age;

            builder?.Invoke( x );
        } );


        protected Person( int id ) => Id = id;


        [TrackableProperty]
        public int Id { get; }


        [TrackableProperty]
        public virtual string Name { get; set; }


        [TrackableProperty]
        public virtual int Age { get; set; }


        [ObservableProperty]
        public virtual DateTime UntrackedTime { get; set; } = DateTime.UtcNow;


        [TrackableProperty]
        public virtual Person Target { get; set; }


        [TrackableProperty]
        public virtual Person Target2 { get; set; }


        [TrackableProperty]
        public virtual TrackableList<Person> People { get; set; }


        [TrackableProperty]
        public virtual Person Mother { get; set; }


        [ObservablePropertyDependency( nameof( Mother ) )]
        public int? MotherId => Mother?.Id;


        [TrackableProperty]
        public TrackableList<Person> Relatives { get; } = [];
    }
}
