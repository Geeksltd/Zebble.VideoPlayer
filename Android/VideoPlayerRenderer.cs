namespace Zebble.Plugin.Renderer
{
    using System.ComponentModel;
    using System.Threading.Tasks;
    using Android.Views;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public class VideoPlayerRenderer : INativeRenderer
    {
        AndroidVideo Result;

        public Task<View> Render(Zebble.Renderer renderer)
        {
            Result = new AndroidVideo((VideoPlayer)renderer.View);
            return Task.FromResult<View>(Result);
        }

        public void Dispose()
        {
            Result?.Dispose();
            Result = null;
        }
    }
}