namespace Zebble
{
    using System;
    using Android.Media;
    using Android.Widget;
    using Android.Views;
    using Zebble.AndroidOS;

    class AndroidVideo : RelativeLayout
    {
        VideoPlayer View;
        MediaController MediaController;

        VideoView Video;

        public AndroidVideo(VideoPlayer view) : base(UIRuntime.CurrentActivity)
        {
            View = view;

            Video = new VideoView(UIRuntime.CurrentActivity);
            var @params = new RelativeLayout.LayoutParams(FrameLayout.LayoutParams.FillParent, FrameLayout.LayoutParams.FillParent);
            @params.AddRule(LayoutRules.AlignParentTop);
            @params.AddRule(LayoutRules.AlignParentBottom);
            @params.AddRule(LayoutRules.AlignParentLeft);
            @params.AddRule(LayoutRules.AlignParentRight);
            Video.LayoutParameters = @params;

            View.PathChanged.HandleOn(Thread.UI, () => LoadVideo());
            View.Started.HandleOn(Thread.UI, () => Video.Start());
            View.Paused.HandleOn(Thread.UI, () => Video.Pause());
            View.Resumed.HandleOn(Thread.UI, () => Video.Resume());
            View.Stopped.HandleOn(Thread.UI, () => Video.StopPlayback());

            LoadVideo();

            AddView(Video);
        }

        void LoadVideo()
        {
            var path = View.Path;

            MediaController = new MediaController(UIRuntime.CurrentActivity);
            MediaController.SetAnchorView(this);

            if (View.ShowControls)
                Video.SetMediaController(MediaController);

            if (path.IsUrl()) Video.SetVideoURI(Android.Net.Uri.Parse(path));
            else Video.SetVideoPath(Device.IO.AbsolutePath(path));

            Video.SetOnPreparedListener(new MediaPlayerDelegate { Loop = View.Loop });

            Video.Start();
            Video.SetZOrderOnTop(onTop: true);
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
                Video.Pause();
                Video.StopPlayback();
                MediaController?.Dispose();
                MediaController = null;
                View = null;
            }

            base.Dispose(disposing);
        }
    }

    public class MediaPlayerDelegate : Java.Lang.Object, MediaPlayer.IOnPreparedListener
    {
        public bool Loop;
        public void OnPrepared(MediaPlayer mp)
        {
            mp.Looping = Loop;
        }
    }
}