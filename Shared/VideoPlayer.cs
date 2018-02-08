namespace Zebble
{
    public partial class VideoPlayer : View, IRenderedBy<VideoPlayerRenderer>
    {
        string path;
        public readonly AsyncEvent PathChanged = new AsyncEvent();
        public readonly AsyncEvent Played = new AsyncEvent();
        public readonly AsyncEvent Paused = new AsyncEvent();
        public readonly AsyncEvent Stopped = new AsyncEvent();

        public string Path
        {
            get => path;
            set
            {
                if (path == value) return;
                path = value;
                PathChanged.Raise();
            }
        }

        public bool AutoPlay { get; set; }

        public bool Loop { get; set; }

        public bool ShowControls { get; set; }

        public void Play() => Played.Raise();

        public void Pause() => Paused.Raise();

        public void Stop() => Stopped.Raise();

        public override void Dispose()
        {
            PathChanged?.Dispose();
            base.Dispose();
        }
    }
}