using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp2.Models
{
    public class ImageInfo : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        ImageLabelState _labelState;
        ImageProcessState _processState;

        public ImageLabelState LabelState
        {
            get => _labelState; set
            {
                if (_labelState != value)
                {
                    _labelState = value;
                    OnPropertyChanged();
                }
            }
        }
        public ImageProcessState ProcessState
        {
            get => _processState; set
            {
                if (_processState != value)
                {
                    _processState = value;
                    OnPropertyChanged();
                }
            }
        }
    }

    public enum ImageProcessState
    {
        Unlabeled,
        Labeled,
        Saving,
        Invalid
    }
}
