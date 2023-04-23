namespace Zebble
{
    using Olive;
    using System.Threading.Tasks;
    using Windows.UI.Xaml;

    class VideoPlayerRenderer : INativeRenderer
    {
        FrameworkElement Result;

        public Task<FrameworkElement> Render(Renderer renderer)
        {
            return new UWPVideoViewer((VideoPlayer)renderer.View).Render().Get(x => (FrameworkElement)x);
        }

        public void Dispose() => Result = null;
    }
}