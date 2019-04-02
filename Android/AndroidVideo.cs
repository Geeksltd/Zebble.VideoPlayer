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
        VideoPlayer View;

        SurfaceView VideoSurface;
        MediaPlayer VideoPlayer;
        Preparedhandler Prepared = new Preparedhandler();
        bool IsSurfaceCreated;

        public AndroidVideo(IntPtr handle, JniHandleOwnership transfer) : base(UIRuntime.CurrentActivity)
        {
        }

        public AndroidVideo(VideoPlayer view) : base(UIRuntime.CurrentActivity)
        {
            View = view;
            CreateSurfceView();
            CreateVideoPlayer();

            View.Buffered.HandleOn(Thread.UI, () => VideoPlayer?.PrepareAsync());
            View.PathChanged.HandleOn(Thread.UI, () => { if (View.AutoPlay) LoadVideo(); });
            View.Started.HandleOn(Thread.UI, () => Prepared.Raise(VideoState.Play));
            View.Paused.HandleOn(Thread.UI, () => Prepared.Raise(VideoState.Pause));
            View.Resumed.HandleOn(Thread.UI, () => Prepared.Raise(VideoState.Play));
            View.Stopped.HandleOn(Thread.UI, () => Prepared.Raise(VideoState.Stop));
            View.SoughtBeginning.HandleOn(Thread.UI, () => Prepared.Raise(VideoState.SeekToBegining));
            Prepared.Handle(HandleStateCommand);
        }

        void CreateSurfceView()
        {
            VideoSurface = new SurfaceView(UIRuntime.CurrentActivity);
            VideoSurface.Holder.AddCallback(this);
            VideoSurface.LayoutParameters = CreateLayout();
            AddView(VideoSurface);
        }

        void CreateVideoPlayer()
        {
            VideoPlayer = new MediaPlayer { Looping = View.Loop };
            VideoPlayer.SetOnPreparedListener(this);
            VideoPlayer.Completion += OnCompletion;
            VideoPlayer.VideoSizeChanged += OnVideoSizeChanged;
        }

        LayoutParams CreateLayout()
        {
            var result = new LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
            result.AddRule(LayoutRules.AlignParentTop);
            result.AddRule(LayoutRules.AlignParentBottom);
            result.AddRule(LayoutRules.AlignParentLeft);
            result.AddRule(LayoutRules.AlignParentRight);
            return result;
        }

        public override ViewStates Visibility
        {
            get => base.Visibility;
            set
            {
                if (value == ViewStates.Visible)
                    LayoutParameters = View.GetFrame();
                else if (value == ViewStates.Invisible || value == ViewStates.Gone)
                    LayoutParameters = new FrameLayout.LayoutParams(0, 0);
            }
        }

        public void SurfaceChanged(ISurfaceHolder holder, [GeneratedEnum] Format format, int width, int height) { }

        public void SurfaceCreated(ISurfaceHolder holder)
        {
            IsSurfaceCreated = holder.Surface?.IsValid == true;

            VideoPlayer.SetDisplay(VideoSurface.Holder);

            if (View.AutoPlay && IsSurfaceCreated)
                LoadVideo();
        }

        public void SurfaceDestroyed(ISurfaceHolder holder)
        {
            View.IsReady = false;
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

        public void OnPrepared(MediaPlayer mp)
        {
            VideoPlayer.SetVideoScalingMode(VideoScalingMode.ScaleToFitWithCropping);
            View.IsReady = true;
            if (View.AutoPlay) mp.Start();
        }

        void LoadVideo()
        {
            var path = View.Path;
            if (path.LacksValue()) return;

            if (!IsSurfaceCreated)
            {
                Log.Error("Surface is not created when LoadVideo is called!!");
                return;
            }

            if (IO.IsAbsolute(path)) path = "file://" + path;
            else if (!path.IsUrl()) path = IO.AbsolutePath(path);

            try
            {
                VideoPlayer.SetDataSource(path);

                if (!path.IsUrl() || View.AutoBuffer)
                    VideoPlayer.PrepareAsync();
            }
            catch (Java.Lang.Exception ex)
            {
                Log.Error("This error is raised without seemingly affecting anything! " + ex.Message);
            }
        }

        async void OnCompletion(object e, EventArgs args) => await View.FinishedPlaying.RaiseOn(Thread.Pool);

        void OnVideoSizeChanged(object e, EventArgs args)
        {
            var view = View;
            if (view == null || view.IsDisposing) return;
            if (view.VideoSize.Width == 0)
            {
                view.VideoSize = new Size(VideoPlayer?.VideoWidth ?? 0, VideoPlayer?.VideoHeight ?? 0);
                view.LoadCompleted?.RaiseOn(Thread.Pool);
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
            }

            base.Dispose(disposing);
        }
    }
}