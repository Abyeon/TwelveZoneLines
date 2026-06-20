using System;
using System.Collections.Generic;
using Dalamud.Game.ClientState;

namespace TwelveZoneLines.Utils;

public class ZoneWatcher : IDisposable
{
    public List<ZoneExit> ZoneExits = [];
    
    public ZoneWatcher()
    {
        Plugin.ClientState.MapIdChanged += OnMapIdChange;
        Plugin.ClientState.Login += OnLogin;
        UpdateExits();
    }

    private void OnLogin() => UpdateExits();
    private void OnMapIdChange(uint obj) => UpdateExits();

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
        Plugin.ClientState.MapIdChanged -= OnMapIdChange;
        Plugin.ClientState.Login -= OnLogin;
    }
}
