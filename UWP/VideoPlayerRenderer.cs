namespace Zebble.Plugin.Renderer
{
    using System.ComponentModel;
    using System.Threading.Tasks;
    using Windows.UI.Xaml;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public class VideoPlayerRenderer : INativeRenderer
    {
        FrameworkElement Result;

        public async Task<FrameworkElement> Render(Zebble.Renderer renderer)
        {
            return Result = await new UWPVideoViewer((VideoPlayer)renderer.View).Render();
        }

        public void Dispose() => Result = null;
    }
}