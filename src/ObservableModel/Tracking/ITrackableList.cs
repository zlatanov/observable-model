using System.Collections.Generic;
using System.ComponentModel;

namespace ObservableModel
{
    public interface ITrackableList : IObservableList, ITrackable, ISupportInitialize
    {
        IEnumerable<TrackableListChangedItem> GetChangedItems();
    }
}
