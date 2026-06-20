using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using System.Numerics;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit;
using KamiToolKit.UiOverlay;
using TwelveZoneLines.Addons;
using TwelveZoneLines.Utils;

namespace TwelveZoneLines;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IObjectTable ObjectTable { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IGameGui GameGui { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    
    public Configuration Configuration { get; init; }

    public static ZoneWatcher ZoneWatcher { get; private set; } = null!;
    public OverlayController? OverlayController { get; private set; }

    public Plugin()
    {
        ZoneWatcher = new ZoneWatcher();
        
        KamiToolKitLibrary.Initialize(PluginInterface, "TwelveZoneLines");

        Framework.RunOnFrameworkThread(InitializeOverlay);
        
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
    }

    public unsafe void InitializeOverlay()
    {
        var travelIconPath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "travelIcon.png");
        
        OverlayController = new OverlayController();
        OverlayController.AddNode(new ZoneLabelNode(travelIconPath)
        {
            Size = new Vector2(30, 30),
            Position = ((Vector2)AtkStage.Instance()->ScreenSize / 2.0f) - (new Vector2(150.0f, 30.0f) / 2.0f)
        });
    }

    public void Dispose()
    {
        OverlayController?.Dispose();
        ZoneWatcher.Dispose();
    }
}
