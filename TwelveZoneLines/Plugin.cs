using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using System.Numerics;
using System.Resources;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit;
using KamiToolKit.UiOverlay;
using TwelveZoneLines.Addons;
using TwelveZoneLines.Utils;
using TwelveZoneLines.Windows;

namespace TwelveZoneLines;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IObjectTable ObjectTable { get; private set; } = null!;
    [PluginService] internal static IPlayerState PlayerState { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IGameGui GameGui { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;

    private const string CommandName = "/pmycommand";

    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("TwelveZoneLines");
    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }

    public static ZoneWatcher ZoneWatcher { get; private set; } = null!;
    public OverlayController? OverlayController { get; private set; }
    internal static ResourceManager ResourceManager { get; private set; } = null!;

    public Plugin()
    {
        ZoneWatcher = new ZoneWatcher();

        ResourceManager = new ResourceManager("TwelveZoneLines.Plugin", typeof(Plugin).Assembly);
        KamiToolKitLibrary.Initialize(PluginInterface, "TwelveZoneLines");
        // KamiToolKitLibrary.SetResourceManager(ResourceManager);

        Framework.RunOnFrameworkThread(InitializeOverlay);
        
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        // You might normally want to embed resources and load them from the manifest stream
        var goatImagePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "goat.png");

        ConfigWindow = new ConfigWindow(this);
        MainWindow = new MainWindow(this, goatImagePath);

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "A useful message to display in /xlhelp"
        });
        
        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;
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
        
        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;
        
        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();
        MainWindow.Dispose();

        CommandManager.RemoveHandler(CommandName);
    }

    private void OnCommand(string command, string args)
    {
        MainWindow.Toggle();
    }
    
    public void ToggleConfigUi() => ConfigWindow.Toggle();
    public void ToggleMainUi() => MainWindow.Toggle();
}
