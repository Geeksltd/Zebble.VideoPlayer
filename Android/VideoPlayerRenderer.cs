namespace Zebble.Plugin.Renderer
{
    using System.ComponentModel;
    using System.Threading.Tasks;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public class VideoPlayerRenderer : ICustomRenderer
    {
        VideoPlayer View;
        AndroidVideo Result;

        public Task<Android.Views.View> Render(object view)
        {
            View = (VideoPlayer)view;
            Result = new AndroidVideo(View);
            return Task.FromResult((Android.Views.View)Result);
        }

        public void Dispose()
        {
            Result?.Dispose();
            Result = null;
            View = null;
        }
    }
}