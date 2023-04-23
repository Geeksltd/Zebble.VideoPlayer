namespace Zebble
{
    using System.Threading.Tasks;
    using Windows.UI.Xaml;

    class VideoPlayerRenderer : INativeRenderer
    {
        UWPVideoViewer Result;

        public Task<FrameworkElement> Render(Renderer renderer)
        {
            Result = new UWPVideoViewer((VideoPlayer)renderer.View);
            return Task.FromResult((FrameworkElement)Result.Result);
        }

        public void Dispose()
        {
            Result?.Dispose();
            Result = null;
        }
    }
}