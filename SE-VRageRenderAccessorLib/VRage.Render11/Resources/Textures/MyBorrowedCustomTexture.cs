using SharpDX.Direct3D11;
using VRageMath;

namespace VRageRenderAccessor.VRage.Render11.Resources.Textures
{
    public class MyBorrowedCustomTexture : IBorrowedCustomTexture
    {
        public object Instance { get; }
        public string Name => IResourceAccessor.GetName(this);
        public Resource Resource => IResourceAccessor.GetResource(this);
        public Vector3I Size3 => IResourceAccessor.GetSize3(this);
        public Vector2I Size => IResourceAccessor.GetSize(this);
        public UnorderedAccessView Uav => ViewBindableAccessor.GetUav(this);
        public ShaderResourceView Srv => ViewBindableAccessor.GetSrv(this);
        public SharpDX.DXGI.Format Format => ITextureAccessor.GetFormat(this);
        public int MipLevels => ITextureAccessor.GetMipLevels(this);

        internal MyBorrowedCustomTexture(object instance)
        {
            Instance = instance;
        }
    }
}