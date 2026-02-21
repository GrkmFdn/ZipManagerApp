using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ZipApp.Models
{
    public class FileItem : INotifyPropertyChanged
    {
        private bool _isSelected;

        public string Name { get; set; }
        public string FullPath { get; set; }
        public string Type { get; set; } // "File" or "Folder"
        public string Size { get; set; } // Formatted string, e.g. "1.2 MB" or ""

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
