using SharpDX.Direct3D11;
using VRageMath;

namespace VRageRenderAccessor.VRage.Render11.Resources
{
    public interface IResource : IPrivateObjectWrapper
    {
        string Name { get; }
        Resource Resource { get; }
        Vector2I Size { get; }
        Vector3I Size3 { get; }
    }
}
