using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Zebble
{
    public partial class VideoPlayer : View, IRenderedBy<VideoPlayerRenderer>
    {
        string path; bool isMute;
        internal readonly AsyncEvent PathChanged = new AsyncEvent();
        internal readonly AsyncEvent Started = new AsyncEvent();
        internal readonly AsyncEvent Paused = new AsyncEvent();
        internal readonly AsyncEvent Resumed = new AsyncEvent();
        internal readonly AsyncEvent Stopped = new AsyncEvent();
        internal readonly AsyncEvent SoughtBeginning = new AsyncEvent();
        internal readonly AsyncEvent Buffered = new AsyncEvent();
        internal readonly AsyncEvent Muted = new AsyncEvent();

        internal static VideoPlayer Instance;

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

        public bool IsMuted
        {
            get => isMute;
            set
            {
                if (isMute == value) return;
                isMute = value;
                Muted.Raise();
            }
        }

        public bool AutoPlay { get; set; }

        public bool AutoBuffer { get; set; } = true;

        public bool IsReady { get; set; }

        public bool Loop { get; set; }

        public bool ShowControls { get; set; }

        public void Start() => Started.Raise();

        public void Pause() => Paused.Raise();

        public void Resume() => Resumed.Raise();

        public void Stop() => Stopped.Raise();

        public void SeekBeginning() => SoughtBeginning.Raise();

        public void BufferVideo() => Buffered.Raise();

        public override void Dispose()
        {
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