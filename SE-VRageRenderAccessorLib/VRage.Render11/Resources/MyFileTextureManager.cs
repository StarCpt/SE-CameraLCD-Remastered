using HarmonyLib;
using System;
using System.Collections;
using System.Reflection;
using VRageRenderAccessor.VRage.Render11.Resources.Internal;

namespace VRageRenderAccessor.VRage.Render11.Resources
{
    public class MyFileTextureManager : IPrivateObjectWrapper
    {
        private static readonly Type _MyFileTextureManager = AccessTools.TypeByName("VRage.Render11.Resources.MyFileTextureManager");
        private static readonly AccessTools.FieldRef<object, IDictionary> _m_generatedTextures = _MyFileTextureManager.FieldRefAccess<IDictionary>("m_generatedTextures");
        private static readonly MethodInfo _TryGetTexture1 = _MyFileTextureManager.Method(
            "TryGetTexture", new Type[] { typeof(string), AccessTools.TypeByName("VRage.Render11.Resources.ITexture").MakeByRefType(), });
        private static readonly MethodInfo _TryGetTexture2 = _MyFileTextureManager.Method(
            "TryGetTexture", new Type[] { typeof(string), AccessTools.TypeByName("VRage.Render11.Resources.IUserGeneratedTexture").MakeByRefType(), });

        public object Instance { get; }
        public IDictionary m_generatedTextures => _m_generatedTextures.Invoke(Instance);

        internal MyFileTextureManager(object instance)
        {
            Instance = instance;
        }

        public bool TryGetTexture(string name, out ITexture texture)
        {
            throw new NotImplementedException();
            object[] parameters = { name, null, };
            bool success = (bool)_TryGetTexture1.Invoke(Instance, parameters);
            //texture = success ? new IUserGeneratedTextureWrapper(parameters[1]) : null;
            return success;
        }

        public bool TryGetTexture(string name, out MyUserGeneratedTexture texture)
        {
            object[] parameters = { name, null, };
            bool success = (bool)_TryGetTexture2.Invoke(Instance, parameters);
            texture = success ? new MyUserGeneratedTexture(parameters[1]) : null;
            return success;
        }
    }
}