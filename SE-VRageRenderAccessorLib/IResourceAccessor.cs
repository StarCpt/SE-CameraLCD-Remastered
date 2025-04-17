using HarmonyLib;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;
using VRageRenderAccessor.VRage.Render11.Resources;

namespace VRageRenderAccessor
{
    internal static class IResourceAccessor
    {
        private static readonly Type _IResource = AccessTools.TypeByName("VRage.Render11.Resources.IResource");
        private static readonly Func<object, string> _Name_Getter = _IResource.PropertyGetter("Name").CreateInvoker();
        private static readonly Func<object, Resource> _Resource_Getter = _IResource.PropertyGetter("Resource").CreateInvoker();
        private static readonly Func<object, Vector3I> _Size3_Getter = _IResource.PropertyGetter("Size3").CreateInvoker();
        private static readonly Func<object, Vector2I> _Size_Getter = _IResource.PropertyGetter("Size").CreateInvoker();

        public static string GetName(this IResource resource) => _Name_Getter.Invoke(resource.Instance);
        public static Resource GetResource(this IResource resource) => _Resource_Getter.Invoke(resource.Instance);
        public static Vector3I GetSize3(this IResource resource) => _Size3_Getter.Invoke(resource.Instance);
        public static Vector2I GetSize(this IResource resource) => _Size_Getter.Invoke(resource.Instance);
    }
}
