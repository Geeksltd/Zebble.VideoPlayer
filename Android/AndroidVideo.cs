namespace Zebble
{
    using System;
    using Android.Media;
    using Android.Widget;
    using Android.Views;
    using Zebble.AndroidOS;
    using Android.Graphics;
    using Android.Runtime;
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

            View.PathChanged.HandleOn(Thread.UI, () => StartVideo());
            View.Started.HandleOn(Thread.UI, () => VideoPlayer.Start());
            View.Paused.HandleOn(Thread.UI, () => VideoPlayer.Pause());
            View.Resumed.HandleOn(Thread.UI, () => VideoPlayer.Start());
            View.Stopped.HandleOn(Thread.UI, () => VideoPlayer.Stop());
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
                if(VideoPlayer!=null)
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
            StartVideo();
        }

        private void StartVideo()
        {
            if (surfaceCreated == false)
                return;

            if (VideoPlayer == null)
            {
                VideoPlayer = new MediaPlayer();
                VideoPlayer.SetDisplay(VideoSurface.Holder);
            }

            try
            {
                VideoPlayer.SetDataSource(View.Path);
                VideoPlayer.SetVideoScalingMode(VideoScalingMode.ScaleToFitWithCropping);
                VideoPlayer.SetOnPreparedListener(this);
                VideoPlayer.Looping = View.Loop;
                VideoPlayer.Prepare();
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToFullMessage());
            }
        }

        public void SurfaceDestroyed(ISurfaceHolder holder)
        {
        }

        public void OnPrepared(MediaPlayer mp)
        {
            if (View.AutoPlay)
                VideoPlayer.Start();
        }

    }
}