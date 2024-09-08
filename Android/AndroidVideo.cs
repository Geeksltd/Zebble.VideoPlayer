namespace Zebble
{
    using System;
    using Android.Media;
    using Android.OS;
    using Android.Runtime;
    using Android.Widget;
    using Olive;
    using Zebble.Device;

    class AndroidVideo : VideoView, MediaPlayer.IOnPreparedListener, MediaPlayer.IOnVideoSizeChangedListener, MediaPlayer.IOnCompletionListener, MediaPlayer.IOnErrorListener
    {
        VideoPlayer View;
        MediaPlayer Player;
        int LastPosition;

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

            if (OS.IsAtLeast(BuildVersionCodes.O))
                SetAudioFocusRequest(AudioFocus.None);

            // VideoView.setVideoURI will call player.prepareAsync()
            // view.BufferRequested.HandleOn(Thread.UI, () => SafeInvoke(() => Player?.PrepareAsync()));
            view.PathChanged.HandleOn(Thread.UI, () => SafeInvoke(SetPath));
            view.PathNullified.HandleOn(Thread.UI, () => SafeInvoke(OnPathNullified));
            view.Started.HandleOn(Thread.UI, Play);
            view.Paused.HandleOn(Thread.UI, OnPause);
            view.Resumed.HandleOn(Thread.UI, OnResume);
            view.Stopped.HandleOn(Thread.UI, Stop);
            view.SoughtBeginning.HandleOn(Thread.UI, OnSeekBeginning);
            view.Muted.HandleOn(Thread.UI, Mute);
            view.Seeked.HandleOn(Thread.UI, (position) => SafeInvoke(() => SeekTo((int)position.TotalMilliseconds)));
            view.GetCurrentTime = () => SafeInvoke(OnGetCurrentTime);
            view.InitializeTimer();

            Thread.UI.Run(SetPath);
        }

        private TimeSpan? OnGetCurrentTime()
        {
            if (IsPlaying == false) return 0.Seconds();
            return CurrentPosition.Milliseconds();
        }

        void Play()
        {
            if (IsDead(out var view)) return;

            try
            {
                LastPosition = default;

                SetPath();
                if (view.AutoPlay) return;

                Start();
            }
            catch (Exception ex) { Log.For(this).Error(ex); }
        }

        void Stop()
        {
            if (IsDead(out var _)) return;

            try
            {
                LastPosition = default;
                StopPlayback();
                OnSeekBeginning();
            }
            catch (Exception ex) { Log.For(this).Error(ex); }
        }

        void MediaPlayer.IOnPreparedListener.OnPrepared(MediaPlayer mp)
        {
            try
            {
                Player?.SetOnVideoSizeChangedListener(null);

                if (IsDead(out var view)) return;

                Player = mp;
                Player?.SetOnVideoSizeChangedListener(this);

                try
                {
                    Player?.SetVideoScalingMode(view.BackgroundImageStretch == Stretch.Fit
                        ? VideoScalingMode.ScaleToFit
                        : VideoScalingMode.ScaleToFitWithCropping);
                }
                catch (Exception ex) { Log.For(this).Error(ex); }

                Player.Set(x => x.Looping = view.Loop);
                view.IsReady = true;
                view.OnLoaded();

                try { view.Duration = Player?.Duration.Milliseconds(); }
                catch { }

                Mute();

                if (view.AutoPlay || view.AutoBuffer)
                {
                    try { Start(); }
                    catch { }
                }
            }
            catch (Exception ex) { Log.For(this).Error(ex); }
        }

        bool MediaPlayer.IOnErrorListener.OnError(MediaPlayer mp, [GeneratedEnum] MediaError what, int extra)
        {
            Log.For(this).Error("Failed to play a video. Error: " + what + ", Extra: " + extra);
            return true;
        }

        public void OnVideoSizeChanged(MediaPlayer mp, int width, int height)
        {
            if (IsDead(out var view)) return;
            view.VideoSize = new Size(width, height);
            view.LoadCompleted.RaiseOn(Thread.Pool);
        }

        void OnPathNullified() => SetVideoURI(null);

        void SetPath()
        {
            SetOnPreparedListener(null);
            SetOnCompletionListener(null);
            SetOnErrorListener(null);

            if (IsDead(out var view)) return;

            var source = view.Path;
            if (source.IsEmpty()) return;
            view.LoadedPath = source;

            if (IO.IsAbsolute(source)) source = "file://" + source;
            else if (!source.IsUrl()) source = "file://" + IO.AbsolutePath(source);

            try
            {
                SetVideoURI(Android.Net.Uri.Parse(source));

                SetOnPreparedListener(this);
                SetOnCompletionListener(this);
                SetOnErrorListener(this);
            }
            catch (Java.Lang.Exception ex)
            {
                Log.For(this).Error(ex, "This error is raised without seemingly affecting anything!");
            }
        }

        void OnResume()
        {
            if (IsDead(out _)) return;

            try
            {
                SeekTo(LastPosition);
                Start();
            }
            catch (Exception ex)
            {
                Log.For(this).Error(ex);
            }
        }

        void OnPause()
        {
            if (IsDead(out _)) return;

            try
            {
                Pause();
                LastPosition = CurrentPosition;
            }
            catch (Exception ex) { Log.For(this).Error(ex); }
        }

        void OnSeekBeginning()
        {
            if (IsDead(out _)) return;

            try { SeekTo(0); LastPosition = default; }
            catch (Exception ex) { Log.For(this).Error(ex); }
        }

        void SafeInvoke(Action action)
        {
            if (IsDead(out _)) return;

            try { action(); }
            catch (Exception ex) { Log.For(this).Error(ex); }
        }

        T SafeInvoke<T>(Func<T> func)
        {
            if (IsDead(out _)) return default;

            try { return func(); }
            catch (Exception ex) { Log.For(this).Error(ex); }
            return default;
        }

        public void OnCompletion(MediaPlayer mp)
        {
            if (IsDead(out var view)) return;
            view.FinishedPlaying.RaiseOn(Thread.Pool).ConfigureAwait(false);
        }

        void Mute()
        {
            if (IsDead(out var view)) return;

            if (view.IsMuted)
            {
                try
                {
                    SetAudioFocusRequest(AudioFocus.None);
                    Player?.SetVolume(0, 0);
                }
                catch { }
            }
            else
            {
                try
                {
                    SetAudioFocusRequest(AudioFocus.GainTransientMayDuck);
                    Player?.SetVolume(1, 1);
                }
                catch { }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Player will be released by VideoView
                // Player?.SetOnVideoSizeChangedListener(null);
                // Player?.Release();
                // Player?.Dispose();
                Player = null;

                OnPathNullified();
                SetOnPreparedListener(null);
                SetOnCompletionListener(null);
                SetOnErrorListener(null);

                var view = View;
                if (view is not null) view.GetCurrentTime = null;
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
