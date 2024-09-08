using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;

namespace Zebble
{
    public partial class VideoPlayer : View, IRenderedBy<VideoPlayerRenderer>
    {
        string path, originalPath; bool isMute;
        internal string LoadedPath { get; set; }
        internal readonly AsyncEvent PathChanged = new AsyncEvent();
        internal readonly AsyncEvent PathNullified = new();
        internal readonly AsyncEvent Started = new AsyncEvent();
        internal readonly AsyncEvent Paused = new AsyncEvent();
        internal readonly AsyncEvent Resumed = new AsyncEvent();
        internal readonly AsyncEvent Stopped = new AsyncEvent();
        internal readonly AsyncEvent SoughtBeginning = new AsyncEvent();
        internal readonly AsyncEvent BufferRequested = new AsyncEvent();
        internal readonly AsyncEvent<TimeSpan> Seeked = new AsyncEvent<TimeSpan>();
        public readonly AsyncEvent<TimeSpan?> TimeChanged = new AsyncEvent<TimeSpan?>();
        internal readonly AsyncEvent<VideoPlayer> Muted = new AsyncEvent<VideoPlayer>();
        internal Func<TimeSpan?> GetCurrentTime;

        public readonly AsyncEvent FinishedPlaying = new AsyncEvent();
        public readonly AsyncEvent LoadCompleted = new AsyncEvent();

        public VideoQuality Quality { get; set; } = VideoQuality.Medium;
        public Size VideoSize { get; set; } = new Size(0, 0);
        public TimeSpan? StartPosition { get; set; }
        public TimeSpan? EndPosition { get; set; }

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

        public bool IsMuted
        {
            get => isMute;
            set
            {
                if (isMute == value) return;
                isMute = value;
                Muted.Raise(this);
            }
        }

        public bool AutoPlay { get; set; }

        public bool AutoBuffer { get; set; } = true;

        public bool IsReady { get; set; }

        public bool Loop { get; set; }

        public bool ShowControls { get; set; }

        public VideoPlayer()
        {
            Zebble.Device.App.WentIntoBackground += OnBackgroundGone;
            Zebble.Device.App.CameToForeground += OnForegroundGone;
        }

        void OnBackgroundGone()
        {
            Stop();
            originalPath = path;
            path = null;
            PathNullified?.Raise();
        }

        void OnForegroundGone() => Path = originalPath;

        public void Start() => Started.Raise();

        public void Pause() => Paused.Raise();

        public void Resume() => Resumed.Raise();

        public void Stop() => Stopped.Raise();

        public void SeekBeginning() => SoughtBeginning.Raise();

        public void BufferVideo() => BufferRequested.Raise();

        public void Seek(TimeSpan timeSpan) => Seeked.Raise(timeSpan);

        public TimeSpan? Duration { get; internal set; }

        public TimeSpan? CurrentTime
        {
            get
            {
                if (IsDisposed) return TimeSpan.Zero;
                if (GetCurrentTime is null) return TimeSpan.Zero;
                return GetCurrentTime() ?? TimeSpan.Zero;
            }
        }

        System.Timers.Timer CurrentTimeChangedTimer;
        internal void InitializeTimer()
        {
            if (CurrentTimeChangedTimer != null) return;
            CurrentTimeChangedTimer = new Timer();
            CurrentTimeChangedTimer.Elapsed += OnRaiseCurrentTime;
            CurrentTimeChangedTimer.Interval = 1000;
            CurrentTimeChangedTimer.Enabled = true;
        }

        void OnRaiseCurrentTime(object sender, ElapsedEventArgs e)
        {
            var mayNeedRestart = Loop && EndPosition.HasValue && StartPosition.HasValue;

            if (mayNeedRestart || TimeChanged.IsHandled())
                Thread.UI.Run(() =>
                {
                    if (mayNeedRestart && CurrentTime >= EndPosition)
                        Seek(StartPosition.Value);

                    TimeChanged.Raise(CurrentTime);
                });
        }

        internal void OnLoaded()
        {
            if (StartPosition.HasValue) Seek(StartPosition.Value);
        }

        public override void Dispose()
        {
            var timer = CurrentTimeChangedTimer;

            if (timer != null)
            {
                timer.Elapsed -= OnRaiseCurrentTime;
                timer.Dispose();
            }

            Zebble.Device.App.WentIntoBackground -= OnBackgroundGone;
            Zebble.Device.App.CameToForeground -= OnForegroundGone;

            PathChanged?.Dispose();
            base.Dispose();
        }

        internal enum VideoState { Play, Pause, Stop, SeekToBegining, Resume }
        internal class Preparedhandler
        {
            readonly AsyncEvent<VideoState> Prepared = new AsyncEvent<VideoState>();
            readonly HashSet<VideoState> QueuedActions = new HashSet<VideoState>();

            public Task Raise(VideoState state)
            {
                if (!Prepared.IsHandled())
                    QueuedActions.Add(state);

                return Prepared.Raise(state);
            }

            public void Handle(Action<VideoState> action)
            {
                if (QueuedActions.Count > 0)
                    foreach (var state in QueuedActions) action(state);

                QueuedActions.Clear();
                Prepared.ClearHandlers();
                Prepared.Handle(action);
            }
        }
    }
}
