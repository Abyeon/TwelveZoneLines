using System;
using System.Numerics;
using Dalamud.Interface.Utility;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;

using Matrix4x4 = FFXIVClientStructs.FFXIV.Common.Math.Matrix4x4;

namespace TwelveZoneLines.Utils;

public static class Gui
{
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
