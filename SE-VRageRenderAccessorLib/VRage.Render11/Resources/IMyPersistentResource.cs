namespace VRageRenderAccessor.VRage.Render11.Resources
{
    public interface IMyPersistentResource<TDescription> : IPrivateObjectWrapper
    {
        string Name { get; }
        TDescription Description { get; }
        void ChangeDescription(ref TDescription desc);
    }
}