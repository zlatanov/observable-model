using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Xunit;

namespace ObservableModel
{
    public class TrackableTests
    {
        [Fact]
        public void Tracked()
        {
            Person person = new Untracked();
            Assert.False( Trackable.IsTracked( person ) );

            person = Person.Create();
            Assert.True( Trackable.IsTracked( person ) );
            Assert.NotEqual( DateTime.MinValue, person.UntrackedTime );
            Assert.Equal( person.Id, person.GetOriginalValue<int>( "Id" ) );
        }


        [Fact]
        public void OriginalEquals()
        {
            var person = Person.Create( name: "Monika" );
            Assert.True( person.OriginalEquals( person ) );

            person.Name = "Ivan";
            Assert.False( person.OriginalEquals( person ) );
        }


        [Fact]
        public void OverrideTrackableProperty()
        {
            var person = PersonDerived.Create( name: "Monika" );

            Assert.Throws<ArgumentException>( () => person.GetOriginalValue<string>( "Name" ) );
            var propertyChanges = new List<string>();
            person.PropertyChanges.Subscribe( x => propertyChanges.Add( x.PropertyName ) );
            person.Name = "Ivan";
            Assert.False( person.IsChanged );
            Assert.True( person.OriginalEquals( person ) );
            Assert.Single( propertyChanges, "Name" );
        }


        [Fact]
        public void ChangeAndRestoreProperty()
        {
            var person = Person.Create( name: "Marry" );

            Assert.Equal( "Trackable<Person>", person.GetType().Name );
            Assert.False( person.IsChanged );

            person.Name = "John";
            Assert.Equal( "John", person.Name );
            Assert.True( person.IsChanged );
            Assert.True( person.IsPropertyChanged( "Name" ) );

            person.Name = "Marry";
            Assert.False( person.IsChanged );
        }


        [Fact]
        public void AcceptChanges()
        {
            var person = Person.Create();

            person.Name = "John";
            person.Age = 30;

            Assert.True( person.IsChanged );
            person.AcceptChanges();

            Assert.False( person.IsChanged );
        }


        [Fact]
        public void RejectChanges()
        {
            var person = Person.Create( name: "John", age: 30 );
            var motherChanges = new List<int?>();

            person.Observe( x => x.MotherId ).Subscribe( motherChanges.Add );
            Assert.False( person.IsChanged );

            person.Name = "Test";
            person.Age = 14;
            person.Mother = Person.Create( id: 19 );

            Assert.True( person.IsChanged );
            Assert.True( person.IsPropertyChanged( "Name" ) );
            Assert.True( person.IsPropertyChanged( "Age" ) );
            Assert.Single( motherChanges, 19 );

            motherChanges.Clear();
            person.RejectChanges();

            Assert.False( person.IsChanged );
            Assert.Equal( "John", person.Name );
            Assert.Equal( 30, person.Age );

            Assert.Single( motherChanges, new int?() );
        }


        [Fact]
        public void EqualsDifferentInstances()
        {
            var person1 = Person.Create( name: "Random Name", age: 42 );
            var person2 = Person.Create( name: "Random Name", age: 42 );
            var person3 = Person.Create( name: "Not Random Name", age: 42 );

            Assert.NotEqual( person1, person3, TrackableEqualityComparer<Person>.Default );
            Assert.NotEqual( person2, person3, TrackableEqualityComparer<Person>.Default );
            Assert.Equal( person1, person2, TrackableEqualityComparer<Person>.Default );
        }


        [Fact]
        public void OriginalValues()
        {
            var person = Person.Create( name: "Ivan", age: 33 );

            person.Name = "Not Ivan";
            person.Age = 34;

            Assert.Equal( "Ivan", person.GetOriginalValue<string>( nameof( Person.Name ) ) );
            Assert.Equal( 33, person.GetOriginalValue<int>( nameof( Person.Age ) ) );

            Assert.Throws<ArgumentException>( () => person.GetOriginalValue<string>( "Missing" ) );
            Assert.Throws<InvalidCastException>( () => person.GetOriginalValue<int>( nameof( Person.Name ) ) );

            person.AcceptChanges();

            Assert.Equal( "Not Ivan", person.GetOriginalValue<string>( nameof( Person.Name ) ) );
            Assert.Equal( 34, person.GetOriginalValue<int>( nameof( Person.Age ) ) );
        }


