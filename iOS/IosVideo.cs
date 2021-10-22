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

        NSObject NotificationCenterToken;

        Preparedhandler Prepared = new Preparedhandler();

        public IosVideo(VideoPlayer view)
        {
            View = view;

            view.Width.Changed.HandleOn(Thread.UI, OnFrameChanged);
            view.Height.Changed.HandleOn(Thread.UI, OnFrameChanged);

            View.Buffered.HandleOn(Thread.UI, BufferVideo);
            View.PathChanged.HandleOn(Thread.UI, LoadVideo);
            View.Started.HandleOn(Thread.UI, () => Prepared.Raise(VideoState.Play));
            View.Paused.HandleOn(Thread.UI, () => Prepared.Raise(VideoState.Pause));
            View.Resumed.HandleOn(Thread.UI, () => Prepared.Raise(VideoState.Resume));
            View.Stopped.HandleOn(Thread.UI, () => Prepared.Raise(VideoState.Stop));
            View.SoughtBeginning.HandleOn(Thread.UI, () => Prepared.Raise(VideoState.SeekToBegining));
            View.Muted.HandleOn(Thread.UI, Mute);

            NotificationCenterToken = NSNotificationCenter.DefaultCenter.AddObserver(AVPlayerItem.DidPlayToEndTimeNotification, (notify) =>
            {
                if (IsDead(out var _)) return;
                View.FinishedPlaying.RaiseOn(Thread.UI);
            });

            LoadVideo();
        }

        void OnFrameChanged()
        {
            if (PlayerLayer == null) return;
            var frame = View.GetFrame();

            Frame = frame;
            PlayerLayer.Frame = Bounds;
        }

        void Mute()
        {
            if (View.Loop)
                QueuePlayer.Muted = View.IsMuted;
            else
                Player.Muted = View.IsMuted;
        }

        void Resume()
        {
            if (View.Loop)
                QueuePlayer?.Play();
            else
                Player?.Play();
        }

        void SeekBeginning()
        {
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
            if (View.Loop)
                QueuePlayer?.Pause();
            else
                Player?.Pause();
        }

        void Stop()
        {
            if (View.Loop)
                QueuePlayer?.Pause();
            else
                Player?.Pause();
        }

        void LoadVideo()
        {
            string url = View.Path;
            if (url.IsEmpty()) return;

            if (url.IsUrl())
            {
                if (View.AutoBuffer) BufferVideo();
            }
            else
            {
                UIGraphics.BeginImageContext(new CoreGraphics.CGSize(1, 1));

                Frame = View.GetFrame();

                url = "file://" + Device.IO.File(url).FullName;
                Asset = AVAsset.FromUrl(NSUrl.FromString(url));

                SetNaturalVideoSize(asset: Asset, urlAsset: null);

                PlayerItem = new AVPlayerItem(Asset);

                InitializePlayerItem();

                UIGraphics.EndImageContext();
            }

            View.LoadCompleted.Raise();
        }


        void InitializePlayerItem()
        {
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

                PlayerItem.AddObserver(Self, "status", 0, IntPtr.Zero);
            }

            PlayerLayer.VideoGravity = AVLayerVideoGravity.ResizeAspectFill;

            PlayerLayer.Frame = Bounds;
            Layer.AddSublayer(PlayerLayer);
        }

        void BufferVideo()
        {
            string url = View.Path;
            if (url.IsEmpty()) return;

            UIGraphics.BeginImageContext(new CoreGraphics.CGSize(1, 1));

            Frame = View.GetFrame();

            UrlAsset = new AVUrlAsset(NSUrl.FromString(url));

            SetNaturalVideoSize(asset: null, urlAsset: UrlAsset);

            PlayerItem = new AVPlayerItem(UrlAsset);

            InitializePlayerItem();

            UIGraphics.EndImageContext();
        }

        void OnReady()
        {
            if (View.AutoPlay)
            {
                if (View.Loop)
                    QueuePlayer.Play();
                else
                    Player.Play();
            }

            Prepared.Handle(result =>
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

            AVAssetTrack[] tracks;

            if (asset == null) tracks = urlAsset.TracksWithMediaType(AVMediaType.Video);
            else tracks = asset.TracksWithMediaType(AVMediaType.Video);

            if (tracks.None()) return;

            var track = tracks.First();

            var size = track.NaturalSize;
            var txf = track.PreferredTransform;

            var videoSize = txf.TransformSize(size);

            View.VideoSize = new Size((float)videoSize.Width, (float)videoSize.Height);
        }

        public override void ObserveValue(NSString keyPath, NSObject ofObject, NSDictionary change, IntPtr context)
        {
            if (IsDead(out var _)) return;
            if (ofObject is AVPlayerItem item && keyPath == "status")
            {
                if (item.Status == AVPlayerItemStatus.ReadyToPlay)
                {
                    View.IsReady = true;
                    OnReady();
                }
                else View.IsReady = false;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                NSNotificationCenter.DefaultCenter.RemoveObserver(NotificationCenterToken);

                if (View is not null && View.Loop == false) PlayerItem.RemoveObserver(Self, "status");

                Asset?.Dispose();
                Asset = null;

                UrlAsset?.Dispose();
                UrlAsset = null;

                PlayerItem?.Dispose();
                PlayerItem = null;

                Player?.Pause();
                Player?.Dispose();
                Player = null;

                QueuePlayer?.Pause();
                QueuePlayer?.Dispose();
                QueuePlayer = null;

                PlayerLooper?.DisableLooping();
                PlayerLooper?.Dispose();
                PlayerLooper = null;

                PlayerLayer?.Dispose();
                PlayerLayer = null;

                Prepared = null;
                View = null;
            }

            base.Dispose(disposing);
        }

        [EscapeGCop("In this case an out parameter can improve the code.")]
        bool IsDead(out VideoPlayer result)
        {
            result = View;
            if (result == null) return true;
            return result.IsDisposing;
        }
    }
}