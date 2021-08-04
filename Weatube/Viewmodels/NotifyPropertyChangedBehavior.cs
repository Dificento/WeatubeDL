using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Weatube.Viewmodels
{
    class NotifyPropertyChangedBehavior : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
    }
}