        [Fact]
        public void SetOriginalValueShouldSetCurrentIfNotChanged()
        {
            var person = Person.Create( name: "Test" );
            person.SetOriginalValue( nameof( Person.Name ), "123" );

            Assert.False( person.IsChanged );
            Assert.Equal( "123", person.Name );
            Assert.Equal( "123", person.GetOriginalValue<string>( nameof( Person.Name ) ) );
        }


        [Fact]
        public void OriginalComparisons()
        {
            var person1 = Person.Create( name: "Ivan", age: 33 );

            person1.Name = "Not Ivan";
            person1.Age = 34;

            var person2 = Person.Create( name: "Ivan", age: 33 );

            Assert.NotEqual( person1, person2 );
            Assert.True( person1.OriginalEquals( person2 ) );
            Assert.False( person2.OriginalEquals( person1 ) );
        }


        [Fact]
        public void NestedTrackableObjects()
        {
            var person = Person.Create( name: "Monika", age: 36 );

            Assert.False( person.IsChanged );

            person.Age++;

            Assert.True( person.IsChanged );
            Assert.Equal( 37, person.Age );
            Assert.Equal( 36, person.GetOriginalValue<int>( nameof( Person.Age ) ) );

            person.RejectChanges();
            Assert.False( person.IsChanged );
            Assert.Equal( 36, person.Age );
            Assert.Equal( 36, person.GetOriginalValue<int>( nameof( Person.Age ) ) );

            person.Mother = Person.Create( name: "Natalia" );

            Assert.True( person.IsChanged );

            person.AcceptChanges();
            Assert.False( person.IsChanged );

            person.Mother.Age = 60;
            Assert.True( person.IsChanged );
            Assert.True( person.Mother.IsChanged );

            person.Mother.AcceptChanges();
            Assert.False( person.IsChanged );
            Assert.False( person.Mother.IsChanged );

            person.Mother.Age = 61;
            Assert.True( person.IsChanged );
            Assert.True( person.Mother.IsChanged );

            person.AcceptChanges();
            Assert.False( person.IsChanged );
            Assert.False( person.Mother.IsChanged );

            person.Mother.Age = 65;
            Assert.True( person.IsChanged );
            person.Mother.SetOriginalValue( "Age", 65 );
            Assert.False( person.IsChanged );
        }


        [Fact]
        public void ChangingNestedTrackableObjectDuringInitialization()
        {
            var person = Person.Create( name: "Ivan" );

            person.BeginInit();
            person.Name = "Ivan";
            person.Mother = Person.Create( name: "Ivan's Mother" );
            person.EndInit();
            person.Mother.BeginInit();
            person.Mother.Age = 55;
            person.Mother.EndInit();

            Assert.False( person.IsChanged );
            Assert.False( person.Mother.IsChanged );
        }


        [Fact]
        public void NestedCollections()
        {
            var person = Person.Create();

            Assert.False( person.IsChanged );

            person.Relatives.Add( Person.Create() );

            Assert.True( person.IsChanged );
            Assert.True( person.Relatives.IsChanged );

            person.AcceptChanges();

            Assert.False( person.IsChanged );
            Assert.False( person.Relatives.IsChanged );
        }


        [Fact]
        public void NonTrackablePropertyShouldRaisePropertyChanged()
        {
            var person = Person.Create();
            var changes = new List<string>();

            person.PropertyChanges.Subscribe( x => changes.Add( x.PropertyName ) );
            person.UntrackedTime = person.UntrackedTime.AddSeconds( 1 );

            Assert.False( person.IsChanged );
            Assert.Single( changes );
            Assert.Equal( nameof( Person.UntrackedTime ), changes[ 0 ] );
        }


