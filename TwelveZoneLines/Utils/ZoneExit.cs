using System;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision;
using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace TwelveZoneLines.Utils;

/// <summary>
/// Combines ExitRanges and LineVfx into one
/// </summary>
public struct ZoneExit
{
    public RowRef<TerritoryType> TerritoryType;
    public Transform Transform;

    public bool IsValid => TerritoryType.IsValid;
    public string Name => IsValid ? TerritoryType.Value.PlaceName.Value.Name.ExtractText() : string.Empty;

    /// <summary>
    /// Gets the closest point on the LineVfx object.
    /// </summary>
    /// <param name="target">Target point to search from</param>
    /// <returns>Closest point on the plane</returns>
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

    /// <summary>
    /// Finds the closest point on the LineVfx object then casts downwards to adjust towards the ground.
    /// </summary>
    /// <param name="target">Target point to search from</param>
    /// <returns>Closest point on the plane, adjusted towards the ground.</returns>
    public Vector3 GetClosestGroundPoint(Vector3 target)
    {
        var closest = GetClosestPoint(target);
        var point = closest;

        if (BGCollisionModule.RaycastMaterialFilter(point, Vector3.UnitY, out var hit1))
        {
            point.Y = hit1.Point.Y + 2f;
        }
        else
        {
            point.Y = Transform.Translation.Y + (Transform.Scale.Y * 2);
        }
        
        if (BGCollisionModule.RaycastMaterialFilter(point, -Vector3.UnitY, out var hit2))
        {
            point.Y = hit2.Point.Y;
        }
        else
        {
            point.Y = closest.Y;
        }
        
        return point;
    }
}
