using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;

namespace TwelveZoneLines.Utils.Structs;

[StructLayout(LayoutKind.Explicit, Size = 0xA0)]
public struct LineVfxLayoutInstance
{
    [FieldOffset(0x30)] public Transform Transform;
}
