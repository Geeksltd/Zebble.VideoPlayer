namespace Zebble.Plugin.Renderer
{
    using System;
    using System.ComponentModel;
    using System.Threading.Tasks;
    using UIKit;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public class VideoPlayerRenderer : INativeRenderer
    {
        UIView Result;

        public Task<UIView> Render(Zebble.Renderer renderer)
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