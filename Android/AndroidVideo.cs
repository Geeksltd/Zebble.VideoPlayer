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
        bool surfaceCreated;

        public AndroidVideo(VideoPlayer view) : base(UIRuntime.CurrentActivity)
        {
            View = view;

            var @params = new LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
            @params.AddRule(LayoutRules.AlignParentTop);
            @params.AddRule(LayoutRules.AlignParentBottom);
            @params.AddRule(LayoutRules.AlignParentLeft);
            @params.AddRule(LayoutRules.AlignParentRight);

            VideoSurface = new SurfaceView(UIRuntime.CurrentActivity);
            VideoSurface.SetBackgroundColor(Android.Graphics.Color.Transparent);
            VideoSurface.Holder.AddCallback(this);
            VideoSurface.LayoutParameters = @params;
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
            surfaceCreated = true;
            StartVideo().RunInParallel();
        }

        private Task StartVideo()
        {
            if (surfaceCreated == false)
                return Task.CompletedTask;

            if (VideoPlayer == null)
            {
                VideoPlayer = new MediaPlayer();
                VideoPlayer.SetDisplay(VideoSurface.Holder);
            }

            if (View.PathChanged.HandlersCount == 0)
            {
                View.PathChanged.HandleOn(Thread.UI, () => StartVideo());
                View.Started.HandleOn(Thread.UI, () => Play());
                View.Paused.HandleOn(Thread.UI, () => Pause());
                View.Resumed.HandleOn(Thread.UI, () => Play());
                View.Stopped.HandleOn(Thread.UI, () => Stop());

                VideoPlayer.Completion += (e, args) => View.FinishedPlaying.RaiseOn(Thread.UI);
            }

            try
            {
                VideoPlayer.SetDataSource(View.Path);
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

        public void SurfaceDestroyed(ISurfaceHolder holder)
        {
        }

        public void OnPrepared(MediaPlayer mp)
        {
            if (View.AutoPlay)
                VideoPlayer.Start();
        }

        void Play() => VideoPlayer.Start();
                       
        void Pause()
        {
            if (VideoPlayer.IsPlaying)
                VideoPlayer.Pause();
            else
                Stop();
        }

        void Stop()
        {
            VideoPlayer.Pause();
            VideoPlayer.SeekTo(0);
        }
    }
}