namespace Zebble
{
    using System;
    using System.Threading.Tasks;
    using Android.Graphics;
    using Android.Media;
    using Android.Runtime;
    using Android.Views;
    using Android.Widget;
    using Olive;
    using Zebble.Device;
    using static Zebble.VideoPlayer;

    class AndroidVideo : RelativeLayout, ISurfaceHolderCallback, MediaPlayer.IOnPreparedListener, MediaPlayer.IOnVideoSizeChangedListener
    {
        VideoPlayer View;

        SurfaceView VideoSurface;
        MediaPlayer VideoPlayer;
        Preparedhandler Prepared = new Preparedhandler();
        Preparedhandler VideoSurfaceCreate = new Preparedhandler();
        bool IsSurfaceCreated, IsVideoBuffered;

        [Preserve]
        public AndroidVideo(IntPtr handle, JniHandleOwnership transfer) : base(UIRuntime.CurrentActivity) { }

        public AndroidVideo(VideoPlayer view) : base(UIRuntime.CurrentActivity)
        {
            View = view;
            CreateSurfaceView();
            CreateVideoPlayer();

            ScaleX = View.ScaleX;
            ScaleY = View.ScaleY;
            Rotation = View.Rotation;
            RotationX = -View.RotationX;
            RotationY = View.RotationY;

            View.Buffered
                .HandleOn(Thread.UI, () => SafeInvoke(() => { VideoPlayer.PrepareAsync(); IsVideoBuffered = true; }));
            View.PathChanged.HandleOn(Thread.UI, () => SafeInvoke(() => { if (View.AutoPlay) LoadVideo(); }));
            View.Started.HandleOn(Thread.UI, () => SafeInvoke(OnVideoStart));
            View.Paused.HandleOn(Thread.UI, () => SafeInvoke(() => Prepared.Raise(VideoState.Pause)));
            View.Resumed.HandleOn(Thread.UI, () => SafeInvoke(() => Prepared.Raise(VideoState.Play)));
            View.Stopped.HandleOn(Thread.UI, () => SafeInvoke(() => Prepared.Raise(VideoState.Stop)));
            View.SoughtBeginning.HandleOn(Thread.UI, () => SafeInvoke(() => Prepared.Raise(VideoState.SeekToBegining)));
            View.Muted.HandleOn(Thread.UI, Mute);
            View.Seeked.HandleOn(Thread.UI, (position) => VideoPlayer.SeekTo(position.Milliseconds));
            View.GetCurrentTime = () => VideoPlayer.CurrentPosition.Milliseconds();
            View.InitializeTimer();
            Prepared.Handle(HandleStateCommand);
            VideoPlayer.Info += VideoPlayer_Info;
        }

        void CreateSurfaceView()
        {
            VideoSurface = new SurfaceView(UIRuntime.CurrentActivity);
            VideoSurface.Holder.AddCallback(this);
            VideoSurface.LayoutParameters = CreateLayout();
            AddView(VideoSurface);
        }

        void CreateVideoPlayer()
        {
            VideoPlayer = new MediaPlayer();
            VideoPlayer.SetOnPreparedListener(this);
            VideoPlayer.SetOnVideoSizeChangedListener(this);
            VideoPlayer.Completion += OnCompletion;
            VideoPlayer.VideoSizeChanged += OnVideoSizeChanged;
        }

        LayoutParams CreateLayout()
        {
            var result = new LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
            result.AddRule(LayoutRules.CenterInParent);
            return result;
        }

        void ISurfaceHolderCallback.SurfaceChanged(ISurfaceHolder holder, [GeneratedEnum] Format format, int width, int height) { }

        void ISurfaceHolderCallback.SurfaceCreated(ISurfaceHolder holder)
        {
            if (IsDead(out var view)) return;

            IsSurfaceCreated = holder.Surface?.IsValid == true;

            VideoPlayer.SetDisplay(VideoSurface.Holder);

            if (IsSurfaceCreated)
            {
                VideoSurfaceCreate.Handle(_ => LoadVideo());
                if (view.AutoPlay) LoadVideo();
            }
        }

        void ISurfaceHolderCallback.SurfaceDestroyed(ISurfaceHolder holder)
        {
            if (IsDead(out var view)) return;

            view.IsReady = false;
            IsSurfaceCreated = false;
            holder?.Surface?.Release();
        }

