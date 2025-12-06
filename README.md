# ObservableModel

A lightweight .NET library providing observable and trackable object models with support for `INotifyPropertyChanged`, reactive patterns with `IObservable<T>`, and comprehensive change tracking capabilities.

[![NuGet](https://img.shields.io/nuget/v/ObservableModel.svg)](https://www.nuget.org/packages/ObservableModel/)

## Installation

Install via NuGet Package Manager:

```bash
dotnet add package ObservableModel
```

## Features

- **Observable Objects**: Automatic `INotifyPropertyChanged` implementation
- **Change Tracking**: Track changes to properties with original value preservation
- **Reactive Extensions**: Built-in support for `IObservable<T>` patterns
- **Observable Collections**: Feature-rich observable lists with change notifications
- **Property Dependencies**: Automatic dependent property notifications
- **Deferred Changes**: Batch property change notifications
- **MVVM Support**: Perfect for WPF, Avalonia, and other MVVM frameworks

## Quick Start

### Observable Objects

Create objects with automatic property change notifications:

```csharp
public abstract class Person : ObservableObject
{
    [ObservableProperty]
    public virtual string Name { get; set; }
    
    [ObservableProperty]
    public virtual int Age { get; set; }
}

// Create an instance
var person = Observable<Person>.Create(x =>
{
    x.Name = "John Doe";
    x.Age = 30;
});

// Subscribe to property changes
person.PropertyChanged += (sender, e) =>
{
    Console.WriteLine($"Property {e.PropertyName} changed");
};

person.Name = "Jane Doe"; // Triggers PropertyChanged event
```

### Observable Collections

```csharp
var people = new ObservableList<Person>();

// Subscribe to collection changes
people.CollectionChanged += (sender, e) =>
{
    Console.WriteLine($"Collection changed: {e.Action}");
};

people.Add(Observable<Person>.Create(x => 
{
    x.Name = "Alice";
    x.Age = 25;
}));

// Sort with persistent sorting
people.SortBy(x => x.Age, persist: true);

// Aggregate values reactively
var averageAge = people.Aggregate(0.0, (sum, p) => sum + p.Age / people.Count);
```

### Reactive Observables

Use LINQ-style operators with observables:

```csharp
var person = Observable<Person>.Create();

// Create a reactive property
var nameObservable = person.Observe(x => x.Name)
    .DistinctUntilChanged()
    .Select(name => name.ToUpper())
    .ToProperty();

person.Name = "john"; // nameObservable.Value becomes "JOHN"
```

### Change Tracking

Track changes to objects with original value preservation:

```csharp
public abstract class Employee : Trackable
{
    [TrackableProperty]
    public virtual string Name { get; set; }
    
    [TrackableProperty]
    public virtual decimal Salary { get; set; }
}

var employee = Trackable<Employee>.Create(x =>
{
    x.Name = "John";
    x.Salary = 50000m;
});

employee.Salary = 55000m;

Console.WriteLine(employee.IsChanged); // True
Console.WriteLine(employee.GetOriginalValue<decimal>(nameof(Employee.Salary))); // 50000

// Revert changes
employee.RejectChanges();
Console.WriteLine(employee.Salary); // 50000

// Or accept changes
employee.Salary = 60000m;
employee.AcceptChanges();
Console.WriteLine(employee.IsChanged); // False
```

### Trackable Collections

```csharp
var team = new TrackableList<Employee>();

team.Reset(employees, initialize: true);
team[0].Salary = 65000m;

// Get changes
var changes = team.GetChanges();
foreach (var change in changes)
{
    Console.WriteLine($"{change.Type}: {change.Item}");
}

// Revert all changes
team.RejectChanges();
```

### Property Dependencies

Automatically notify dependent properties:

```csharp
public abstract class Person : ObservableObject
{
    [ObservableProperty]
    public virtual string FirstName { get; set; }
    
    [ObservableProperty]
    public virtual string LastName { get; set; }
    
    [ObservablePropertyDependency(nameof(FirstName), nameof(LastName))]
    public string FullName => $"{FirstName} {LastName}";
}

var person = Observable<Person>.Create();
person.PropertyChanged += (s, e) => Console.WriteLine(e.PropertyName);

person.FirstName = "John"; // Triggers: FirstName, FullName
```

### Deferred Property Changes

Batch multiple property changes into a single notification:

```csharp
var person = Observable<Person>.Create();

using (person.DeferPropertyChanges())
{
    person.FirstName = "John";
    person.LastName = "Doe";
    person.Age = 30;
    // No PropertyChanged events yet
}
// All PropertyChanged events fire here
```

### Combining Observables

```csharp
var firstName = new BehaviorSubject<string>("John");
var lastName = new BehaviorSubject<string>("Doe");

var fullName = Observable.CombineLatest(
    firstName, 
    lastName, 
    (first, last) => $"{first} {last}"
);

fullName.Subscribe(name => Console.WriteLine(name)); // "John Doe"
lastName.OnNext("Smith"); // "John Smith"
```

## Advanced Features

### Observable Aggregates

```csharp
var numbers = new ObservableList<int> { 1, 2, 3, 4, 5 };
var sum = numbers.Aggregate(0, (total, n) => total + n);

sum.PropertyChanges.Subscribe(change => 
{
    Console.WriteLine($"Sum changed to: {sum.Value}");
});

numbers.Add(6); // Sum automatically updates to 21
```

### Weak References

Prevent memory leaks with weak subscriptions:

```csharp
observable.SubscribeWeak(observer);
```

### Observable Operators

- `Select`, `Where`, `DistinctUntilChanged`
- `Take`, `Skip`
- `CombineLatest`
- `ObserveOn` (dispatcher support)
- `FirstAsync`

## Requirements

- .NET 9.0 or later

## License

This project is licensed under the MIT License.

## Contributing

Contributions are welcome! Please feel free to submit issues or pull requests.

## Repository

[https://github.com/zlatanov/observable-model](https://github.com/zlatanov/observable-model)
