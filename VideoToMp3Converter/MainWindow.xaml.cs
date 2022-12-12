using DotNetTools.SharpGrabber;
using DotNetTools.SharpGrabber.Grabbed;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace VideoToMp3Converter
{
    public partial class MainWindow : Window
    {
        public ViewModel ViewModel { get; set; } = new();

        private readonly IMultiGrabber Grabber;
        private readonly HttpClient Client = new();

        public MainWindow()
        {
            InitializeComponent();

            Client.Timeout = TimeSpan.FromMinutes(180.0);

            Grabber = GrabberBuilder.New()
                .UseDefaultServices()
                .AddYouTube()
                .Build();
        }

        private void OnDrag(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void OnExit(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void OnUrlChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                ViewModel.InputMaskVisibility = Visibility.Collapsed;
                ViewModel.Step = "Getting video information...";
                try
                {
                    var result = await Grabber.GrabAsync(new Uri(textBox.Text, UriKind.Absolute));
                    ViewModel.FileName = result.Title;
                }
                catch (Exception exception)
                {
                    MessageBox.Show(exception.Message, "Error", MessageBoxButton.OK);
                }
                ViewModel.InputMaskVisibility = Visibility.Visible;
            }
        }

        private async void OnStart(object sender, RoutedEventArgs e)
        {
            if (ViewModel.URL.Length == 0 || !Uri.IsWellFormedUriString(ViewModel.URL, UriKind.Absolute))
            {
                MessageBox.Show("The URL is invalid.", "Invalid Input", MessageBoxButton.OK);
            }

            if (ViewModel.FileName.Length == 0)
            {
                MessageBox.Show("The filename is invalid.", "Invalid Input", MessageBoxButton.OK);
            }

            try
            {
                ViewModel.Step = "Preparing...";
                ViewModel.InputMaskVisibility = Visibility.Collapsed;
                string outputFilename = ViewModel.FileName;

                foreach (var invalidPathCharacter in Path.GetInvalidFileNameChars())
                {
                    outputFilename = outputFilename.Replace(invalidPathCharacter, '_');
                }

                outputFilename = Path.Combine(GetDownloadDirectoryPath(), $"{outputFilename}.mp3");

                ViewModel.Step = "Grabbing video information...";
                var result = await Grabber.GrabAsync(new Uri(ViewModel.URL, UriKind.Absolute));
                var resources = result.Resources<GrabbedMedia>().Where(x => x.Channels == MediaChannels.Audio);

                if (resources.First(x => x.BitRateString == "128k") is GrabbedMedia audioStream)
                {
                    ViewModel.Step = "Getting audio stream...";
                    using var response = await Client.GetAsync(audioStream.ResourceUri);
                    response.EnsureSuccessStatusCode();

                    ViewModel.Step = "Parsing audio stream...";
                    using var downloadStream = await response.Content.ReadAsStreamAsync();

                    ViewModel.Step = "Generating resource stream...";
                    using var resourceStream = await result.WrapStreamAsync(downloadStream);

                    ViewModel.Step = "Copy data to file...";
                    using var fileStream = new FileStream(outputFilename, FileMode.Create);
                    await resourceStream.CopyToAsync(fileStream);
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Error", MessageBoxButton.OK);
            }
            finally
            {
                ViewModel.Step = "Cleaning up...";
                ViewModel.InputMaskVisibility = Visibility.Visible;
            }
        }

        private static string GetDownloadDirectoryPath()
        {
            return SHGetKnownFolderPath(new("374DE290-123F-4565-9164-39C4925E467B"), 0);
        }

        [DllImport("shell32", CharSet = CharSet.Unicode, ExactSpelling = true, PreserveSig = false)]
        private static extern string SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint dwFlags, nint hToken = 0);
    }
}