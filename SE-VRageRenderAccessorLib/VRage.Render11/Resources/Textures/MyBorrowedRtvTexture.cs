using SharpDX.Direct3D11;
using VRageMath;

namespace VRageRenderAccessor.VRage.Render11.Resources.Textures
{
    public class MyBorrowedRtvTexture : IBorrowedRtvTexture
    {
        public object Instance { get; }
        public string Name => IResourceAccessor.GetName(this);
        public Resource Resource => IResourceAccessor.GetResource(this);
        public Vector3I Size3 => IResourceAccessor.GetSize3(this);
        public Vector2I Size => IResourceAccessor.GetSize(this);
        public RenderTargetView Rtv => ViewBindableAccessor.GetRtv(this);
        public ShaderResourceView Srv => ViewBindableAccessor.GetSrv(this);
        public SharpDX.DXGI.Format Format => ITextureAccessor.GetFormat(this);
        public int MipLevels => ITextureAccessor.GetMipLevels(this);

        internal MyBorrowedRtvTexture(object instance)
        {
            Instance = instance;
        }

        public void AddRef() => IBorrowedSrvTextureAccessor.AddRef(this);
        public void Release() => IBorrowedSrvTextureAccessor.Release(this);
    }
}