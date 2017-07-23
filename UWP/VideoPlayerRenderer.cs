namespace Zebble.Plugin.Renderer
{
    using System.ComponentModel;
    using System.Threading.Tasks;
    using Windows.UI.Xaml;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public class VideoPlayerRenderer : ICustomRenderer
    {
        VideoPlayer View;
        FrameworkElement Result;

        public async Task<FrameworkElement> Render(object view)
        {
            View = (VideoPlayer)view;
            return Result = await new UWPVideoViewer(View).Render();
        }

        public void Dispose()
        {
            View = null;
            Result = null;
        }
    }
}