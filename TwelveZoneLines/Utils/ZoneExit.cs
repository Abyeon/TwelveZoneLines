using System;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace TwelveZoneLines.Utils;

/// <summary>
/// Combines ExitRanges and LineVfx into one
/// </summary>
public struct ZoneExit
{
    public RowRef<TerritoryType> TerritoryType;
    public ushort DestinationId;
    public Transform Transform;

    public bool IsValid => TerritoryType.IsValid;
    public string Name => IsValid ? TerritoryType.Value.PlaceName.Value.Name.ExtractText() : string.Empty;

    public Vector3 GetClosestPoint(Vector3 target)
    {
        var pos = Transform.Translation;
        var rot = Transform.Rotation;
        var scale = Transform.Scale;
                    
        var up = Vector3.Transform(Vector3.UnitY, rot);
        var right = Vector3.Transform(Vector3.UnitX, rot);
        
        var dir = target - pos;

        var localX = Vector3.Dot(dir, right);
        var localY = Vector3.Dot(dir, up);
        
        var clampedX = Math.Clamp(localX, -scale.X, scale.X);
        var clampedY = Math.Clamp(localY, -scale.Y, scale.Y);
        
        return pos + (right * clampedX) + (up * clampedY);
    }
}
