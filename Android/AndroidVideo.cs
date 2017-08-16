namespace Zebble
{
    using System;
    using Android.Widget;

    class AndroidVideo : VideoView
    {
        VideoPlayer View;
        MediaController MediaController;

        public AndroidVideo(VideoPlayer view) : base(UIRuntime.CurrentActivity)
        {
            View = view;
            LoadVideo();
        }

        void LoadVideo()
        {
            var path = View.Path;

            MediaController = new MediaController(UIRuntime.CurrentActivity);
            MediaController.SetAnchorView(this);
            SetMediaController(MediaController);

            if (path.IsUrl()) SetVideoURI(Android.Net.Uri.Parse(path));
            else SetVideoPath(Device.IO.AbsolutePath(path));

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
}