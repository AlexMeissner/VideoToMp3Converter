using PropertyChanged;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace VideoToMp3Converter
{
    [AddINotifyPropertyChangedInterface]
    public class ViewModel : INotifyPropertyChanged
    {
        public string URL { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string Step { get; set; } = string.Empty;
        public Visibility InputMaskVisibility { get; set; } = Visibility.Visible;
        public Visibility SpinnerVisibility => InputMaskVisibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;

        public event PropertyChangedEventHandler? PropertyChanged = (sender, e) => { };

        public void OnPropertyChanged([CallerMemberName] string name = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}