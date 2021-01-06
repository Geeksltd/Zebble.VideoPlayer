namespace Zebble
{
    using System.Threading.Tasks;
    using Windows.UI.Xaml;

    class VideoPlayerRenderer : INativeRenderer
    {
        FrameworkElement Result;

        public async Task<FrameworkElement> Render(Renderer renderer)
        {
            return Result = await new UWPVideoViewer((VideoPlayer)renderer.View).Render();
        }

        public void Dispose() => Result = null;
    }
}