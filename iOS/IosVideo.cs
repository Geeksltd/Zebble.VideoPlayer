namespace Zebble
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using AVFoundation;
    using CoreMedia;
    using Foundation;
    using Olive;
    using UIKit;
    using static Zebble.VideoPlayer;

    class IosVideo : UIView
    {
        VideoPlayer View;
        AVAsset Asset;
        AVPlayer Player;
        AVPlayerItem PlayerItem;
        AVPlayerLayer PlayerLayer;
        AVPlayerLooper PlayerLooper;

        IDisposable DidPlayToEndTimeObservation, StatusObservation;

        Preparedhandler Prepared = new();

        public IosVideo(VideoPlayer view)
        {
            View = view;

            view.Width.Changed.HandleOn(Thread.UI, OnFrameChanged);
            view.Height.Changed.HandleOn(Thread.UI, OnFrameChanged);
            view.BufferRequested.HandleOn(Thread.UI, BufferVideo);
            view.PathChanged.HandleOn(Thread.UI, () => { CoreDispose(); LoadVideo(); });
            view.Started.HandleOn(Thread.UI, () => Prepared?.Raise(VideoState.Play));
            view.Paused.HandleOn(Thread.UI, () => Prepared?.Raise(VideoState.Pause));
            view.Resumed.HandleOn(Thread.UI, () => Prepared?.Raise(VideoState.Resume));
            view.Stopped.HandleOn(Thread.UI, () => Prepared?.Raise(VideoState.Stop));
            view.SoughtBeginning.HandleOn(Thread.UI, () => Prepared?.Raise(VideoState.SeekToBegining));
            view.Muted.HandleOn(Thread.UI, Mute);
            view.Seeked.HandleOn(Thread.UI, (position) => Player?.Seek(CMTime.FromSeconds(position.Seconds, 0)));

            view.GetCurrentTime = () => TimeSpan.FromSeconds(Player?.CurrentTime.Seconds ?? 0);
            view.InitializeTimer();

            LoadVideo();
        }

        void OnFrameChanged()
        {
            if (IsDead(out var view) || PlayerLayer == null) return;

            Frame = view.GetFrame();
            PlayerLayer.Frame = Bounds;
        }

        void Mute()
        {
            if (IsDead(out var view)) return;

            Player.Muted = view.IsMuted;
            if (view.IsMuted == false)
                Zebble.Device.Audio.AcquireSession();
        }

        void Resume()
        {
            if (IsDead(out _)) return;

            Player?.Play();
            Zebble.Device.Audio.AcquireSession();
        }

        void SeekBeginning()
        {
            if (!IsDead(out _)) Player?.Seek(CMTime.Zero);
        }

        void Play()
        {
            SeekBeginning();
            Resume();
        }

        void Pause()
        {
            if (IsDead(out _)) return;

            Player?.Pause();
        }

        void Stop()
        {
            if (IsDead(out _)) return;

            Player?.Pause();
            SeekBeginning();
        }

        void LoadVideo()
        {
            if (IsDead(out var view)) return;

            if (view.Path.IsEmpty()) return;
            view.LoadedPath = view.Path;

            if (!view.Path.IsUrl()) view.LoadedPath = "file://" + Device.IO.File(view.Path).FullName;

            if (view.AutoBuffer || view.AutoPlay) BufferVideo();

            view.LoadCompleted.Raise().RunInParallel();
            view.OnLoaded();
        }

        void PlayedToEnd()
        {
            if (IsDead(out var view)) return;

            if (view.Loop) Play();
            else view.FinishedPlaying.RaiseOn(Thread.UI);
        }

        void ItemStatusChanged()
        {
            if (IsDead(out var view)) return;

            if (PlayerItem?.Status == AVPlayerItemStatus.ReadyToPlay)
            {
                view.IsReady = true;
                OnReady();
            }
            else view.IsReady = false;
        }

        void InitializePlayer()
        {
            PlayerItem = new AVPlayerItem(Asset);

            DidPlayToEndTimeObservation?.Dispose();
            DidPlayToEndTimeObservation = AVPlayerItem.Notifications.ObserveDidPlayToEndTime(PlayerItem, (_, _) => PlayedToEnd());
            StatusObservation = PlayerItem.AddObserver("status", 0, _ => ItemStatusChanged());

            Player = new AVPlayer(PlayerItem);
            PlayerLayer = AVPlayerLayer.FromPlayer(Player);
            PlayerLayer.VideoGravity = GetStretch();
            PlayerLayer.Frame = Bounds;
            Layer.AddSublayer(PlayerLayer);

            Mute();
        }

        AVLayerVideoGravity GetStretch()
        {
            switch (View.BackgroundImageStretch)
            {
                case Stretch.Fill: return AVLayerVideoGravity.Resize;
                case Stretch.AspectFill: return AVLayerVideoGravity.ResizeAspectFill;
                default: return AVLayerVideoGravity.ResizeAspect;
            }
        }

        void BufferVideo()
        {
            if (IsDead(out var view)) return;

            var nsUrl = view.LoadedPath.ToNsUrl();
            if (nsUrl == null) return; // It's possible for a non-null url, NSUrl return a null value

            try
            {
                Frame = view.GetFrame();
                Asset?.Dispose();
                Asset = AVAsset.FromUrl(nsUrl);

                UIGraphics.BeginImageContext(new CoreGraphics.CGSize(1, 1));

                var track = Asset?.GetTracks(AVMediaTypes.Video)?.FirstOrDefault();

                if (track != null)
                {
                    var size = track.PreferredTransform.TransformSize(track.NaturalSize);
                    view.VideoSize = new Size((float)size.Width, (float)size.Height);
                }

                InitializePlayer();

                UIGraphics.EndImageContext();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToLogString());
            }
        }

        void OnReady()
        {
            if (IsDead(out var view)) return;

            if (view.AutoPlay) Play();

            view.Duration = ((int)PlayerItem.Duration.Seconds).Seconds();

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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                CoreDispose();

                Prepared = null;
                var view = View;
                if (view is not null) view.GetCurrentTime = null;
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

            PlayerItem?.Dispose();
            PlayerItem = null;

            Player?.Dispose();
            Player = null;

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