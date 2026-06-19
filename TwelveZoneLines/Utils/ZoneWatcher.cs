using System;
using System.Collections.Generic;
using Dalamud.Game.ClientState;

namespace TwelveZoneLines.Utils;

public class ZoneWatcher : IDisposable
{
    public List<ZoneExit> ZoneExits = [];
    
    public ZoneWatcher()
    {
        Plugin.ClientState.ZoneInit += OnZoneInit;
        UpdateExits();
    }
    
    private void OnZoneInit(ZoneInitEventArgs obj) => UpdateExits();

    private void UpdateExits()
    {
        ZoneExits.Clear();
        if (LayoutMatcher.TryBuildAssociations(out var exits))
        {
            ZoneExits = exits;
        }
    }

    public void Dispose()
    {
        Plugin.ClientState.ZoneInit -= OnZoneInit;
    }
}