        [Fact]
        public void SimpleChange()
        {
            var one = Person.Create( 1, "Test" );
            Assert.False( one.IsChanged );

            one.Name = "Not a test";

            Assert.True( one.IsChanged );
        }


        [Fact]
        public void SimpleChangeThenReturn()
        {
            var one = Person.Create( 1, "Test" );

            one.Name = "Not a test";
            one.Name = "Test";

            Assert.False( one.IsChanged );
        }


        [Fact]
        public void SimpleChangeInCollection()
        {
            var list = new TrackableList<Person>();

            list.BeginInit();
            list.Add( Person.Create( 1, "Test" ) );
            list.EndInit();
            list[ 0 ].Name = "Not a test";

            Assert.True( list.IsChanged );
            Assert.True( list[ 0 ].IsChanged );
        }


        [Fact]
        public void SimpleChangeThenReturnInCollection()
        {
            var list = new TrackableList<Person>();

            list.BeginInit();
            list.Add( Person.Create( 1, "Test" ) );
            list.EndInit();
            list[ 0 ].Name = "Not a test";
            list[ 0 ].Name = "Test";

            Assert.False( list.IsChanged );
            Assert.False( list[ 0 ].IsChanged );
        }


        [Fact]
        public void SimpleChangeBeforeAddInCollection()
        {
            var list = new TrackableList<Person>();
            var one = Person.Create( 1, "Test" );
            one.Name = "Not a test";

            list.BeginInit();
            list.Add( one );
            list.EndInit();

            Assert.True( one.IsChanged );
            Assert.True( list.IsChanged );
        }


        [Fact]
        public void SimpleChangeBeforeInitValue()
        {
            var target = Person.Create( 1, "Target" );
            target.Name = "Not a Target";

            var person = Trackable<Person>.Create( 2, x =>
            {
                x.Name = "Parent";
                x.Target = target;
            } );

            Assert.True( target.IsChanged );
            Assert.True( person.IsChanged );
        }


        [Fact]
        public void CheckUnsubscribeWorks()
        {
            var p1 = Person.Create( 1, "Target" );
            var p2 = Person.Create( 2, "Target 2" );

            p1.Target = p2;
            p1.Target = null;
            Assert.False( p1.IsChanged );

            p2.Name = "Target 2 Changed";
            Assert.False( p1.IsChanged );
            Assert.True( p2.IsChanged );
        }


        [Fact]
        public void CollectionSort()
        {
            var list = new TrackableList<Person>()
            {
                Person.Create( 1, "Test", 20 ),
                Person.Create( 2, "Test", 50 ),
                Person.Create( 3, "Test", 10 )
            };
            Assert.True( list.IsChanged );

            list.AcceptChanges();
            Assert.False( list.IsChanged );

            list.SortBy( x => x.Age );
            Assert.True( list.IsChanged );
            list.AcceptChanges();

            list.SortBy( x => x.Age );
            Assert.False( list.IsChanged );
        }


        [Fact]
        public void Dictionary()
        {
            var dictionary = new TrackableDictionary<int, Person>( x => x.Id )
            {
                Person.Create( 1, "Test", 20 ),
                Person.Create( 2, "Test", 50 )
            };
            Assert.True( dictionary.IsChanged );
            Assert.False( dictionary[ 0 ].IsChanged );
            Assert.False( dictionary[ 1 ].IsChanged );

            dictionary.AcceptChanges();
            Assert.False( dictionary.IsChanged );
            Assert.False( dictionary[ 0 ].IsChanged );
            Assert.False( dictionary[ 1 ].IsChanged );

            dictionary[ 0 ] = Person.Create( 3, "12345", 50 );
            Assert.True( dictionary.IsChanged );
            Assert.False( dictionary[ 0 ].IsChanged );
            Assert.False( dictionary[ 1 ].IsChanged );

            dictionary.AcceptChanges();
            dictionary.SortBy( x => x.Age );
            Assert.False( dictionary.IsChanged );
        }


