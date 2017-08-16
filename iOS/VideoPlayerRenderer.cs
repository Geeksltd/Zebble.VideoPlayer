namespace Zebble
{
    using System.Threading.Tasks;
    using UIKit;

    class VideoPlayerRenderer : INativeRenderer
    {
        UIView Result;

        public Task<UIView> Render(Renderer renderer)
        {
            Result = new IosVideo((VideoPlayer)renderer.View);
            return Task.FromResult(Result);
        }

        public void Dispose()
        {
            Result?.Dispose();
            Result = null;
        }
    }
}