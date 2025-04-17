using HarmonyLib;
using System;
using System.Diagnostics;

namespace VRageRenderAccessor.VRage.Render11.Resources
{
    [DebuggerDisplay("Name = {Name}")]
    public abstract class MyPersistentResourceBase<TDescription> : IMyPersistentResource<TDescription>
    {
        private delegate void ChangeDescriptionDel(object target, ref TDescription desc);

        private static readonly Type _MyPersistentResourceBase_TDescription = AccessTools.TypeByName("VRage.Render11.Resources.MyPersistentResourceBase`1")
            .MakeGenericType(typeof(TDescription));
        private static readonly dynamic _ChangeDescription = _MyPersistentResourceBase_TDescription.Method("ChangeDescription").CreateInvoker();
        private static readonly AccessTools.FieldRef<object, string> _m_name = _MyPersistentResourceBase_TDescription.FieldRefAccess<string>("m_name");
        private static readonly AccessTools.FieldRef<object, TDescription> _m_description = _MyPersistentResourceBase_TDescription.FieldRefAccess<TDescription>("m_description");

        public object Instance { get; }
        public string Name => _m_name.Invoke(Instance);
        public TDescription Description => _m_description.Invoke(Instance);

        internal MyPersistentResourceBase(object instance)
        {
            Instance = instance;
        }

        public void ChangeDescription(ref TDescription desc) => _ChangeDescription.Invoke(Instance, ref desc);
    }
}