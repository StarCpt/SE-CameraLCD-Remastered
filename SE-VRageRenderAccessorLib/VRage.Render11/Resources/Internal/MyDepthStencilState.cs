using SharpDX.Direct3D11;

namespace VRageRenderAccessor.VRage.Render11.Resources.Internal
{
    public class MyDepthStencilState : MyPersistentResource<DepthStencilState, DepthStencilStateDescription>, IDepthStencilStateInternal
    {
        internal MyDepthStencilState(object instance) : base(instance)
        {
        }
    }
}
