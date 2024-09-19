namespace Zebble
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices.WindowsRuntime;
    using Olive;
    using controls = Microsoft.UI.Xaml.Controls;

    class WinUIVideoViewer
    {
        internal controls.MediaPlayerElement Result;
        VideoPlayer View;

        public WinUIVideoViewer(VideoPlayer view)
        {
            View = view;
            View.PathChanged.HandleOn(Thread.UI, Load);
            View.PathNullified.HandleOn(Thread.UI, OnPathNullified);
            View.Started.HandleOn(Thread.UI, () => Result?.MediaPlayer?.Play());
            View.Paused.HandleOn(Thread.UI, () => Result?.MediaPlayer?.Pause());
            View.Resumed.HandleOn(Thread.UI, () => Result?.MediaPlayer?.Play());
            View.Stopped.HandleOn(Thread.UI, () => Result?.MediaPlayer?.Pause());
            View.Seeked.HandleOn(Thread.UI, (position) => Result.MediaPlayer.Position = position);
            View.SoughtBeginning.HandleOn(Thread.UI, () => Result.MediaPlayer.Position = TimeSpan.Zero);
            view.Muted.HandleOn(Thread.UI, () => Result.MediaPlayer.IsMuted = view.IsMuted);
            View.GetCurrentTime = () => Result.MediaPlayer.Position;
            View.InitializeTimer();

            var mediaPlayer = new Windows.Media.Playback.MediaPlayer();
            mediaPlayer.IsLoopingEnabled = View.Loop;

            Result = new controls.MediaPlayerElement
            {
                Stretch = view.BackgroundImageStretch.Render(),
                AutoPlay = View.AutoPlay
            };
            Result.MediaPlayer.IsLoopingEnabled = View.Loop;
            Result.MediaPlayer.MediaEnded += MediaEnded;
            Result.MediaPlayer.MediaOpened += MediaOpened;

            Load();
        }

        void MediaEnded(Windows.Media.Playback.MediaPlayer sender, object args) => View.FinishedPlaying.RaiseOn(Thread.Pool);

        void OnPathNullified() => Result.MediaPlayer.Source = null;

        void Load()
        {
            var url = View.Path;
            if (url.IsEmpty()) return;

            if (url.IsUrl())
            {
                Result.MediaPlayer.Source = Windows.Media.Core.MediaSource.CreateFromUri(new Uri(url));
                View.LoadedPath = url;
            }
            else
            {
                try
                {
                    var file = Device.IO.File(url);
                    var data = file.ReadAllBytes();
                    var source = data.AsBuffer().AsStream().AsRandomAccessStream();
                    Result.MediaPlayer.SetStreamSource(source);
                }
                catch (Exception ex)
                {
                    Dialogs.Current.Toast("Failed to show video: " + ex.Message);
                }
                View.LoadedPath = url;
            }
        }

        void MediaOpened(Windows.Media.Playback.MediaPlayer sender, object args)
        {
            View.IsReady = true;
            View.Duration = TimeSpan.FromTicks(Result.MediaPlayer.NaturalDuration.Ticks);
            View.VideoSize = new Size(Result.MediaPlayer.PlaybackSession.NaturalVideoWidth, Result.MediaPlayer.PlaybackSession.NaturalVideoHeight);
            View.LoadCompleted.Raise();
            View.OnLoaded();
        }

        internal void Dispose()
        {
            if (Result is null) return;
            Result.MediaPlayer.MediaEnded -= MediaEnded;
            Result.MediaPlayer.MediaOpened -= MediaOpened;
            Result = null;

            GC.SuppressFinalize(this);
        }
    }
}
