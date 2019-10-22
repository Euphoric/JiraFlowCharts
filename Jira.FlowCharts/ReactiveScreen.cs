using System.ComponentModel;
using Caliburn.Micro;
using ReactiveUI;

namespace Jira.FlowCharts
{
    public abstract class ReactiveScreen : Screen, IReactiveObject
    {
        public override void NotifyOfPropertyChange(string propertyName = null)
        {
            PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));

            base.NotifyOfPropertyChange(propertyName);
        }

        public event PropertyChangingEventHandler PropertyChanging;

        public void RaisePropertyChanging(PropertyChangingEventArgs args)
        {
        }

        public void RaisePropertyChanged(PropertyChangedEventArgs args)
        {
            NotifyOfPropertyChange(args.PropertyName);
        }
    }
}