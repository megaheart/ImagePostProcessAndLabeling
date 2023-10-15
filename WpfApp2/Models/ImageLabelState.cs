using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp2.Models
{
    public class ImageLabelState : INotifyPropertyChanged
    {
        string _imagePath;
        public string ImagePath
        {
            get => _imagePath;
            set
            {
                if (_imagePath != value)
                {
                    _imagePath = value;
                    OnPropertyChanged();
                }
            }
        }
        public Dictionary<string, bool> Labels { get; set; } = new Dictionary<string, bool>();
        public int CutOffsetX { get; set; } = 0;
        public int CutOffsetY { get; set; } = 0;
        public int CutWidth { get; set; } = 0;
        public int CutHeight { get; set; } = 0;

        public int StateVersion { get; set; } = 0;

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ImageLabelState Clone()
        {
            return new ImageLabelState()
            {
                ImagePath = this.ImagePath,
                Labels = this.Labels.ToDictionary(x => x.Key, x => x.Value),
                CutOffsetX = this.CutOffsetX,
                CutOffsetY = this.CutOffsetY,
                CutWidth = this.CutWidth,
                CutHeight = this.CutHeight,
                StateVersion = this.StateVersion
            };
        }
    }
}

