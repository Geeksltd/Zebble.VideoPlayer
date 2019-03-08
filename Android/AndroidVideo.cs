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
    using static Zebble.VideoPlayer;

    class AndroidVideo : RelativeLayout, ISurfaceHolderCallback, MediaPlayer.IOnPreparedListener
    {
        SurfaceView VideoSurface;
        MediaPlayer VideoPlayer;

        VideoPlayer View;
        bool surfaceCreated, isVideoCreatedDifferently;
        Preparedhandler Prepared = new Preparedhandler();

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

            Prepared = new Preparedhandler();
            StartVideo().RunInParallel();
        }

        public void SurfaceDestroyed(ISurfaceHolder holder)
        {
            surfaceCreated = false;
            holder.Surface.Release();

            VideoPlayer = null;
            View.IsReady = false;
        }

        public void OnPrepared(MediaPlayer mp)
        {
            View.IsReady = true;
            if (View.AutoPlay)
                mp.Start();

            Prepared.Handle(result =>
            {
                switch (result)
                {
                    case VideoState.Play:
                        Play(mp);
                        break;
                    case VideoState.Pause:
                        Pause(mp);
                        break;
                    case VideoState.Stop:
                        Stop(mp);
                        break;
                    case VideoState.SeekToBegining:
                        SeekBeginning(mp);
                        break;
                    default:
                        break;
                }
            });
        }

        Task StartVideo()
        {
            if (string.IsNullOrEmpty(View.Path)) return Task.CompletedTask;
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
                View.Buffered.HandleOn(Thread.UI, () => VideoPlayer?.PrepareAsync());
                View.PathChanged.HandleOn(Thread.UI, () => StartVideo());
                View.Started.HandleOn(Thread.UI, () => Prepared.Raise(VideoState.Play));
                View.Paused.HandleOn(Thread.UI, () => Prepared.Raise(VideoState.Pause));
                View.Resumed.HandleOn(Thread.UI, () => Prepared.Raise(VideoState.Play));
                View.Stopped.HandleOn(Thread.UI, () => Prepared.Raise(VideoState.Stop));
                View.SoughtBeginning.HandleOn(Thread.UI, () => Prepared.Raise(VideoState.SeekToBegining));
            }

            try
            {
                var path = View.Path;
                if (IO.IsAbsolute(path)) path = "file://" + path;
                else if (!path.IsUrl()) path = IO.AbsolutePath(path);

                VideoPlayer.SetDataSource(path);
                VideoPlayer.SetVideoScalingMode(VideoScalingMode.ScaleToFitWithCropping);
                VideoPlayer.SetOnPreparedListener(this);
                VideoPlayer.Looping = View.Loop;
                if (path.IsUrl() && View.AutoBuffer)
                    VideoPlayer.PrepareAsync();
                else if (!path.IsUrl())
                    VideoPlayer.PrepareAsync();
            }
            catch (Java.Lang.Exception ex)
            {
                Log.Error(ex.ToFullMessage());
            }

            return Task.CompletedTask;
        }

        void Play(MediaPlayer mp) => mp.Start();

        void Pause(MediaPlayer mp)
        {
            if (mp.IsPlaying)
                mp.Pause();
            else
                Stop(mp);
        }

        void SeekBeginning(MediaPlayer mp) => mp.SeekTo(0);

        void Stop(MediaPlayer mp)
        {
            mp.Pause();
            mp.SeekTo(0);
        }

        async void OnCompletion(object e, EventArgs args) => await View.FinishedPlaying.RaiseOn(Thread.UI);

        void OnVideoSizeChanged(object e, EventArgs args)
        {
            if (View.VideoSize.Width == 0)
            {
                View.VideoSize = new Size(VideoPlayer?.VideoWidth ?? 0, VideoPlayer?.VideoHeight ?? 0);
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