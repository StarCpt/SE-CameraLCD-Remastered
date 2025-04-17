using HarmonyLib;
using System;

namespace VRageRenderAccessor.VRage.Render11.Resources
{
    public abstract class MyPersistentResource<TResource, TDescription> : MyPersistentResourceBase<TDescription> where TResource : IDisposable
    {
        private static readonly Type _MyPersistentResource_TResource_TDescription = AccessTools.TypeByName("VRage.Render11.Resources.MyPersistentResource`2")
            .MakeGenericType(typeof(TResource), typeof(TDescription));
        private static readonly AccessTools.FieldRef<object, TResource> _m_resource = _MyPersistentResource_TResource_TDescription.FieldRefAccess<TResource>("m_resource");

        public TResource Resource => _m_resource(Instance);

        internal MyPersistentResource(object instance) : base(instance)
        {
        }
    }
}