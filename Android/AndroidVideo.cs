namespace Zebble
{
    using Android.Runtime;
    using Android.Views;
    using Android.Widget;
    using System;
    using Zebble.Device;

    class AndroidVideo : VideoView
    {
        VideoPlayer View;

        public AndroidVideo(IntPtr handle, JniHandleOwnership transfer) : base(UIRuntime.CurrentActivity) { }

        public AndroidVideo(VideoPlayer view) : base(UIRuntime.CurrentActivity)
        {
            View = view;
            Completion += OnCompletion;

            view.Started.HandleOn(Thread.UI, () => SafeInvoke(Start));
            view.Paused.HandleOn(Thread.UI, () => SafeInvoke(Pause));
            view.Resumed.HandleOn(Thread.UI, () => SafeInvoke(Resume));
            view.Stopped.HandleOn(Thread.UI, () => SafeInvoke(StopPlayback));
            view.SoughtBeginning.HandleOn(Thread.UI, () => SafeInvoke(() => SeekTo(0)));
            view.PathChanged.HandleOn(Thread.UI, () => SafeInvoke(SetSource));
            SetSource();
        }

        void SafeInvoke(Action action)
        {
            if (IsDead(out var view)) return;

            try { action(); }
            catch (Exception ex) { Log.Error(ex); }
        }

        void SetSource()
        {
            if (IsDead(out var view)) return;

            var source = view.Path;
            if (source.LacksValue()) return;

            if (IO.IsAbsolute(source)) source = "file://" + source;
            else if (!source.IsUrl()) source = IO.AbsolutePath(source);
            SetVideoURI(Android.Net.Uri.Parse(source));

            view.WhenShown(() =>
            {
                if (view.AutoPlay) Start();
            });
        }

        async void OnCompletion(object e, EventArgs args)
        {
            if (IsDead(out var view)) return;

            if (view.Loop)
            {
                SeekTo(0);
                Start();
            }
            else await view.FinishedPlaying.RaiseOn(Thread.Pool);
        }

        protected override void OnSizeChanged(int w, int h, int oldw, int oldh)
        {
            base.OnSizeChanged(w, h, oldw, oldh);
            if (IsDead(out var view)) return;

            if (view.VideoSize.Width == 0)
            {
                view.VideoSize = new Size(w, h);
                view.LoadCompleted?.RaiseOn(Thread.Pool);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Completion -= OnCompletion;
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