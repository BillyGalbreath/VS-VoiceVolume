using System.Reflection;

namespace VoiceVolume;

public static class ReflectionHelper {
    public static T? GetField<T>(this object obj, string name) {
        return (T?)obj.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(obj);
    }

    public static void SetField(this object obj, string name, object value) {
        obj.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(obj, value);
    }

    public static T Invoke<T>(this object obj, string name, object?[]? parameters = null) {
        return (T)obj.GetType().GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(obj, parameters)!;
    }

    public static void InvokeVoid(this object obj, string name, object?[]? parameters = null) {
        obj.GetType().GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(obj, parameters);
    }
}
