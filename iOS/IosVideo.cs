namespace Zebble
{
    using AVFoundation;
    using Foundation;
    using System;
    using System.Linq;
    using UIKit;
    using static Zebble.VideoPlayer;
    using Olive;

    class IosVideo : UIView
    {
        VideoPlayer View;
        AVAsset Asset;
        AVUrlAsset UrlAsset;
        AVPlayerItem PlayerItem;
        AVPlayer Player;
        AVPlayerLayer PlayerLayer;

        AVPlayerLooper PlayerLooper;
        AVQueuePlayer QueuePlayer;

        IDisposable DidPlayToEndTimeObservation;
        IDisposable StatusObservation;

        Preparedhandler Prepared = new();

        public IosVideo(VideoPlayer view)
        {
            View = view;

            View.Width.Changed.HandleOn(Thread.UI, OnFrameChanged);
            View.Height.Changed.HandleOn(Thread.UI, OnFrameChanged);
            View.Buffered.HandleOn(Thread.UI, BufferVideo);
            View.PathChanged.HandleOn(Thread.UI, () => { CoreDispose(); LoadVideo(); });
            View.Started.HandleOn(Thread.UI, () => Prepared?.Raise(VideoState.Play));
            View.Paused.HandleOn(Thread.UI, () => Prepared?.Raise(VideoState.Pause));
            View.Resumed.HandleOn(Thread.UI, () => Prepared?.Raise(VideoState.Resume));
            View.Stopped.HandleOn(Thread.UI, () => Prepared?.Raise(VideoState.Stop));
            View.SoughtBeginning.HandleOn(Thread.UI, () => Prepared?.Raise(VideoState.SeekToBegining));
            View.Muted.HandleOn(Thread.UI, Mute);

            LoadVideo();
        }

        void OnFrameChanged()
        {
            if (IsDead(out var view)) return;
            if (PlayerLayer == null) return;

            var frame = View.GetFrame();

            Frame = frame;
            PlayerLayer.Frame = Bounds;
        }

        void Mute()
        {
            if (IsDead(out var _)) return;

            if (View.Loop)
                QueuePlayer.Muted = View.IsMuted;
            else
                Player.Muted = View.IsMuted;
        }

        void Resume()
        {
            if (IsDead(out _)) return;

            if (View.Loop)
                QueuePlayer?.Play();
            else
                Player?.Play();
        }

        void SeekBeginning()
        {
            if (IsDead(out _)) return;

            if (View.Loop)
                QueuePlayer?.Seek(CoreMedia.CMTime.Zero);
            else
                Player?.Seek(CoreMedia.CMTime.Zero);
        }

        void Play()
        {
            SeekBeginning();
            Resume();
        }

        void Pause()
        {
            if (IsDead(out _)) return;

            if (View.Loop)
                QueuePlayer?.Pause();
            else
                Player?.Pause();
        }

        void Stop()
        {
            if (IsDead(out _)) return;

            if (View.Loop)
                QueuePlayer?.Pause();
            else
                Player?.Pause();
        }

        void LoadVideo()
        {
            if (IsDead(out var _)) return;

            string url = View.Path;
            if (url.IsEmpty()) return;

            if (url.IsUrl())
            {
                if (View.AutoBuffer) BufferVideo();
            }
            else
            {
                url = "file://" + Device.IO.File(url).FullName;
                var nsUrl = url.ToNsUrl();
                // It's possible for a non-null url, NSUrl return a null value
                if (nsUrl == null) return;

                UIGraphics.BeginImageContext(new CoreGraphics.CGSize(1, 1));

                Frame = View.GetFrame();

                Asset = AVAsset.FromUrl(nsUrl);

                SetNaturalVideoSize(asset: Asset, urlAsset: null);

                PlayerItem = new AVPlayerItem(Asset);

                InitializePlayerItem();

                UIGraphics.EndImageContext();
            }

            View.LoadCompleted.Raise();
        }

        void InitializePlayerItem()
        {
            DidPlayToEndTimeObservation = AVPlayerItem.Notifications.ObserveDidPlayToEndTime(PlayerItem, (_, _) =>
            {
                if (IsDead(out var _)) return;
                View.FinishedPlaying.RaiseOn(Thread.UI);
            });

            if (View.Loop)
            {
                QueuePlayer = new AVQueuePlayer();
                PlayerLayer = AVPlayerLayer.FromPlayer(QueuePlayer);
                PlayerLooper = new AVPlayerLooper(QueuePlayer, PlayerItem, CoreMedia.CMTimeRange.InvalidRange);

                OnReady();
                View.IsReady = true;
            }
            else
            {
                Player = new AVPlayer(PlayerItem);
                PlayerLayer = AVPlayerLayer.FromPlayer(Player);

                StatusObservation = PlayerItem.AddObserver("status", 0, _ =>
                {
                    if (IsDead(out var _)) return;
                    if (PlayerItem?.Status == AVPlayerItemStatus.ReadyToPlay)
                    {
                        View.IsReady = true;
                        OnReady();
                    }
                    else View.IsReady = false;
                });
            }

            PlayerLayer.VideoGravity = AVLayerVideoGravity.ResizeAspect;

            PlayerLayer.Frame = Bounds;
            Layer.AddSublayer(PlayerLayer);
        }

        void BufferVideo()
        {
            if (IsDead(out var _)) return;

            string url = View.Path;
            if (url.IsEmpty()) return;

            var nsUrl = url.ToNsUrl();
            // It's possible for a non-null url, NSUrl return a null value
            if (nsUrl == null) return;

            UIGraphics.BeginImageContext(new CoreGraphics.CGSize(1, 1));

            Frame = View.GetFrame();

            UrlAsset = new AVUrlAsset(nsUrl);

            SetNaturalVideoSize(asset: null, urlAsset: UrlAsset);

            PlayerItem = new AVPlayerItem(UrlAsset);

            InitializePlayerItem();

            UIGraphics.EndImageContext();
        }

        void OnReady()
        {
            if (IsDead(out _)) return;

            if (View.AutoPlay)
            {
                if (View.Loop)
                    QueuePlayer.Play();
                else
                    Player.Play();
            }

            Prepared?.Handle(result =>
            {
                if (IsDead(out var _)) return;

                switch (result)
                {
                    case VideoState.Play:
                        Play();
                        break;
                    case VideoState.Pause:
                        Pause();
                        break;
                    case VideoState.Stop:
                        Stop();
                        break;
                    case VideoState.SeekToBegining:
                        SeekBeginning();
                        break;
                    case VideoState.Resume:
                        Resume();
                        break;
                    default:
                        break;
                }
            });
        }

        void SetNaturalVideoSize(AVAsset asset = null, AVUrlAsset urlAsset = null)
        {
            if (asset == null && urlAsset == null) return;

            var tracks = (asset ?? urlAsset).TracksWithMediaType(AVMediaType.Video);
            if (tracks.None()) return;

            var track = tracks.First();

            var size = track.NaturalSize;
            var txf = track.PreferredTransform;

            var videoSize = txf.TransformSize(size);

            View.VideoSize = new Size((float)videoSize.Width, (float)videoSize.Height);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                CoreDispose();

                Prepared = null;
                View = null;
            }

            base.Dispose(disposing);
        }

        void CoreDispose()
        {
            DidPlayToEndTimeObservation?.Dispose();
            DidPlayToEndTimeObservation = null;

            StatusObservation?.Dispose();
            StatusObservation = null;

            Asset?.Dispose();
            Asset = null;

            UrlAsset?.Dispose();
            UrlAsset = null;

            PlayerItem?.Dispose();
            PlayerItem = null;

            Player?.Dispose();
            Player = null;

            QueuePlayer?.Dispose();
            QueuePlayer = null;

            PlayerLooper?.DisableLooping();
            PlayerLooper?.Dispose();
            PlayerLooper = null;

            PlayerLayer?.Dispose();
            PlayerLayer = null;
        }

        [EscapeGCop("In this case an out parameter can improve the code.")]
        bool IsDead(out VideoPlayer result)
        {
            result = View;
            if (result == null) return true;
            return result.IsDisposing || View.IsDisposed;
        }
    }
}