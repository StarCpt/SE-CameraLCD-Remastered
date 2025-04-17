using SharpDX.Direct3D11;
using System;

namespace VRageRenderAccessor.VRage.Render11.Resources.Internal
{
    public class MyBlendState : MyPersistentResource<BlendState, BlendStateDescription>, IBlendStateInternal
    {
        internal MyBlendState(object instance) : base(instance)
        {
        }
    }
}