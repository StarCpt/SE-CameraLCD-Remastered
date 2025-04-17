using SharpDX.DXGI;

namespace VRageRenderAccessor.VRage.Render11.Resources
{
    public interface ITexture : ISrvBindable, IResource, IPrivateObjectWrapper
    {
        Format Format { get; }
        int MipLevels { get; }
    }
}