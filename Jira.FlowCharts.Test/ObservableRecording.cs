using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reactive.Linq;

namespace Jira.FlowCharts
{
    public class ObservableRecording<T> : IDisposable
    {
        private readonly List<T> observedItems = new List<T>();
        private readonly string propertyName;
        private readonly INotifyPropertyChanged _inpc;

        public IEnumerable<T> ObservedItems { get => observedItems; }

        public ObservableRecording(INotifyPropertyChanged inpc, string propertyName)
        {
            _inpc = inpc;
            _inpc.PropertyChanged += PropertyChanged;
            this.propertyName = propertyName;
        }

        private void PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != propertyName)
            {
                return;
            }

            var prop = sender.GetType().GetProperty(propertyName);
            var value = prop.GetValue(sender);
            observedItems.Add((T)value);
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
