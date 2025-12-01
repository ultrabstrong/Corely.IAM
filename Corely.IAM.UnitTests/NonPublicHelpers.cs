using System.Reflection;

namespace Corely.IAM.UnitTests;

public static class NonPublicHelpers
{
    private const BindingFlags BINDING_FLAGS =
        BindingFlags.Instance
        | BindingFlags.NonPublic
        | BindingFlags.FlattenHierarchy
        | BindingFlags.Static;

    public static T? InvokeNonPublicMethod<T>(object classInstance, string methodName)
    {
        var methodInfo =
            GetNonPublicMethod(classInstance, methodName)
            ?? throw new NullReferenceException(
                $"Method {methodName} not found in type {classInstance.GetType().Name}"
            );
        return (T?)methodInfo.Invoke(classInstance, null);
    }

    public static T? InvokeNonPublicMethod<T>(
        object classInstance,
        string methodName,
        params object[] args
    )
    {
        var methodInfo =
            GetNonPublicMethod(classInstance, methodName)
            ?? throw new NullReferenceException(
                $"Method {methodName} not found in type {classInstance.GetType().Name}"
            );
        return (T?)methodInfo.Invoke(classInstance, args);
    }

    /// <summary>
    /// Use this when method is overloaded
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="classInstance"></param>
    /// <param name="methodName"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    public static T? InvokeNonPublicMethod<T>(
        object classInstance,
        string methodName,
        params (Type, object)[] args
    )
    {
        var methodInfo =
            GetNonPublicMethod(classInstance, methodName, [.. args.Select(a => a.Item1)])
            ?? throw new NullReferenceException(
                $"Method {methodName} not found in type {classInstance.GetType().Name}"
            );
        return (T?)methodInfo.Invoke(classInstance, [.. args.Select(a => a.Item2)]);
    }

    private static MethodInfo? GetNonPublicMethod(object classInstance, string methodName)
    {
        var methodInfo =
            classInstance.GetType().GetMethod(methodName, BINDING_FLAGS)
            // This is mostly for cases where class is wrapped for unit testing
            ?? classInstance.GetType().BaseType?.GetMethod(methodName, BINDING_FLAGS);

        return methodInfo;
    }

    private static MethodInfo? GetNonPublicMethod(
        object classInstance,
        string methodName,
        params Type[] paramTypes
    )
    {
        var methodInfo =
            classInstance.GetType().GetMethod(methodName, BINDING_FLAGS, paramTypes)
            ?? classInstance.GetType().BaseType?.GetMethod(methodName, BINDING_FLAGS, paramTypes);

        return methodInfo;
    }

    public static T? GetNonPublicProperty<T>(
        object instance,
        string propName,
        bool getBackingFieldFallback = true
    )
    {
        try
        {
            var prop =
                GetPropertyInfo(instance, propName)
                ?? throw new NullReferenceException(
                    $"Property {propName} not found in type {instance.GetType()}"
                );
            if (prop.GetValue(instance) is T value)
            {
                return value;
            }
            else
            {
                throw new InvalidCastException($"Property {propName} is not of type {typeof(T)}");
            }
        }
        catch (NullReferenceException)
        {
            if (getBackingFieldFallback)
            {
                string fieldname = $"<{propName}>k__BackingField";
                return GetNonPublicField<T>(instance, fieldname);
            }
            else
            {
                throw;
            }
        }
    }

    public static T? GetNonPublicField<T>(object instance, string fieldName)
    {
        var field =
            GetFieldInfo(instance, fieldName)
            ?? throw new NullReferenceException(
                $"Field {fieldName} not found in type {instance.GetType()}"
            );

        if (field.GetValue(instance) is T value)
        {
            return value;
        }
        else
        {
            throw new InvalidCastException($"Field {fieldName} is not of type {typeof(T)}");
        }
    }

    public static void SetNonPublicProperty(
        object instance,
        string propName,
        object value,
        bool setBackingFieldFallback = true
    )
    {
        try
        {
            var prop =
                GetPropertyInfo(instance, propName)
                ?? throw new NullReferenceException(
                    $"Property {propName} not found in type {instance.GetType()}"
                );
            prop.SetValue(instance, value);
        }
        catch (NullReferenceException)
        {
            if (setBackingFieldFallback)
            {
                string fieldname = $"<{propName}>k__BackingField";
                SetNonPublicField(instance, fieldname, value);
            }
            else
            {
                throw;
            }
        }
    }

    public static void SetNonPublicField(object instance, string fieldName, object value)
    {
        var field =
            GetFieldInfo(instance, fieldName)
            ?? throw new NullReferenceException(
                $"Field {fieldName} not found in type {instance.GetType()}"
            );
        field.SetValue(instance, value);
    }

    private static PropertyInfo? GetPropertyInfo(object instance, string propName)
    {
        var instanceType = instance.GetType();
        var prop = instanceType.GetProperty(propName, BINDING_FLAGS);

        while (prop == null && instanceType.BaseType != null)
        {
            instanceType = instanceType.BaseType;
            prop = instanceType.GetProperty(propName, BINDING_FLAGS);
        }

        return prop;
    }

    private static FieldInfo? GetFieldInfo(object instance, string fieldName)
    {
        var instanceType = instance.GetType();
        var field = instanceType.GetField(fieldName, BINDING_FLAGS);

        while (field == null && instanceType.BaseType != null)
        {
            instanceType = instanceType.BaseType;
            field = instanceType.GetField(fieldName, BINDING_FLAGS);
        }

        return field;
    }
}
