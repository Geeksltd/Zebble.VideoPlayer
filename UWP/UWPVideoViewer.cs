namespace Zebble
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices.WindowsRuntime;
    using Olive;
    using controls = Windows.UI.Xaml.Controls;

    class UWPVideoViewer
    {
        internal controls.MediaElement Result;
        VideoPlayer View;

        public UWPVideoViewer(VideoPlayer view)
        {
            View = view;
            View.PathChanged.HandleOn(Thread.UI, Load);
            View.Started.HandleOn(Thread.UI, () => Result?.Play());
            View.Paused.HandleOn(Thread.UI, () => Result?.Pause());
            View.Resumed.HandleOn(Thread.UI, () => Result?.Play());
            View.Stopped.HandleOn(Thread.UI, () => Result?.Stop());
            View.Seeked.HandleOn(Thread.UI, (position) => Result.Position = position);
            View.SoughtBeginning.HandleOn(Thread.UI, () => Result.Position = 0.Milliseconds());
            view.Muted.HandleOn(Thread.UI, () => Result.IsMuted = view.IsMuted);
            View.GetCurrentTime = () => Result.Position;
            View.InitializeTimer();

            Result = new controls.MediaElement { Stretch = view.BackgroundImageStretch.Render(), AutoPlay = View.AutoPlay, IsLooping = View.Loop };
            Result.MediaEnded += MediaEnded;
            Result.MediaOpened += MediaOpened;            

            Load();
        }

        void MediaEnded(object sender, Windows.UI.Xaml.RoutedEventArgs e) => View.FinishedPlaying.RaiseOn(Thread.Pool);

        void Load()
        {
            var url = View.Path;
            if (url.IsEmpty()) return;

            if (url.IsUrl())
            {
                Result.Source = url.AsUri();
                View.LoadedPath = url;
            }
            else
            {
                try
                {
                    var file = Device.IO.File(url);
                    var data = file.ReadAllBytes();
                    var source = data.AsBuffer().AsStream().AsRandomAccessStream();
                    Result.SetSource(source, string.Empty);
                }
                catch (Exception ex)
                {
                    Dialogs.Current.Toast("Failed to show video: " + ex.Message);
                }
                View.LoadedPath = url;
            }
        }

        void MediaOpened(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            View.IsReady = true;
            View.Duration = Result.NaturalDuration.TimeSpan;
            View.VideoSize = new Size(Result.NaturalVideoWidth, Result.NaturalVideoHeight);
            View.LoadCompleted.Raise();
            View.OnLoaded();
        }

        internal void Dispose()
        {
            if (Result is null) return;
            Result.MediaEnded -= MediaEnded;
            Result.MediaOpened -= MediaOpened;
            Result = null;
			
			GC.SuppressFinalize(this);
        }
    }
}