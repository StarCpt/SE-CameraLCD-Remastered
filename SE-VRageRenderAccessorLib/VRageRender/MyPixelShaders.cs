using HarmonyLib;
using SharpDX.Direct3D11;
using System;
using System.Runtime.CompilerServices;
using VRage.Library.Collections;

namespace VRageRenderAccessor.VRageRender
{
    public static class MyPixelShaders
    {
        private static readonly Type _MyPixelShaders = AccessTools.TypeByName("VRageRender.MyPixelShaders");
        private static readonly Func<object> _m_shaders_Getter = _MyPixelShaders.Field("m_shaders").CreateStaticGetter<object>();

        public struct Id
        {
            public int Index;

            public static implicit operator PixelShader(Id id)
            {
                return GetShader(id);
            }
        }

        public static PixelShader GetShader(Id id)
        {
            var list = _m_shaders_Getter.Invoke();
            var castList = Unsafe.As<MyFreeList<MyShaderInfo<PixelShader>>>(list);
            return castList[id.Index].Shader;
        }
    }
}

public struct MyShaderInfo<T>
{
    public T Shader;

    public ShaderInfoId InfoId;

    public string File;
}

public struct ShaderInfoId
{
    public int Index;

    public static readonly ShaderInfoId NULL = new ShaderInfoId
    {
        Index = -1
    };

    public static bool operator ==(ShaderInfoId x, ShaderInfoId y)
    {
        return x.Index == y.Index;
    }

    public static bool operator !=(ShaderInfoId x, ShaderInfoId y)
    {
        return x.Index != y.Index;
    }

    public override bool Equals(object obj)
    {
        if (obj is ShaderInfoId shaderInfoId)
        {
            return Index == shaderInfoId.Index;
        }

        return false;
    }

    public override int GetHashCode()
    {
        return Index.GetHashCode();
    }
}