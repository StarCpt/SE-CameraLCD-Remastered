using HarmonyLib;
using System;
using VRageRenderAccessor.VRage.Render11.Resources.Internal;

namespace VRageRenderAccessor.VRage.Render11.Resources
{
    public class MyBlendStateManager
    {
        private static readonly Type _MyBlendStateManager = AccessTools.TypeByName("VRage.Render11.Resources.MyBlendStateManager");
        private static readonly AccessTools.FieldRef<object> _BlendGui                          = AccessTools.StaticFieldRefAccess<object>(_MyBlendStateManager.Field("BlendGui"));
        private static readonly AccessTools.FieldRef<object> _BlendAdditive                     = AccessTools.StaticFieldRefAccess<object>(_MyBlendStateManager.Field("BlendAdditive"));
        private static readonly AccessTools.FieldRef<object> _BlendAtmosphere                   = AccessTools.StaticFieldRefAccess<object>(_MyBlendStateManager.Field("BlendAtmosphere"));
        private static readonly AccessTools.FieldRef<object> _BlendTransparent                  = AccessTools.StaticFieldRefAccess<object>(_MyBlendStateManager.Field("BlendTransparent"));
        private static readonly AccessTools.FieldRef<object> _BlendAlphaPremult                 = AccessTools.StaticFieldRefAccess<object>(_MyBlendStateManager.Field("BlendAlphaPremult"));
        private static readonly AccessTools.FieldRef<object> _BlendAlphaPremultNoAlphaChannel   = AccessTools.StaticFieldRefAccess<object>(_MyBlendStateManager.Field("BlendAlphaPremultNoAlphaChannel"));
        private static readonly AccessTools.FieldRef<object> _BlendReplace                      = AccessTools.StaticFieldRefAccess<object>(_MyBlendStateManager.Field("BlendReplace"));
        private static readonly AccessTools.FieldRef<object> _BlendReplaceNoAlphaChannel        = AccessTools.StaticFieldRefAccess<object>(_MyBlendStateManager.Field("BlendReplaceNoAlphaChannel"));
        private static readonly AccessTools.FieldRef<object> _BlendOutscatter                   = AccessTools.StaticFieldRefAccess<object>(_MyBlendStateManager.Field("BlendOutscatter"));
        private static readonly AccessTools.FieldRef<object> _BlendFactor                       = AccessTools.StaticFieldRefAccess<object>(_MyBlendStateManager.Field("BlendFactor"));
        private static readonly AccessTools.FieldRef<object> _BlendDecalColor                   = AccessTools.StaticFieldRefAccess<object>(_MyBlendStateManager.Field("BlendDecalColor"));
        private static readonly AccessTools.FieldRef<object> _BlendDecalNormal                  = AccessTools.StaticFieldRefAccess<object>(_MyBlendStateManager.Field("BlendDecalNormal"));
        private static readonly AccessTools.FieldRef<object> _BlendDecalNormalColor             = AccessTools.StaticFieldRefAccess<object>(_MyBlendStateManager.Field("BlendDecalNormalColor"));
        private static readonly AccessTools.FieldRef<object> _BlendDecalNormalColorExt          = AccessTools.StaticFieldRefAccess<object>(_MyBlendStateManager.Field("BlendDecalNormalColorExt"));
        private static readonly AccessTools.FieldRef<object> _BlendDecalColorNoPremult          = AccessTools.StaticFieldRefAccess<object>(_MyBlendStateManager.Field("BlendDecalColorNoPremult"));
        private static readonly AccessTools.FieldRef<object> _BlendDecalNormalNoPremult         = AccessTools.StaticFieldRefAccess<object>(_MyBlendStateManager.Field("BlendDecalNormalNoPremult"));
        private static readonly AccessTools.FieldRef<object> _BlendDecalNormalColorNoPremult    = AccessTools.StaticFieldRefAccess<object>(_MyBlendStateManager.Field("BlendDecalNormalColorNoPremult"));
        private static readonly AccessTools.FieldRef<object> _BlendDecalNormalColorExtNoPremult = AccessTools.StaticFieldRefAccess<object>(_MyBlendStateManager.Field("BlendDecalNormalColorExtNoPremult"));
        private static readonly AccessTools.FieldRef<object> _BlendWeightedTransparencyResolve  = AccessTools.StaticFieldRefAccess<object>(_MyBlendStateManager.Field("BlendWeightedTransparencyResolve"));
        private static readonly AccessTools.FieldRef<object> _BlendWeightedTransparency         = AccessTools.StaticFieldRefAccess<object>(_MyBlendStateManager.Field("BlendWeightedTransparency"));

        public static IBlendState BlendGui => new MyBlendState(_BlendGui.Invoke());
        public static IBlendState BlendAdditive => new MyBlendState(_BlendAdditive.Invoke());
        public static IBlendState BlendAtmosphere => new MyBlendState(_BlendAtmosphere.Invoke());
        public static IBlendState BlendTransparent => new MyBlendState(_BlendTransparent.Invoke());
        public static IBlendState BlendAlphaPremult => new MyBlendState(_BlendAlphaPremult.Invoke());
        public static IBlendState BlendAlphaPremultNoAlphaChannel => new MyBlendState(_BlendAlphaPremultNoAlphaChannel.Invoke());
        public static IBlendState BlendReplace => new MyBlendState(_BlendReplace.Invoke());
        public static IBlendState BlendReplaceNoAlphaChannel => new MyBlendState(_BlendReplaceNoAlphaChannel.Invoke());
        public static IBlendState BlendOutscatter => new MyBlendState(_BlendOutscatter.Invoke());
        public static IBlendState BlendFactor => new MyBlendState(_BlendFactor.Invoke());
        public static IBlendState BlendDecalColor => new MyBlendState(_BlendDecalColor.Invoke());
        public static IBlendState BlendDecalNormal => new MyBlendState(_BlendDecalNormal.Invoke());
        public static IBlendState BlendDecalNormalColor => new MyBlendState(_BlendDecalNormalColor.Invoke());
        public static IBlendState BlendDecalNormalColorExt => new MyBlendState(_BlendDecalNormalColorExt.Invoke());
        public static IBlendState BlendDecalColorNoPremult => new MyBlendState(_BlendDecalColorNoPremult.Invoke());
        public static IBlendState BlendDecalNormalNoPremult => new MyBlendState(_BlendDecalNormalNoPremult.Invoke());
        public static IBlendState BlendDecalNormalColorNoPremult => new MyBlendState(_BlendDecalNormalColorNoPremult.Invoke());
        public static IBlendState BlendDecalNormalColorExtNoPremult => new MyBlendState(_BlendDecalNormalColorExtNoPremult.Invoke());
        public static IBlendState BlendWeightedTransparencyResolve => new MyBlendState(_BlendWeightedTransparencyResolve.Invoke());
        public static IBlendState BlendWeightedTransparency => new MyBlendState(_BlendWeightedTransparency.Invoke());
    }
}