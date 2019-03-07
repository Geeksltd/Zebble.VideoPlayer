namespace Zebble
{
    using Android.Graphics;
    using Android.Media;
    using Android.Runtime;
    using Android.Views;
    using Android.Widget;
    using System;
    using System.Threading.Tasks;
    using Zebble.Device;

    class AndroidVideo : RelativeLayout, ISurfaceHolderCallback, MediaPlayer.IOnPreparedListener
    {
        SurfaceView VideoSurface;
        MediaPlayer VideoPlayer;

        VideoPlayer View;
        bool surfaceCreated, isVideoCreatedDifferently;
        readonly AsyncEvent SurfaceInitialized = new AsyncEvent();

        public AndroidVideo(IntPtr handle, JniHandleOwnership transfer) : base(UIRuntime.CurrentActivity) => CreateMediaPlayer(null);

        public AndroidVideo(VideoPlayer view) : base(UIRuntime.CurrentActivity) => CreateMediaPlayer(view);

        void CreateMediaPlayer(VideoPlayer view)
        {
            if (view == null)
            {
                isVideoCreatedDifferently = true;
                View = Zebble.VideoPlayer.Instance;
            }
            else Zebble.VideoPlayer.Instance = View = view;

            var @params = CreateLayout();
            CreateSurfceView();

            VideoSurface.LayoutParameters = @params;
        }

        LayoutParams CreateLayout()
        {
            var @params = new LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
            @params.AddRule(LayoutRules.AlignParentTop);
            @params.AddRule(LayoutRules.AlignParentBottom);
            @params.AddRule(LayoutRules.AlignParentLeft);
            @params.AddRule(LayoutRules.AlignParentRight);

            return @params;
        }

        void CreateSurfceView()
        {
            VideoSurface = new SurfaceView(UIRuntime.CurrentActivity);
            VideoSurface.SetBackgroundColor(Android.Graphics.Color.Transparent);
            VideoSurface.Holder.AddCallback(this);
            AddView(VideoSurface);
        }

        public override ViewStates Visibility
        {
            get => base.Visibility;
            set
            {
                if (value == ViewStates.Visible)
                {
                    LayoutParameters = View.GetFrame();
                }
                else if (value == ViewStates.Invisible || value == ViewStates.Gone)
                {
                    LayoutParameters = new FrameLayout.LayoutParams(0, 0);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (VideoPlayer != null)
                {
                    VideoPlayer.Release();
                    VideoPlayer = null;
                }
            }

            base.Dispose(disposing);
        }

        public void SurfaceChanged(ISurfaceHolder holder, [GeneratedEnum] Format format, int width, int height)
        {
        }

        public void SurfaceCreated(ISurfaceHolder holder)
        {
            var surface = holder.Surface;

            if (surface == null || !surface.IsValid) surfaceCreated = false;
            else surfaceCreated = true;

            StartVideo().RunInParallel();
        }

        public void SurfaceDestroyed(ISurfaceHolder holder)
        {
            surfaceCreated = false;
            holder.Surface.Release();

            VideoPlayer = null;
        }

        public void OnPrepared(MediaPlayer mp)
        {
            if (View.AutoPlay)
                mp.Start();
        }

        Task StartVideo()
        {
            if (surfaceCreated == false && isVideoCreatedDifferently == false) return Task.CompletedTask;
            else if (surfaceCreated == false && isVideoCreatedDifferently)
            {
                CreateSurfceView();
                var @params = CreateLayout();
                VideoSurface.LayoutParameters = @params;
            }

            if (VideoPlayer == null)
            {
                try
                {
                    VideoPlayer = new MediaPlayer();
                    VideoPlayer.SetDisplay(VideoSurface.Holder);
                    SetEvents(clear: true);
                }
                catch { }
            }

            if (View.PathChanged.HandlersCount == 0)
            {
                View.PathChanged.HandleOn(Thread.UI, () => StartVideo());
                View.Started.HandleOn(Thread.UI, () => Play());
                View.Paused.HandleOn(Thread.UI, () => Pause());
                View.Resumed.HandleOn(Thread.UI, () => Play());
                View.Stopped.HandleOn(Thread.UI, () => Stop());
                View.SoughtBeginning.HandleOn(Thread.UI, () => SeekBeginning());
            }

            try
            {
                var path = View.Path;
                if (Zebble.Device.IO.IsAbsolute(path)) path = "file://" + path;
                else if (!path.IsUrl()) path = Zebble.Device.IO.AbsolutePath(path);

                VideoPlayer.SetDataSource(path);
                VideoPlayer.SetVideoScalingMode(VideoScalingMode.ScaleToFitWithCropping);
                VideoPlayer.SetOnPreparedListener(this);
                VideoPlayer.Looping = View.Loop;
                VideoPlayer.PrepareAsync();
            }
            catch (Java.Lang.Exception ex)
            {
                Log.Error(ex.ToFullMessage());
            }

            return Task.CompletedTask;
        }

        void Play() => VideoPlayer.Start();

        void Pause()
        {
            if (VideoPlayer.IsPlaying)
                VideoPlayer.Pause();
            else
                Stop();
        }

        void SeekBeginning() => VideoPlayer.SeekTo(0);

        void Stop()
        {
            VideoPlayer.Pause();
            VideoPlayer.SeekTo(0);
        }

        async void OnCompletion(object e, EventArgs args) => await View.FinishedPlaying.RaiseOn(Thread.UI);

        void OnVideoSizeChanged(object e, EventArgs args)
        {
            if (View.VideoSize.Width == 0)
            {
                View.VideoSize = new Size(VideoPlayer.VideoWidth, VideoPlayer.VideoHeight);
                View.LoadCompleted.Raise();
            }
        }

        void SetEvents(bool clear = false)
        {
            if (clear)
            {
                VideoPlayer.Completion -= OnCompletion;
                VideoPlayer.VideoSizeChanged -= OnVideoSizeChanged;
            }

            VideoPlayer.Completion += OnCompletion;
            VideoPlayer.VideoSizeChanged += OnVideoSizeChanged;
        }
    }
}