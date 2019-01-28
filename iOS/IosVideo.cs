namespace Zebble
{
    using AVFoundation;
    using Foundation;
    using System;
    using UIKit;

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

        bool IsStopped = false;


        public IosVideo(VideoPlayer view)
        {
            View = view;

            view.Width.Changed.HandleOn(Thread.UI, () => OnFrameChanged());
            view.Height.Changed.HandleOn(Thread.UI, () => OnFrameChanged());

            View.PathChanged.HandleOn(Thread.UI, () => LoadVideo());
            View.Started.HandleOn(Thread.UI, () => Play());
            View.Paused.HandleOn(Thread.UI, () => Pause());
            View.Resumed.HandleOn(Thread.UI, () => Play());
            View.Stopped.HandleOn(Thread.UI, () => Stop());

            NSNotificationCenter.DefaultCenter.AddObserver(AVPlayerItem.DidPlayToEndTimeNotification, (notify) =>
            {
                View.FinishedPlaying.RaiseOn(Thread.UI);
                IsStopped = true;
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

        void Play()
        {
            if (View.Loop)
            {
                if (IsStopped)
                {
                    QueuePlayer?.Seek(CoreMedia.CMTime.Zero);
                    IsStopped = false;
                }

                QueuePlayer?.Play();
            }
            else
            {
                if (IsStopped)
                {
                    Player?.Seek(CoreMedia.CMTime.Zero);
                    IsStopped = false;
                }

                Player?.Play();
            }
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

            IsStopped = true;
        }

        void LoadVideo()
        {
            UIGraphics.BeginImageContext(new CoreGraphics.CGSize(1, 1));

            Frame = View.GetFrame();

            string url = View.Path;

            if (url.IsUrl())
            {
                UrlAsset = new AVUrlAsset(NSUrl.FromString(url));
                PlayerItem = new AVPlayerItem(UrlAsset);
            }
            else
            {
                url = "file://" + Device.IO.File(url).FullName;
                Asset = AVAsset.FromUrl(NSUrl.FromString(url));
                PlayerItem = new AVPlayerItem(Asset);
            }

            if (View.Loop)
            {
                QueuePlayer = new AVQueuePlayer();
                PlayerLayer = AVPlayerLayer.FromPlayer(QueuePlayer);
                PlayerLooper = new AVPlayerLooper(QueuePlayer, PlayerItem, CoreMedia.CMTimeRange.InvalidRange);
            }
            else
            {
                Player = new AVPlayer(PlayerItem);
                PlayerLayer = AVPlayerLayer.FromPlayer(Player);
            }

            PlayerLayer.VideoGravity = AVLayerVideoGravity.ResizeAspectFill;
            PlayerLayer.Frame = Bounds;

            Layer.AddSublayer(PlayerLayer);

            UIGraphics.EndImageContext();

            if (View.AutoPlay)
            {
                if (View.Loop)
                    QueuePlayer.Play();
                else
                    Player.Play();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
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

                View = null;
            }

            base.Dispose(disposing);
        }
    }
}