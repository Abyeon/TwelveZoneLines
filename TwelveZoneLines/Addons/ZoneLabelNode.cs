using System;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Utility;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine.Layer;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Enums;
using KamiToolKit.Nodes;
using KamiToolKit.UiOverlay;
using TwelveZoneLines.Utils;

namespace TwelveZoneLines.Addons;

public class ZoneLabelNode : WorldOverlayNode
{
    public override Vector3 Position { get; set; }
    public override OverlayLayer OverlayLayer => OverlayLayer.Background;
    
    private readonly TextNode labelNode;
    private readonly ImGuiImageNode imageNode;

    public ZoneLabelNode(string imagePath)
    {
        labelNode = new TextNode
        {
            TextFlags = TextFlags.AutoAdjustNodeSize | TextFlags.Edge,
            TextColor = ColorHelper.GetColor(2),
            TextOutlineColor = new Vector4(0, 0, 0, 1),
            FontSize = 20,
            FontType = FontType.Axis,
            AlignmentType = AlignmentType.Left,
        };
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
    
    protected override void OnUpdate(float deltaTime)
    {
        IsVisible = TryUpdateLabel(deltaTime);
    }

    private Vector3 previousLocation = Vector3.Zero;
    
    private const float MinDistance = 10f;
    private const float MaxDistance = 350f;

    private readonly Vector2 minScale = new(0.5f, 0.5f);
    private readonly Vector2 maxScale = new(1.0f, 1.0f);

    private unsafe bool TryUpdateLabel(float deltaTime)
    {
        if (!Safe.Ptr(Control.Instance(), out var control)) return false;
        if (!Safe.Ptr(control->LocalPlayer, out var player)) return false;
        if (Plugin.ZoneWatcher.ZoneExits.Count == 0) return false;
        
        // Get the closest exit
        Vector3 playerPos = player->Position;
        var exit = Plugin.ZoneWatcher.GetClosestExit(playerPos, out var closestPoint);
        
        // If the player is flying, just get the closest point, else get the closest point on the ground.
        var state = player->MoveController.MovementState;
        closestPoint = state != MovementStateOptions.Normal ? closestPoint : exit.GetClosestGroundPoint(playerPos);
        closestPoint.Y += 1;
        
        // Lerp to current location (maybe should be toggleable)
        closestPoint = Vector3.Lerp(previousLocation, closestPoint, deltaTime * 30f);
        previousLocation = closestPoint;
        
        // Check distance
        var dist = Vector3.DistanceSquared(playerPos, closestPoint);
        if (!(dist < MaxDistance)) return false;
        Position = closestPoint;
        
        // Adjust scale (farther = smaller)
        var s = ( dist - MinDistance ) / ( MaxDistance - MinDistance );
        Scale = Vector2.Lerp(maxScale, minScale, s);

        // Update the name if it's not the same
        var name = exit.Name;
        if (labelNode.String != name)
        {
            labelNode.String = name;
            Size = labelNode.Size with { Y = Height };
        }
            
        return true;
    }

    private bool usingDepthBasedPriority;
    
    private unsafe void UpdateDepth(float depth)
    {
        if (!usingDepthBasedPriority)
        {
            Node->SetUseDepthBasedPriority(true);
            labelNode.Node->SetUseDepthBasedPriority(true);
            imageNode.Node->SetUseDepthBasedPriority(true);
            
            usingDepthBasedPriority = true;
        }

        Node->Depth           = depth;
        labelNode.Node->Depth = depth;
        imageNode.Node->Depth = depth;
    }

    private static unsafe float GetDepth(Vector3 target)
    {
        if (!Safe.Ptr(CameraManager.Instance(), out var cameraManager)) return 0f;
        var cam = cameraManager->Cameras[cameraManager->ActiveCameraIndex].Value->SceneCamera.RenderCamera;
        
        var viewSpace = Vector4.Transform(new Vector4(target, 1f), cam->ViewMatrix);
        var planarDistance = Math.Abs(viewSpace.Z);

        var near = cam->NearPlane;
        var far = cam->FarPlane;
        var depth = (planarDistance - near) / (far - near);
        
        return Math.Clamp(depth, 0f, 1f);
    }
}
