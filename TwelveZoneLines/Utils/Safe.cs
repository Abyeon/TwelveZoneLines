using System.Runtime.CompilerServices;

namespace TwelveZoneLines.Utils;

public static class Safe
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Obj<T>(T field, out T value)
    {
        value = field;
        return field != null;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool Ptr<T>(T* ptr, out T* value) where T : unmanaged
    {
        value = ptr;
        return ptr != null;
    }
}