        [Fact]
        public void TwoPropertiesSameValue()
        {
            var ivan = Person.Create( 1, "Ivan", 31 );
            var parent = Person.Create( 2, "Ivan's Mom", 55 );

            parent.Target = ivan;
            parent.AcceptChanges();

            parent.Target2 = ivan;
            ivan.Name = "Ivan 1";
            Assert.True( parent.IsChanged );

            parent.Target2 = null;

            Assert.True( ivan.IsChanged );
            Assert.True( parent.IsChanged );
        }


        [Fact]
        public void DoubleChange()
        {
            var ivan = Person.Create( 1, "Ivan", 31 );
            var parent = Person.Create( 2, "Ivan's Mom", 55 );

            parent.Target = ivan;
            ivan.Name = "Ivan 1";
            ivan.Name = "Ivan 12";
            Assert.True( parent.IsChanged );

            parent.Target2 = null;

            Assert.True( ivan.IsChanged );
            Assert.True( parent.IsChanged );
        }


        [Fact]
        public void MultiLevelTest()
        {
            var person = Person.Create( 1, "John Wick", 33 );
            person.Target = Person.Create( 2, "Evlampi", 300 );
            person.Target.Target = Person.Create( 3, "Duster", 35 );
            person.Target.Target.Target = Person.Create( 4, "Unknown", -100 );
            person.People =
            [
                Person.Create( 5, "Me", 14 )
            ];

            Assert.True( person.IsChanged );
            Assert.True( person.Target.IsChanged );
            Assert.True( person.Target.Target.IsChanged );
            Assert.False( person.Target.Target.Target.IsChanged );
            Assert.True( person.People.IsChanged );
            Assert.False( person.People[ 0 ].IsChanged );

            person.AcceptChanges();

            Assert.False( person.IsChanged );
            Assert.False( person.Target.IsChanged );
            Assert.False( person.Target.Target.IsChanged );
            Assert.False( person.Target.Target.Target.IsChanged );
            Assert.False( person.People.IsChanged );
            Assert.False( person.People[ 0 ].IsChanged );

            person.Target.UntrackedTime = DateTime.UtcNow;
            person.Target.Target.Target.UntrackedTime = DateTime.UtcNow;
            person.People[ 0 ].UntrackedTime = DateTime.UtcNow;

            Assert.False( person.IsChanged );
            Assert.False( person.Target.IsChanged );
            Assert.False( person.Target.Target.IsChanged );
            Assert.False( person.Target.Target.Target.IsChanged );
            Assert.False( person.People.IsChanged );
            Assert.False( person.People[ 0 ].IsChanged );
        }


        [Fact]
        public void NestedChangedPropertyWhileInitializing()
        {
            var person = Person.Create( builder: x =>
            {
                var target = Person.Create();
                target.Name = "Changed Target";
                Assert.True( target.IsChanged );

                x.Target = target;
            } );

            Assert.True( person.IsChanged );
        }


        [Fact]
        public void MoreThan3ParametersInConstructor()
        {
            var guid = Guid.NewGuid();
            var obj = Trackable<ManyParametersInCtor>.Create( (1, "2", 3M, guid) );

            Assert.Equal( 1, obj.P1 );
            Assert.Equal( "2", obj.P2 );
            Assert.Equal( 3M, obj.P3 );
            Assert.Equal( guid, obj.P4 );
        }


        [Fact]
        public void SetOriginalForObjectWithManyProperties()
        {
            var obj = Trackable<TrackableWithManyProperties>.Create();

            obj.SetOriginalValue( x => x.Length, 1 );
            obj.SetOriginalValue( x => x.Name, "Name" );
            obj.SetOriginalValue( x => x.Guid, Guid.NewGuid() );
            obj.SetOriginalValue( x => x.Decimal, 5 );
            obj.SetOriginalValue( x => x.Person, Person.Create() );
            obj.SetOriginalValue( x => x.Object, Person.Create() );
            obj.SetOriginalValue( x => x.Double, 2 );
            obj.SetOriginalValue( x => x.Byte, (byte)255 );

            Assert.False( obj.IsChanged );
        }


