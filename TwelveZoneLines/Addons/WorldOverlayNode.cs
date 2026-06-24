using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using KamiToolKit.Enums;
using KamiToolKit.UiOverlay;
using TwelveZoneLines.Utils;

namespace TwelveZoneLines.Addons;

public abstract unsafe class WorldOverlayNode : OverlayNode
{
    public new abstract Vector3 Position { get; set; }
    
    public override OverlayLayer OverlayLayer => OverlayLayer.Background;

    protected override void OnUpdate()
    {
        var framework = Framework.Instance();
        OnUpdate(framework->FrameDeltaTime);
        
        IsVisible = Gui.WorldToScreen(Position, out var screenPos) && IsVisible;
        base.Position = screenPos - base.Origin;
    }

    protected abstract void OnUpdate(float deltaTime);
}