        void HandleStateCommand(VideoState result)
        {
            switch (result)
            {
                case VideoState.Play:
                    LoadVideo();
                    break;
                case VideoState.Pause:
                    if (VideoPlayer?.IsPlaying == true) VideoPlayer.Pause();
                    break;
                case VideoState.Stop:
                    VideoPlayer?.Stop();
                    VideoPlayer?.Reset();
                    break;
                case VideoState.SeekToBegining:
                    VideoPlayer?.Reset();
                    break;
                default: break;
            }
        }

        void MediaPlayer.IOnPreparedListener.OnPrepared(MediaPlayer mp)
        {
            if (IsDead(out var view)) return;

            try { mp.SetVideoScalingMode(VideoScalingMode.ScaleToFit); }
            catch (Exception ex) { Log.For(this).Error(ex); }

            mp.Looping = view.Loop;
            view.IsReady = true;
            View.Duration = mp.Duration.Milliseconds();

            if (view.IsMuted) mp.SetVolume(0, 0);
            else mp.SetVolume(1, 1);

            UpdateLayoutSize();

            if (view.AutoBuffer) mp.Start();
        }

        void MediaPlayer.IOnVideoSizeChangedListener.OnVideoSizeChanged(MediaPlayer mp, int width, int height)
            => UpdateLayoutSize();

        void UpdateLayoutSize()
        {
            var contentSize = new Size(VideoPlayer.VideoWidth, VideoPlayer.VideoHeight);
            var frame = new Size(Scale.ToDevice(View.ActualWidth + 2), Scale.ToDevice(View.ActualHeight + 2));

            if (View.BackgroundImageStretch == Stretch.Fill)
                contentSize = frame;

            if (View.BackgroundImageStretch == Stretch.AspectFill)
            {
                double wRatio = frame.Width / contentSize.Width;
                double hRatio = frame.Height / contentSize.Height;
                double multiplier = Math.Max(wRatio, hRatio);
                int w = (int)(contentSize.Width * multiplier);
                int h = (int)(contentSize.Height * multiplier);
                contentSize = new Size(w, h);
            }
            else
                contentSize = contentSize.Scale(10).LimitTo(frame);

            var newParams = VideoSurface.LayoutParameters;
            newParams.Width = (int)contentSize.Width;
            newParams.Height = (int)contentSize.Height;
            VideoSurface.LayoutParameters = newParams;
            SetGravity(GravityFlags.Center | GravityFlags.ClipHorizontal | GravityFlags.ClipVertical);
        }

        void OnVideoStart()
        {
            if (IsDead(out _)) return;

            if (IsVideoBuffered) VideoPlayer.Start();
            else Prepared.Raise(VideoState.Play).GetAwaiter();
        }

        async Task LoadVideo()
        {
            if (IsDead(out var view)) return;

            var source = view.Path;
            if (source.IsEmpty()) return;

            if (View.IsYoutube(source))
                source = await view.LoadYoutube(source);

            View.LoadedPath = source;

            if (!IsSurfaceCreated)
            {
                await VideoSurfaceCreate.Raise(VideoState.Play);
                return;
            }

            if (IO.IsAbsolute(source)) source = "file://" + source;
            else if (!source.IsUrl()) source = "file://" + IO.AbsolutePath(source);

            try
            {
                VideoPlayer.Reset();
                VideoPlayer.SetDataSource(Renderer.Context, Android.Net.Uri.Parse(source));

                if (source.IsUrl() || view.AutoBuffer)
                    VideoPlayer.PrepareAsync();
            }
            catch (Java.Lang.Exception ex)
            {
                Log.For(this).Error(ex, "This error is raised without seemingly affecting anything!");
            }
        }

        void VideoPlayer_Info(object sender, MediaPlayer.InfoEventArgs e)
        {
            View.LoadCompleted.Raise();
            View.OnLoaded();
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

            if (view.IsMuted) VideoPlayer.SetVolume(0, 0);
            else VideoPlayer.SetVolume(1, 1);
        }

        void OnVideoSizeChanged(object sender, EventArgs args)
        {
            if (IsDead(out var view)) return;

            if (sender is MediaPlayer media)
            {
                if (view.VideoSize.Width == 0)
                {
                    view.VideoSize = new Size(media.VideoWidth, media.VideoHeight);
                    view.LoadCompleted?.RaiseOn(Thread.Pool);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && VideoPlayer != null)
            {
                VideoPlayer.Completion -= OnCompletion;
                VideoPlayer.VideoSizeChanged -= OnVideoSizeChanged;
                VideoPlayer.Release();
                VideoPlayer.Dispose();
                VideoPlayer = null;
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