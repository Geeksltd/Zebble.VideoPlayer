namespace Zebble
{
    using System;
    using Android.Media;
    using Android.Widget;

    class AndroidVideo : VideoView
    {
        VideoPlayer View;
        MediaController MediaController;

        public AndroidVideo(VideoPlayer view) : base(UIRuntime.CurrentActivity)
        {
            View = view;

            View.PathChanged.HandleOn(Thread.UI, () => LoadVideo());
            View.Started.HandleOn(Thread.UI, () => Start());
            View.Paused.HandleOn(Thread.UI, () => Pause());
            View.Resumed.HandleOn(Thread.UI, () => Resume());
            View.Stopped.HandleOn(Thread.UI, () => StopPlayback());

            LoadVideo();
        }

        void LoadVideo()
        {
            var path = View.Path;

            MediaController = new MediaController(UIRuntime.CurrentActivity);
            MediaController.SetAnchorView(this);

            if (View.ShowControls)
                SetMediaController(MediaController);

            if (path.IsUrl()) SetVideoURI(Android.Net.Uri.Parse(path));
            else SetVideoPath(Device.IO.AbsolutePath(path));

            SetOnPreparedListener(new MediaPlayerDelegate { Loop = View.Loop });

            Start();
            SetZOrderOnTop(onTop: true);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Pause();
                StopPlayback();
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