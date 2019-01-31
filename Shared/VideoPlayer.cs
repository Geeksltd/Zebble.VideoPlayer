namespace Zebble
{
    public partial class VideoPlayer : View, IRenderedBy<VideoPlayerRenderer>
    {
        string path;
        internal readonly AsyncEvent PathChanged = new AsyncEvent();
        internal readonly AsyncEvent Started = new AsyncEvent();
        internal readonly AsyncEvent Paused = new AsyncEvent();
        internal readonly AsyncEvent Resumed = new AsyncEvent();
        internal readonly AsyncEvent Stopped = new AsyncEvent();
        internal readonly AsyncEvent SoughtBeginning = new AsyncEvent();

        public readonly AsyncEvent FinishedPlaying = new AsyncEvent();
        public readonly AsyncEvent LoadCompleted = new AsyncEvent();

        public Size VideoSize { get; set; } = new Size(0, 0);

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

        public void Start() => Started.Raise();

        public void Pause() => Paused.Raise();

        public void Resume() => Resumed.Raise();

        public void Stop() => Stopped.Raise();

        public void SeekBeginning() => SoughtBeginning.Raise();


        public override void Dispose()
        {
            PathChanged?.Dispose();
            base.Dispose();
        }
    }
}