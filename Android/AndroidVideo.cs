namespace Zebble
{
    using System;
    using Android.Media;
    using Android.OS;
    using Android.Runtime;
    using Android.Widget;
    using Olive;
    using Zebble.Device;

    class AndroidVideo : VideoView, MediaPlayer.IOnPreparedListener, MediaPlayer.IOnErrorListener
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

            view.BufferRequested.HandleOn(Thread.UI, () => SafeInvoke(() => Player?.PrepareAsync()));
            view.PathChanged.HandleOn(Thread.UI, () => SafeInvoke(() => SetPath()));
            view.Started.HandleOn(Thread.UI, Play);
            view.Paused.HandleOn(Thread.UI, OnPause);
            view.Resumed.HandleOn(Thread.UI, OnResume);
            view.Stopped.HandleOn(Thread.UI, Stop);
            view.SoughtBeginning.HandleOn(Thread.UI, OnSeekBeginning);
            view.Muted.HandleOn(Thread.UI, Mute);
            view.Seeked.HandleOn(Thread.UI, (position) => SafeInvoke(() => SeekTo((int)position.TotalMilliseconds)));
            view.GetCurrentTime = () => SafeInvoke(() => CurrentPosition.Milliseconds());
            view.InitializeTimer();

            SetOnPreparedListener(this);
            SetOnErrorListener(this);
            Thread.UI.Run(SetPath);
        }

        void Play()
        {
            if (IsDead(out var view)) return;

            try
            {
                LastPosition = default;

                SetPath();
                if (view.AutoPlay) return;

                Audio.RequestFocus(AudioFocus.GainTransientMayDuck);
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
                Audio.AbandonFocus();
            }
            catch (Exception ex) { Log.For(this).Error(ex); }
        }

        void MediaPlayer.IOnPreparedListener.OnPrepared(MediaPlayer mp)
        {
            try
            {
                if (IsDead(out var view)) return;

                var player = Player;
                if (player is not null)
                {
                    player.VideoSizeChanged -= OnVideoSizeChanged;
                    player.Completion -= OnCompletion;
                    player.Error -= OnErrorOccurred;
                }

                Player = mp;

                try
                {
                    if (view.BackgroundImageStretch == Stretch.Fit) mp.SetVideoScalingMode(VideoScalingMode.ScaleToFit);
                    else mp.SetVideoScalingMode(VideoScalingMode.ScaleToFitWithCropping);
                }
                catch (Exception ex) { Log.For(this).Error(ex); }

                mp.Looping = view.Loop;
                view.IsReady = true;
                view.OnLoaded();

                try { view.Duration = mp.Duration.Milliseconds(); }
                catch { }

                Mute();

                mp.VideoSizeChanged += OnVideoSizeChanged;
                mp.Completion += OnCompletion;
                mp.Error += OnErrorOccurred;

                if (view.AutoPlay)
                {
                    Audio.RequestFocus(AudioFocus.GainTransientMayDuck);
                    try { mp.Start(); }
                    catch { }
                }

                view.LoadCompleted.Raise();
            }
            catch (Exception ex) { Log.For(this).Error(ex); }
        }

        bool MediaPlayer.IOnErrorListener.OnError(MediaPlayer mp, [GeneratedEnum] MediaError what, int extra)
        {
            Log.For(this).Error("Failed to play a video. Error: " + what + ", Extra: " + extra);
            return true;
        }

        void OnVideoSizeChanged(object sender, EventArgs args)
        {
            if (IsDead(out var view)) return;

            var player = Player;
            if (player is null) return;

            try
            {
                if (player.VideoWidth == 0) return;
                view.VideoSize = new Size(player.VideoWidth, player.VideoHeight);
            }
            catch { }

            view.LoadCompleted.RaiseOn(Thread.Pool);
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
                if (view.IsMuted) Audio.AbandonFocus();
                else Audio.RequestFocus(AudioFocus.GainTransientMayDuck);

                SetVideoURI(Android.Net.Uri.Parse(source));

                if (source.IsUrl() || view.AutoBuffer) Start();
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
                Audio.RequestFocus(AudioFocus.GainTransientMayDuck);
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
                Audio.AbandonFocus();
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

        async void OnCompletion(object sender, EventArgs args)
        {
            if (IsDead(out var view)) return;
            await view.FinishedPlaying.RaiseOn(Thread.Pool).ConfigureAwait(false);
            Audio.AbandonFocus();
        }

        void OnErrorOccurred(object sender, MediaPlayer.ErrorEventArgs e)
        {
            if (IsDead(out var view)) return;
            Audio.AbandonFocus();
            Player?.Reset();
        }

        void Mute()
        {
            if (IsDead(out var view)) return;

            if (view.IsMuted)
            {
                Audio.AbandonFocus();

                try { Player.SetVolume(0, 0); }
                catch { }
            }
            else
            {
                Audio.RequestFocus(AudioFocus.GainTransientMayDuck);

                try { Player.SetVolume(1, 1); }
                catch { }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                var vp = Player;
                if (vp != null)
                {
                    Audio.AbandonFocus();

                    vp.Completion -= OnCompletion;
                    vp.Error -= OnErrorOccurred;
                    vp.VideoSizeChanged -= OnVideoSizeChanged;
                    vp.Release();
                    vp.Dispose();

                    SetOnPreparedListener(null);
                    SetOnErrorListener(null);

                    Player = null;
                }

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
