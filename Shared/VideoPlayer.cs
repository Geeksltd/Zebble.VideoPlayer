namespace Zebble
{
    public partial class VideoPlayer : View, IRenderedBy<VideoPlayerRenderer>
    {
        string path;
        public readonly AsyncEvent PathChanged = new AsyncEvent();

        public string Path
        {
            get => path;
            set { path = value; PathChanged.Raise(); }
        }

        public bool AutoPlay { get; set; }

        public bool Loop { get; set; }

        public bool ShowControls { get; set; }

        public override void Dispose()
        {
            PathChanged?.Dispose();
            base.Dispose();
        }
    }
}