using System;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Common.Math;

namespace TwelveZoneLines.Utils.Structs;

[StructLayout(LayoutKind.Explicit, Size = 0x48C0)]
public unsafe struct PostEffectManager
{
    [FieldOffset(0xA50)] public Matrix4x4 ViewProjectionMatrix;

    private static PostEffectManager* _instance;
    
    public static PostEffectManager* Instance()
    {
        if (_instance != null)
        {
            return _instance;
        }
        
        if (Plugin.SigScanner.TryGetStaticAddressFromSig("48 8B 0D ?? ?? ?? ?? 72", out var result))
        {
            return *(PostEffectManager**)result;
        }
        
        return null;
    }
}