        [Fact]
        public void TrackableWithReadonlyStruct()
        {
            var person = Person.Create( name: "Monika" );

            var employee = Employee.Create( person, "+359888966414" );

            Assert.False( employee.IsChanged );

            employee.Salary = 50;
            Assert.True( employee.IsChanged );
        }


        [Fact]
        public void ResetValue()
        {
            var person = Person.Create();
            var propertyChanges = new List<string>();

            person.PropertyChanges.Subscribe( x =>
            {
                propertyChanges.Add( x.PropertyName );
            } );
            person.ResetValue( nameof( Person.Name ), "Test" );
            Assert.False( person.IsChanged );
            Assert.Equal( [ "Name" ], propertyChanges );

            person.Name = "Changed";
            Assert.True( person.IsChanged );

            propertyChanges.Clear();
            person.ResetValue( nameof( Person.Name ), "Test" );
            Assert.False( person.IsChanged );
            Assert.Equal( [ "IsChanged", "Name" ], propertyChanges );

            person.ResetValue( nameof( Person.People ), new TrackableList<Person>( [ Person.Create() ] ) );
            Assert.False( person.IsChanged );

            person.People[ 0 ].Age += 1;
            Assert.True( person.IsChanged );
        }


        [Fact]
        public void GetChanges()
        {
            var person = Person.Create( name: "1", age: 20 );

            person.Name = "2";
            person.Age = 30;

            var changes = person.GetChanges();

            Assert.Equal( 2, changes.Length );
            Assert.Equal( "Name", changes[ 0 ].PropertyName );
            Assert.Equal( "1", changes[ 0 ].OriginalValue );
            Assert.Equal( "2", changes[ 0 ].Value );

            Assert.Equal( "Age", changes[ 1 ].PropertyName );
            Assert.Equal( 20, changes[ 1 ].OriginalValue );
            Assert.Equal( 30, changes[ 1 ].Value );

            changes = person.GetChanges( Person.Create( name: "2", age: 40 ) );
            Assert.Single( changes );
            Assert.Equal( "Age", changes[ 0 ].PropertyName );
            Assert.Equal( 40, changes[ 0 ].OriginalValue );
            Assert.Equal( 30, changes[ 0 ].Value );

            changes = person.GetChanges( Person.Create( name: "2", age: 30 ) );
            Assert.Empty( changes );
        }


        class Untracked : Person
        {
            public Untracked() : base( 1 ) { }
        }


        public abstract class ManyParametersInCtor : Trackable
        {
            protected ManyParametersInCtor( int p1, string p2, decimal p3, Guid p4 )
            {
                P1 = p1;
                P2 = p2;
                P3 = p3;
                P4 = p4;
            }


            public int P1 { get; }
            public string P2 { get; }
            public decimal P3 { get; }
            public Guid P4 { get; }
        }


        public abstract class TrackableWithManyProperties : Trackable
        {
            [TrackableProperty]
            public virtual int Length { get; set; }

            [TrackableProperty]
            public virtual string Name { get; set; }

            [TrackableProperty]
            public virtual Guid Guid { get; set; }

            [TrackableProperty]
            public virtual decimal Decimal { get; set; }

            [TrackableProperty]
            public virtual Person Person { get; set; }

            [TrackableProperty]
            public virtual object Object { get; set; }

            [TrackableProperty]
            public virtual double Double { get; set; }

            [TrackableProperty]
            public virtual byte Byte { get; set; }

            public void SetOriginalValue<T>( Expression<Func<TrackableWithManyProperties, T>> property, T value )
            {
                var propertyName = ( (MemberExpression)property.Body ).Member.Name;

                SetOriginalValue( propertyName, value );
            }
        }
    }
}
