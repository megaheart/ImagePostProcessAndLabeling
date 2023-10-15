using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp2.Models
{
    public class WindowStateContext : INotifyPropertyChanged
    {
        private bool _allReactiveUIEnabled;
        public bool AllReactiveUIEnabled 
        { 
            get => _allReactiveUIEnabled;
            set
            {
                if(_allReactiveUIEnabled != value)
                {
                    _allReactiveUIEnabled = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName]string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
