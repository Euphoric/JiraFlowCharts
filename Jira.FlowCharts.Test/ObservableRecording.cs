using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reactive.Linq;

namespace Jira.FlowCharts
{
    public class ObservableRecording<T> : IDisposable
    {
        private readonly List<T> _observedItems = new List<T>();
        private readonly string _propertyName;
        private readonly INotifyPropertyChanged _inpc;

        public IEnumerable<T> ObservedItems { get => _observedItems; }

        public ObservableRecording(INotifyPropertyChanged inpc, string propertyName)
        {
            _inpc = inpc;
            _inpc.PropertyChanged += PropertyChanged;
            _propertyName = propertyName;
        }

        private void PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != _propertyName)
            {
                return;
            }

            var prop = sender.GetType().GetProperty(_propertyName);
            var value = prop.GetValue(sender);
            _observedItems.Add((T)value);
        }

        public void Dispose()
        {
            _inpc.PropertyChanged -= PropertyChanged;
        }
    }

    public static class ObservableRecordingExtension
    {
        public static ObservableRecording<T> RecordProperty<T>(this INotifyPropertyChanged inpc, string propertyName)
        {
            return new ObservableRecording<T>(inpc, propertyName);
        }
    }
}
