namespace ObservableModel
{
    public abstract class Animal : ObservableObject
    {
        [ObservableProperty]
        public virtual string Name { get; set; }
    }
}
