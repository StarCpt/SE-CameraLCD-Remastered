using HarmonyLib;
using System;
using VRageRenderAccessor.VRage.Render11.Resources.Internal;

namespace VRageRenderAccessor.VRage.Render11.Resources
{
    public class MyDepthStencilStateManager
    {
        private static readonly Type _MyDepthStencilStateManager = AccessTools.TypeByName("VRage.Render11.Resources.MyDepthStencilStateManager");
        private static readonly AccessTools.FieldRef<object> _DepthTestWrite                   = AccessTools.StaticFieldRefAccess<object>(_MyDepthStencilStateManager.Field("DepthTestWrite"));
        private static readonly AccessTools.FieldRef<object> _DepthTestReadOnly                = AccessTools.StaticFieldRefAccess<object>(_MyDepthStencilStateManager.Field("DepthTestReadOnly"));
        private static readonly AccessTools.FieldRef<object> _DepthTestReadOnlyInverse         = AccessTools.StaticFieldRefAccess<object>(_MyDepthStencilStateManager.Field("DepthTestReadOnlyInverse"));
        private static readonly AccessTools.FieldRef<object> _DepthTestPassReadOnly            = AccessTools.StaticFieldRefAccess<object>(_MyDepthStencilStateManager.Field("DepthTestPassReadOnly"));
        private static readonly AccessTools.FieldRef<object> _IgnoreDepthStencil               = AccessTools.StaticFieldRefAccess<object>(_MyDepthStencilStateManager.Field("IgnoreDepthStencil"));
        private static readonly AccessTools.FieldRef<object> _MarkEdgeInStencil                = AccessTools.StaticFieldRefAccess<object>(_MyDepthStencilStateManager.Field("MarkEdgeInStencil"));
        private static readonly AccessTools.FieldRef<object> _WriteHighlightStencil            = AccessTools.StaticFieldRefAccess<object>(_MyDepthStencilStateManager.Field("WriteHighlightStencil"));
        private static readonly AccessTools.FieldRef<object> _WriteOverlappingHighlightStencil = AccessTools.StaticFieldRefAccess<object>(_MyDepthStencilStateManager.Field("WriteOverlappingHighlightStencil"));
        private static readonly AccessTools.FieldRef<object> _TestHighlightOuterStencil        = AccessTools.StaticFieldRefAccess<object>(_MyDepthStencilStateManager.Field("TestHighlightOuterStencil"));
        private static readonly AccessTools.FieldRef<object> _TestHighlightInnerStencil        = AccessTools.StaticFieldRefAccess<object>(_MyDepthStencilStateManager.Field("TestHighlightInnerStencil"));
        private static readonly AccessTools.FieldRef<object> _TestEdgeStencil                  = AccessTools.StaticFieldRefAccess<object>(_MyDepthStencilStateManager.Field("TestEdgeStencil"));
        private static readonly AccessTools.FieldRef<object> _TestDepthAndEdgeStencil          = AccessTools.StaticFieldRefAccess<object>(_MyDepthStencilStateManager.Field("TestDepthAndEdgeStencil"));
        private static readonly AccessTools.FieldRef<object> _StereoDefaultDepthState          = AccessTools.StaticFieldRefAccess<object>(_MyDepthStencilStateManager.Field("StereoDefaultDepthState"));
        private static readonly AccessTools.FieldRef<object> _StereoStencilMask                = AccessTools.StaticFieldRefAccess<object>(_MyDepthStencilStateManager.Field("StereoStencilMask"));
        private static readonly AccessTools.FieldRef<object> _StereoDepthTestReadOnly          = AccessTools.StaticFieldRefAccess<object>(_MyDepthStencilStateManager.Field("StereoDepthTestReadOnly"));
        private static readonly AccessTools.FieldRef<object> _StereoDepthTestWrite             = AccessTools.StaticFieldRefAccess<object>(_MyDepthStencilStateManager.Field("StereoDepthTestWrite"));
        private static readonly AccessTools.FieldRef<object> _StereoIgnoreDepthStencil         = AccessTools.StaticFieldRefAccess<object>(_MyDepthStencilStateManager.Field("StereoIgnoreDepthStencil"));

        public static IDepthStencilState DepthTestWrite => new MyDepthStencilState(_DepthTestWrite.Invoke());
        public static IDepthStencilState DepthTestReadOnly => new MyDepthStencilState(_DepthTestReadOnly.Invoke());
        public static IDepthStencilState DepthTestReadOnlyInverse => new MyDepthStencilState(_DepthTestReadOnlyInverse.Invoke());
        public static IDepthStencilState DepthTestPassReadOnly => new MyDepthStencilState(_DepthTestPassReadOnly.Invoke());
        public static IDepthStencilState IgnoreDepthStencil => new MyDepthStencilState(_IgnoreDepthStencil.Invoke());
        public static IDepthStencilState MarkEdgeInStencil => new MyDepthStencilState(_MarkEdgeInStencil.Invoke());
        public static IDepthStencilState WriteHighlightStencil => new MyDepthStencilState(_WriteHighlightStencil.Invoke());
        public static IDepthStencilState WriteOverlappingHighlightStencil => new MyDepthStencilState(_WriteOverlappingHighlightStencil.Invoke());
        public static IDepthStencilState TestHighlightOuterStencil => new MyDepthStencilState(_TestHighlightOuterStencil.Invoke());
        public static IDepthStencilState TestHighlightInnerStencil => new MyDepthStencilState(_TestHighlightInnerStencil.Invoke());
        public static IDepthStencilState TestEdgeStencil => new MyDepthStencilState(_TestEdgeStencil.Invoke());
        public static IDepthStencilState TestDepthAndEdgeStencil => new MyDepthStencilState(_TestDepthAndEdgeStencil.Invoke());
        //public static IDepthStencilState[] MarkIfInsideCascade;
        //public static IDepthStencilState[] MarkIfInsideCascadeOld;
        public static IDepthStencilState StereoDefaultDepthState => new MyDepthStencilState(_StereoDefaultDepthState.Invoke());
        public static IDepthStencilState StereoStencilMask => new MyDepthStencilState(_StereoStencilMask.Invoke());
        public static IDepthStencilState StereoDepthTestReadOnly => new MyDepthStencilState(_StereoDepthTestReadOnly.Invoke());
        public static IDepthStencilState StereoDepthTestWrite => new MyDepthStencilState(_StereoDepthTestWrite.Invoke());
        public static IDepthStencilState StereoIgnoreDepthStencil => new MyDepthStencilState(_StereoIgnoreDepthStencil.Invoke());
    }
}
