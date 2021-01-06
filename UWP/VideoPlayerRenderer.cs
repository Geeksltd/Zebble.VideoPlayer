namespace Zebble
{
    using System.Threading.Tasks;
    using Windows.UI.Xaml;

    class VideoPlayerRenderer : INativeRenderer
    {
        FrameworkElement Result;

        public Task<FrameworkElement> Render(Renderer renderer)
        {
            Result = new UWPVideoViewer((VideoPlayer)renderer.View).Render().GetAwaiter().GetResult();
            return Task.FromResult(Result);
        }

        public void Dispose() => Result = null;
    }
}