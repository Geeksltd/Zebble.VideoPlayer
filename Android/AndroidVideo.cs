namespace Zebble
{
    using System;
    using Android.Media;
    using Android.Runtime;
    using Android.Widget;
    using Olive;
    using Zebble.Device;

    class AndroidVideo : VideoView, MediaPlayer.IOnPreparedListener
    {
        VideoPlayer View;
        MediaPlayer Player;

        [Preserve]
        public AndroidVideo(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer) { }

        public AndroidVideo(VideoPlayer view) : base(UIRuntime.CurrentActivity)
        {
            View = view;

            ScaleX = view.ScaleX;
            ScaleY = view.ScaleY;
            Rotation = view.Rotation;
            RotationX = -view.RotationX;
            RotationY = view.RotationY;

            view.Buffered.HandleOn(Thread.UI, () => SafeInvoke(() => { Player?.PrepareAsync(); }));
            view.PathChanged.HandleOn(Thread.UI, () => SafeInvoke(() => SetPath()));
            view.Started.HandleOn(Thread.UI, () => SafeInvoke(Play));
            view.Paused.HandleOn(Thread.UI, () => SafeInvoke(Pause));
            view.Resumed.HandleOn(Thread.UI, () => SafeInvoke(Resume));
            view.Stopped.HandleOn(Thread.UI, () => SafeInvoke(Stop));
            view.SoughtBeginning.HandleOn(Thread.UI, () => SafeInvoke(() => SeekTo(0)));
            view.Muted.HandleOn(Thread.UI, Mute);
            view.Seeked.HandleOn(Thread.UI, (position) => SeekTo((int)position.TotalMilliseconds));
            view.GetCurrentTime = () => CurrentPosition.Milliseconds();
            view.InitializeTimer();

            SetOnPreparedListener(this);
            SetPath();
        }

        void Play()
        {
            if (IsDead(out var view)) return;
            SetPath();
            if (!view.AutoPlay) Start();
        }

        void Stop()
        {
            if (IsDead(out var view)) return;
            StopPlayback();
            SeekTo(0);
        }

        void MediaPlayer.IOnPreparedListener.OnPrepared(MediaPlayer mp)
        {
            if (IsDead(out var view)) return;
            Player = mp;

            try
            {
                if (view.BackgroundImageStretch == Stretch.Fit) mp.SetVideoScalingMode(VideoScalingMode.ScaleToFit);
                else mp.SetVideoScalingMode(VideoScalingMode.ScaleToFitWithCropping);
            }
            catch (Exception ex) { Log.For(this).Error(ex); }

            mp.Looping = view.Loop;
            view.IsReady = true;
            view.LoadCompleted.Raise();
            view.OnLoaded();
            view.Duration = mp.Duration.Milliseconds();

            if (view.IsMuted) mp.SetVolume(0, 0);
            else mp.SetVolume(1, 1);

            mp.VideoSizeChanged += OnVideoSizeChanged;
            mp.Completion += OnCompletion;

            if (view.AutoPlay) mp.Start();
        }

        void OnVideoSizeChanged(object sender, EventArgs args)
        {
            if (IsDead(out var view)) return;
            if (Player is null) return;

            if (Player.VideoWidth > 0)
            {
                view.VideoSize = new Size(Player.VideoWidth, Player.VideoHeight);
                view.LoadCompleted?.RaiseOn(Thread.Pool);
            }
        }

        void SetPath()
        {
            if (IsDead(out var view)) return;

            var source = view.Path;
            if (source.IsEmpty()) return;
            view.LoadedPath = source;

            if (IO.IsAbsolute(source)) source = "file://" + source;
            else if (!source.IsUrl()) source = "file://" + IO.AbsolutePath(source);

            try
            {
                SetVideoURI(Android.Net.Uri.Parse(source));
                if (source.IsUrl() || view.AutoBuffer) Player?.PrepareAsync();
            }
            catch (Java.Lang.Exception ex)
            {
                Log.For(this).Error(ex, "This error is raised without seemingly affecting anything!");
            }
        }

        void SafeInvoke(Action action)
        {
            if (IsDead(out _)) return;

            try { action(); }
            catch (Exception ex) { Log.For(this).Error(ex); }
        }

        async void OnCompletion(object sender, EventArgs args)
        {
            if (IsDead(out var view)) return;
            await view.FinishedPlaying.RaiseOn(Thread.Pool);
        }

        void Mute(VideoPlayer currentView)
        {
            if (IsDead(out var view)) view = currentView;

            if (view.IsMuted) Player.SetVolume(0, 0);
            else Player.SetVolume(1, 1);
        }

        protected override void Dispose(bool disposing)
        {
            var vp = Player;
            if (disposing && vp != null)
            {
                vp.Completion -= OnCompletion;
                vp.VideoSizeChanged -= OnVideoSizeChanged;
                vp.Release();
                vp.Dispose();
                Player = null;
                View.GetCurrentTime = null;
                View = null;
            }

            base.Dispose(disposing);
        }

        [EscapeGCop("In this case an out parameter can improve the code.")]
        public bool IsDead(out VideoPlayer result)
        {
            result = View;
            if (result == null) return true;
            return result.IsDisposing;
        }
    }
}
