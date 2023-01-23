using Olive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YoutubeExplode;

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
        internal readonly AsyncEvent<TimeSpan> Seeked = new AsyncEvent<TimeSpan>();
        internal readonly AsyncEvent<VideoPlayer> Muted = new AsyncEvent<VideoPlayer>();

        public readonly AsyncEvent FinishedPlaying = new AsyncEvent();
        public readonly AsyncEvent LoadCompleted = new AsyncEvent();

        public VideoQuality Quality { get; set; } = VideoQuality.Medium;
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
                Muted.Raise(this);
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

        public void Seek(TimeSpan timeSpan) => Seeked.Raise(timeSpan);

        public TimeSpan? Duration { get; set; }

        public override void Dispose()
        {
            PathChanged?.Dispose();
            base.Dispose();
        }

        public async Task LoadYoutube(string url)
        {
            string host = url.AsUri()?.Host;
            if (host.IsEmpty())
                throw new InvalidOperationException(url);
            if (host.EndsWith("youtube.com", StringComparison.OrdinalIgnoreCase) || host.EndsWith("youtu.be", StringComparison.OrdinalIgnoreCase))
            {
                var youtube = new YoutubeClient();
                var video = await youtube.Videos.GetAsync(url);
                var streamManifest = await youtube.Videos.Streams.GetManifestAsync(video.Id);
                var orderedStreams = streamManifest.GetMuxedStreams().OrderByDescending(x => x.ToString().Contains("mp4", StringComparison.OrdinalIgnoreCase));
                string videoUrl = "";
                switch (Quality)
                {
                    case VideoQuality.Low:
                        {
                            videoUrl = orderedStreams.OrderBy(x => x.VideoQuality.MaxHeight).FirstOrDefault()?.Url;
                            break;
                        }
                    case VideoQuality.Medium:
                        {
                            videoUrl = orderedStreams.OrderBy(x => x.VideoQuality.MaxHeight).Skip(orderedStreams.Count() / 2).FirstOrDefault()?.Url;
                            break;
                        }
                    case VideoQuality.High:
                        {
                            videoUrl = orderedStreams.OrderByDescending(x => x.VideoQuality.MaxHeight).FirstOrDefault()?.Url;
                            break;
                        }
                }
                if (videoUrl.IsEmpty())
                    videoUrl = orderedStreams.FirstOrDefault()?.Url;
                if (videoUrl.IsEmpty())
                    throw new InvalidOperationException("Muxed video not found!");
                Path = videoUrl;
            }
            else
                throw new InvalidOperationException(url);
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