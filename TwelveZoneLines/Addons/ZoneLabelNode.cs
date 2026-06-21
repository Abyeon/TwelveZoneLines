using System;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Utility;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Enums;
using KamiToolKit.Nodes;
using KamiToolKit.UiOverlay;
using TwelveZoneLines.Utils;
using Matrix4x4 = FFXIVClientStructs.FFXIV.Common.Math.Matrix4x4;

namespace TwelveZoneLines.Addons;

public class ZoneLabelNode : OverlayNode
{
    public override OverlayLayer OverlayLayer => OverlayLayer.Background;
    
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
        
        Origin = new Vector2(Width / 2, Height);
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
        if (!Safe.Obj(Plugin.ObjectTable.LocalPlayer, out var playerCharacter)) return false;
        if (!Safe.Ptr((BattleChara*)playerCharacter!.Address, out var player)) return false;
        
        var height = player->Height;
        Vector3 playerPos = player->Position;
        
        var exit = Plugin.ZoneWatcher.ZoneExits.MinBy(x => Vector3.DistanceSquared(x.Transform.Translation, playerPos));
        
        if (exit.IsValid)
        {
            var closestPoint = exit.GetClosestPoint(playerPos) + new Vector3(0, height, 0);
            
            // Convert to screen
            var dist = Vector3.DistanceSquared(playerPos, closestPoint);
            if (!(dist < MaxDistance) || !WorldToScreen(closestPoint, out var screenPos))
                return false;
            
            // Adjust scale (farther = smaller)
            var s = ( dist - MinDistance ) / ( MaxDistance - MinDistance );
            var scale = Vector2.Lerp(maxScale, minScale, s);
            Scale = scale;
            
            // Update position / depth
            Position = new Vector2(MathF.Round(screenPos.X - (Width / 2)), MathF.Round(screenPos.Y));
            UpdateDepth(closestPoint);

            // Update the name if it's not the same
            var name = exit.Name;
            if (labelNode.String != name)
            {
                labelNode.String = name;
                Size = labelNode.Size with { Y = Height };
            }
            
            return true;
        }

        return false;
    }

    private bool usingDepthBasedPriority;
    
    private unsafe void UpdateDepth(Vector3 target)
    {
        if (!usingDepthBasedPriority)
        {
            Node->SetUseDepthBasedPriority(true);
            labelNode.Node->SetUseDepthBasedPriority(true);
            imageNode.Node->SetUseDepthBasedPriority(true);
            
            usingDepthBasedPriority = true;
        }

        if (!Safe.Ptr(CameraManager.Instance(), out var cameraManager)) return;
        var cam = cameraManager->Cameras[cameraManager->ActiveCameraIndex].Value->SceneCamera.RenderCamera;
        var pos = cam->Origin;
        var far = cam->FarPlane;
        var near = cam->NearPlane;
        
        var dist = Vector3.Distance(target, pos);
        var depth = (dist - near) / (far - near);

        Node->Depth           = depth;
        labelNode.Node->Depth = depth;
        imageNode.Node->Depth = depth;
    }
    
    /// <inheritdoc/>
    public static bool WorldToScreen(Vector3 worldPos, out Vector2 screenPos, bool frameAhead = false)
        => WorldToScreen(worldPos, out screenPos, out var inView, frameAhead) && inView;
    
    /// <inheritdoc/>
    public static unsafe bool WorldToScreen(Vector3 worldPos, out Vector2 screenPos, out bool inView, bool frameAhead = false)
    {
        // Read current ViewProjectionMatrix plus game window size
        var windowPos = ImGuiHelpers.MainViewport.Pos;

        var cameraMan = CameraManager.Instance();
        var activeCamera = cameraMan->Cameras[cameraMan->ActiveCameraIndex].Value;
        var renderCamera = activeCamera->SceneCamera.RenderCamera;
        
        var view = frameAhead ? renderCamera->ViewMatrix : Matrix4x4.CreateLookAt(activeCamera->SceneCamera.Position, activeCamera->SceneCamera.LookAtVector, Vector3.UnitY);
        var proj = renderCamera->ProjectionMatrix;
        var viewProjectionMatrix = view * proj;
        
        var device = Device.Instance();
        float width = device->Width;
        float height = device->Height;

        var pCoords = Vector4.Transform(new Vector4(worldPos, 1.0f), viewProjectionMatrix);
        var inFront = pCoords.W > 0.0f;

        if (Math.Abs(pCoords.W) < float.Epsilon)
        {
            screenPos = Vector2.Zero;
            inView = false;
            return false;
        }

        pCoords *= MathF.Abs(1.0f / pCoords.W);
        screenPos = new Vector2(pCoords.X, pCoords.Y);

        screenPos.X = (0.5f * width * (screenPos.X + 1f)) + windowPos.X;
        screenPos.Y = (0.5f * height * (1f - screenPos.Y)) + windowPos.Y;

        inView = inFront &&
                 screenPos.X > windowPos.X && screenPos.X < windowPos.X + width &&
                 screenPos.Y > windowPos.Y && screenPos.Y < windowPos.Y + height;

        return inFront;
    }
}
