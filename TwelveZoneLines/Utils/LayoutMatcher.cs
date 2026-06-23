using System;
using System.Collections.Generic;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using TwelveZoneLines.Utils.Structs;

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
        List<Transform> lineVfxInstances = [];
        
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
                        lineVfxInstances.Add(((LineVfxLayoutInstance*)instance)->Transform);
                        continue;
                    default:
                        continue;
                }
            }
        }
        
        foreach (var line in lineVfxInstances)
        {
            var line2D = new Vector2(line.Translation.X, line.Translation.Y);
            
            var smallestDistance = float.MaxValue;
            ushort closestId = 0;
            foreach (var rangePtr in exitRangeInstances)
            {
                var instance = (ILayoutInstance*)rangePtr;
                var transform = instance->GetTransformImpl();
                
                var range2D = new Vector2(transform->Translation.X, transform->Translation.Y);
                var dist = Vector2.DistanceSquared(line2D, range2D);

                if (dist < smallestDistance)
                {
                    smallestDistance = dist;
                    
                    var range = (ExitRangeLayoutInstance*)instance;
                    closestId = range->DestinationId;
                }
            }

            if (closestId != 0)
            {
                exits.Add(new ZoneExit
                {
                    TerritoryType = new RowRef<TerritoryType>(Plugin.DataManager.Excel, closestId),
                    Transform = line,
                });
            }
        }

        return true;
    }
}
