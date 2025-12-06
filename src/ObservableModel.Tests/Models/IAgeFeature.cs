using System.ComponentModel;

namespace ObservableModel
{
    public interface IAgeFeature : INotifyPropertyChanged
    {
        int Age { get; set; }


        [ObservablePropertyDependency( nameof( Age ) )]
        public bool IsAgeEven => Age % 2 == 0;
    }
}
