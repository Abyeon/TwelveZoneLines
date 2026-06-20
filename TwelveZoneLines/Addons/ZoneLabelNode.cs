using System;
using System.Linq;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Enums;
using KamiToolKit.Nodes;
using KamiToolKit.UiOverlay;
using Lumina.Excel.Sheets;
using TwelveZoneLines.Utils;

namespace TwelveZoneLines.Addons;

public class ZoneLabelNode : OverlayNode
{
    public override OverlayLayer OverlayLayer => OverlayLayer.BehindUserInterface;
    
    private readonly TextNode labelNode;
    private readonly ImGuiImageNode imageNode;

    public ZoneLabelNode(string imagePath)
    {
        labelNode = new TextNode();
        labelNode.AttachNode(this);

        imageNode = new ImGuiImageNode
        {
            Position = new Vector2(-30, 0),
            TexturePath = imagePath,
            Size = new Vector2(20, 20),
            FitTexture = true,
        };
        imageNode.AttachNode(this);
    }

    protected override void OnSizeChanged()
    {
        base.OnSizeChanged();
        
        labelNode.Size = Size;
        imageNode.Size = new Vector2(Size.Y);
        
        Origin = new Vector2(Width / 2, Height / 2);
    }

    protected override void OnUpdate()
    {
        labelNode.TextFlags = TextFlags.AutoAdjustNodeSize | TextFlags.Edge | TextFlags.Emboss;
        labelNode.TextColor = ColorHelper.GetColor(2);
        labelNode.TextOutlineColor = new Vector4(0, 0, 0, 1);
        labelNode.FontSize = 20;
        labelNode.FontType = FontType.Axis;
        labelNode.AlignmentType = AlignmentType.Left;
        
        var visible = TryUpdateLabel(); // false if no zone line in range / on screen
        labelNode.IsVisible = visible;
        imageNode.IsVisible = visible;
    }

    private Vector3 previousLocation = Vector3.Zero;
    
    private const float MinDistance = 10f;
    private const float MaxDistance = 350f;

    private readonly Vector2 minScale = new(0.5f, 0.5f);
    private readonly Vector2 maxScale = new(1.0f, 1.0f);

    private unsafe bool TryUpdateLabel()
    {
        if (!Safe.Ptr((BattleChara*)Plugin.ObjectTable.LocalPlayer!.Address, out var player)) return false;
        
        var height = player->Height;
        Vector3 playerPos = player->Position with { Y = player->Position.Y + height };
        
        var territory = Plugin.DataManager.GetExcelSheet<TerritoryType>();
        var closest = Plugin.ZoneWatcher.ZoneExits.MinBy(x => Vector3.DistanceSquared(x.Transform.Translation, playerPos));
        
        if (territory.TryGetRow(closest.DestinationId, out var row))
        {
            var closestPoint = closest.GetClosestPoint(playerPos);

            // Smoothly lerp point over time
            var adjustedWorldPoint = Vector3.Lerp(previousLocation, closestPoint, (float)Plugin.Framework.UpdateDelta.TotalSeconds * 15f);
            previousLocation = adjustedWorldPoint;
            
            // Convert to screen
            var dist = Vector3.DistanceSquared(playerPos, closestPoint);
            if (!(dist < MaxDistance) || !Plugin.GameGui.WorldToScreen(adjustedWorldPoint, out var screenPos)) return false;
            
            // Adjust scale (farther = smaller)
            var s = ( dist - MinDistance ) / ( MaxDistance - MinDistance );
            var scale = Vector2.Lerp(maxScale, minScale, s);
            Scale = scale;
            
            labelNode.String = row.PlaceName.Value.Name.ExtractText().Trim();
            Position = new Vector2(MathF.Round(screenPos.X - (Width / 2)), MathF.Round(screenPos.Y));
            Size = labelNode.Size with { Y = Height };
            
            return true;
        }

        return false;
    }
}
