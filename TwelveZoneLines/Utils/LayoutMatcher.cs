using System;
using System.Collections.Generic;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine.Layer;
using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace TwelveZoneLines.Utils;

public static unsafe class LayoutMatcher
{
    /// <summary>
    /// Combines LineVfx instances and their closest ZoneExit neighbor.
    /// </summary>
    /// <param name="exits">Associated instances</param>
    /// <returns>True if successful</returns>
    public static bool TryBuildAssociations(out List<ZoneExit> exits)
    {
        exits = [];
        if (!Safe.Ptr(LayoutWorld.Instance(), out var world)) return false;
        if (!Safe.Ptr(world->ActiveLayout, out var active)) return false;

        List<IntPtr> exitRangeInstances = [];
        List<IntPtr> lineVfxInstances = [];
        
        foreach (var (_, layerPtr) in active->Layers)
        {
            if (!Safe.Ptr(layerPtr.Value, out var layer)) continue;
            
            foreach (var (_, instancePtr) in layer->Instances)
            {
                if (!Safe.Ptr(instancePtr.Value, out var instance)) continue;

                switch (instance->Id.Type)
                {
                    case InstanceType.ExitRange:
                        exitRangeInstances.Add((IntPtr)instance);
                        continue;
                    case InstanceType.LineVfx:
                        lineVfxInstances.Add((IntPtr)instance);
                        continue;
                    default:
                        continue;
                }
            }
        }
        
        foreach (var ptr in lineVfxInstances)
        {
            var line = (LineVfxLayoutInstance*)ptr;
            
            var line2D = new Vector2(line->Transform.Translation.X, line->Transform.Translation.Y);
            
            var smallestDistance = float.MaxValue;
            ExitRangeLayoutInstance* closest = null;
            foreach (var rangePtr in exitRangeInstances)
            {
                var range = (ExitRangeLayoutInstance*)rangePtr;
                if (range->ExitType != ExitRangeType.ZoneLine) continue;
                
                var transform = range->Transform;
                
                var range2D = new Vector2(transform.Translation.X, transform.Translation.Y);
                var dist = Vector2.DistanceSquared(line2D, range2D);

                if (dist < smallestDistance)
                {
                    smallestDistance = dist;
                    closest = range;
                }
            }

            if (closest != null)
            {
                exits.Add(new ZoneExit(line, closest));
            }
        }

        return true;
    }
}
