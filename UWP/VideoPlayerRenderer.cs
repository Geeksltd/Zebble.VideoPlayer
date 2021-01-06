namespace Zebble
{
    using System.Threading.Tasks;
    using Windows.UI.Xaml;
    using Olive;

    class VideoPlayerRenderer : INativeRenderer
    {
        FrameworkElement Result;

        public Task<FrameworkElement> Render(Renderer renderer)
        {
            return new UWPVideoViewer((VideoPlayer)renderer.View).Render()
                .ContinueWith(x => Result = x.GetAlreadyCompletedResult());
        }

        public void Dispose() => Result = null;
    }
}