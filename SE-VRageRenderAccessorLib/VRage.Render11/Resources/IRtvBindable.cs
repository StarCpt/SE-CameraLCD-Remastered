using SharpDX.Direct3D11;

namespace VRageRenderAccessor.VRage.Render11.Resources
{
    public interface IRtvBindable : ISrvBindable, IResource
    {
        RenderTargetView Rtv { get; }
    }
}