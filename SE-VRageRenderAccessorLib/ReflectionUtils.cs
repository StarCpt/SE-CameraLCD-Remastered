using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace VRageRenderAccessor
{
    public static class ReflectionUtils
    {
        private static readonly MethodInfo _fieldGetterHelper = typeof(ReflectionUtils).Method(nameof(MagicFieldGetterHelper));
        private static readonly MethodInfo _staticFieldGetterHelper = typeof(ReflectionUtils).Method(nameof(MagicStaticFieldGetterHelper));

        public static AccessTools.FieldRef<T> StaticFieldRefAccess<T>(this Type type, string fieldName)
        {
            return AccessTools.StaticFieldRefAccess<T>(type.Field(fieldName));
        }

        public static Func<TTarget, TReturn> CreateGetter<TTarget, TReturn>(this FieldInfo field)
        {
            MethodInfo constructedHelper = _fieldGetterHelper.MakeGenericMethod(field.DeclaringType, field.FieldType, typeof(TReturn));
            return (Func<TTarget, TReturn>)constructedHelper.Invoke(null, new object[] { field });
        }

        public static Func<TReturn> CreateStaticGetter<TReturn>(this FieldInfo field)
        {
            MethodInfo constructedHelper = _staticFieldGetterHelper.MakeGenericMethod(field.FieldType, typeof(TReturn));
            return (Func<TReturn>)constructedHelper.Invoke(null, new object[] { field });
        }

        private static Func<object, TDelegateReturn> MagicFieldGetterHelper<TTarget, TReturn, TDelegateReturn>(FieldInfo field) where TReturn : TDelegateReturn
        {
            if (!field.FieldType.IsAssignableTo(typeof(TReturn)))
                throw new Exception();

            string methodName = field.ReflectedType.FullName + ".get_" + field.Name;
            DynamicMethod getterMethod = new DynamicMethod(methodName, field.FieldType, new Type[] { field.DeclaringType }, true);
            ILGenerator ilGen = getterMethod.GetILGenerator();
            ilGen.Emit(OpCodes.Ldarg_0);
            ilGen.Emit(OpCodes.Ldfld, field);
            ilGen.Emit(OpCodes.Ret);

            var func = (Func<TTarget, TReturn>)getterMethod.CreateDelegate(typeof(Func<TTarget, TReturn>));
            return (target) => func((TTarget)target);
        }

        private static Func<TDelegateReturn> MagicStaticFieldGetterHelper<TReturn, TDelegateReturn>(FieldInfo field) where TReturn : TDelegateReturn
        {
            if (!field.FieldType.IsAssignableTo(typeof(TReturn)))
                throw new Exception();

            string methodName = field.ReflectedType.FullName + ".get_" + field.Name;
            DynamicMethod getterMethod = new DynamicMethod(methodName, field.FieldType, null, true);
            ILGenerator ilGen = getterMethod.GetILGenerator();
            ilGen.Emit(OpCodes.Ldsfld, field);
            ilGen.Emit(OpCodes.Ret);

            var func = (Func<TReturn>)getterMethod.CreateDelegate(typeof(Func<TReturn>));
            return () => func();
        }

        public static string GetBackingFieldName(this PropertyInfo property)
        {
            var field = property.DeclaringType.Field($"<{property.Name}>k__BackingField") ?? throw new ArgumentException("Property is not auto-implemented.");
            return field.Name;
        }

        public static AccessTools.FieldRef<object, T> BackingFieldRefAccess<T>(this Type declaringType, string autoPropertyName)
        {
            var property = declaringType.Property(autoPropertyName);
            var backingFieldName = property.GetBackingFieldName();
            return declaringType.FieldRefAccess<T>(backingFieldName);
        }

        public static AccessTools.FieldRef<T> StaticBackingFieldRefAccess<T>(this Type declaringType, string autoPropertyName)
        {
            var property = declaringType.Property(autoPropertyName);
            var backingFieldName = property.GetBackingFieldName();
            return AccessTools.StaticFieldRefAccess<T>(declaringType.Field(backingFieldName));
        }

        public static dynamic CreateInvoker(this MethodInfo method)
        {
            if (method.IsGenericMethod || method.ContainsGenericParameters)
                throw new NotImplementedException();

            if (method.IsStatic && method.GetParameters().Length == 0)
            {
                if (method.ReturnType.IsVoid())
                {
                    return method.CreateDelegate<Action>();
                }
                else
                {
                    return method.CreateDelegate(Expression.GetFuncType(method.ReturnType));
                }
            }

            Type[] parameters = method.GetParameterTypes().ToArray();
            if (!method.IsStatic)
                parameters = parameters.Prepend(method.DeclaringType).ToArray();

            IEnumerable<Type> paramsWithRefTypesReplacedWithObject = parameters.Select(i => !i.IsValueType && !i.IsByRef ? typeof(object) : i);

            DynamicMethod dynamicInvoker = new DynamicMethod(
                $"CompiledInvoker+{method.DeclaringType.FullName}.{method.Name}({string.Join(", ", parameters.Select(i => i.Name))})",
                method.ReturnType,
                paramsWithRefTypesReplacedWithObject.ToArray(),
                //method.DeclaringType,
                true);

            ILGenerator ilGen = dynamicInvoker.GetILGenerator();

            // push method params onto the stack
            for (int i = 0; i < parameters.Length; i++)
            {
                Type parameterType = parameters[i];
                ilGen.Emit(OpCodes.Ldarg, i);

                // if reference type, try to cast obj to parameter type
                // not strictly necessary but makes the invoker type safe
                if (!parameterType.IsValueType && !parameterType.IsByRef)
                {
                    ilGen.Emit(OpCodes.Castclass, parameterType);
                }
            }

            // call method
            ilGen.EmitCall(method.IsStatic ? OpCodes.Call : OpCodes.Callvirt, method, null);
            // return value
            ilGen.Emit(OpCodes.Ret);

            if (parameters.Any(i => i.IsByRef))
            {
                Type delegateType2 = Expression.GetDelegateType(paramsWithRefTypesReplacedWithObject.Append(method.ReturnType).ToArray());
                return dynamicInvoker.CreateDelegate(delegateType2);
            }

            Type delegateType = method.ReturnType == typeof(void) ?
                Expression.GetActionType(paramsWithRefTypesReplacedWithObject.ToArray()) :
                Expression.GetFuncType(paramsWithRefTypesReplacedWithObject.Append(method.ReturnType).ToArray());
            return dynamicInvoker.CreateDelegate(delegateType);
        }

        public static IEnumerable<Type> GetParameterTypes(this MethodInfo method)
        {
            return method.GetParameters().Select(i => i.ParameterType);
        }

        public static MethodInfo FindMethod(this Type type, string methodName, Type[] parameters = null, Type[] generics = null)
        {
            MethodInfo method = type.Method(methodName, parameters, generics);
            if (method != null || !type.IsInterface)
            {
                return method;
            }

            var interfaces = type.GetInterfaces();
            foreach (var face in interfaces)
            {
                if ((method = face.Method(methodName, parameters, generics)) is MethodInfo)
                {
                    return method;
                }
            }

            throw new Exception($"Method not found. {nameof(type)}={type}, {nameof(methodName)}={methodName}");
        }

        public static MethodInfo FindPropertyGetter(this Type type, string propertyName)
        {
            MethodInfo getter = type.PropertyGetter(propertyName);
            if (getter != null || !type.IsInterface)
            {
                return getter;
            }

            var interfaces = type.GetInterfaces();
            foreach (var face in interfaces)
            {
                if ((getter = face.PropertyGetter(propertyName)) is MethodInfo)
                {
                    return getter;
                }
            }

            throw new Exception($"Property getter not found. {nameof(type)}={type}, {nameof(propertyName)}={propertyName}");
        }

        public static bool IsAssignableTo(this Type type, Type c)
        {
            return c.IsAssignableFrom(type);
        }
    }
}
