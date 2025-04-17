namespace VRageRenderAccessor.VRage.Render11.Resources
{
    public interface IBorrowedRtvTexture : IBorrowedSrvTexture, IRtvTexture
    {
        void AddRef();
        void Release();
    }
}