namespace ObservableModel
{
    public abstract class Dog : Animal
    {
        [ObservableProperty]
        public virtual new object Name { get; set; }
    }
}
