// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Xna.Framework.Input;

namespace Isles;

public enum BirdEyeCameraScrollState
{
    None, N, NE, E, SE, S, SW, W, NW,
}

public class BirdEyeCamera
{
    private const float BoundsBorder = 100;
    private const int ScrollBorder = 10;

    private static readonly Dictionary<(int, int), BirdEyeCameraScrollState> MoveStateMap = new()
    {
        [(0, -1)] = BirdEyeCameraScrollState.N,
        [(1, -1)] = BirdEyeCameraScrollState.NE,
        [(1, 0)] = BirdEyeCameraScrollState.E,
        [(1, 1)] = BirdEyeCameraScrollState.SE,
        [(0, 1)] = BirdEyeCameraScrollState.S,
        [(-1, 1)] = BirdEyeCameraScrollState.SW,
        [(-1, 0)] = BirdEyeCameraScrollState.W,
        [(-1, -1)] = BirdEyeCameraScrollState.NW,
    };

    private Vector3 _lookAt;
    private float _radius = 180;
    private int _scrollWheelValue;

    public Matrix View { get; private set; } = Matrix.Identity;

    public float MinRadius { get; set; } = 80;
    public float MaxRadius { get; set; } = 250;
    public float WheelSpeed { get; set; } = 0.1f;
    public float ScrollSpeed { get; set; } = 0.4f;

    public bool Freezed { get; set; }

    public BirdEyeCameraScrollState ScrollState { get; private set; }

    public Matrix GetProjection(Rectangle viewport)
    {
        return Matrix.CreatePerspectiveFieldOfView(
            MathHelper.PiOver4, (float)viewport.Width / viewport.Height, 1, 5000);
    }

    public void SetTarget(Vector3 position)
    {
        _lookAt = position;
    }

    public void Update(ITerrain? terrain)
    {
        if (terrain != null)
        {
            _lookAt.X = MathHelper.Clamp(_lookAt.X, BoundsBorder, terrain.Size.X - BoundsBorder);
            _lookAt.Y = MathHelper.Clamp(_lookAt.Y, BoundsBorder, terrain.Size.Y - BoundsBorder);
            _lookAt.Z = terrain.GetHeight(_lookAt.X, _lookAt.Y);
        }

        var angle = MathHelper.ToRadians(60);
        var eye = _lookAt;
        eye.Z += _radius * MathF.Sin(angle);
        eye.Y -= _radius * MathF.Cos(angle);

        View = Matrix.CreateLookAt(eye, _lookAt, Vector3.UnitZ);
    }

    public void UpdateInput(float dt, Rectangle viewport)
    {
        if (Freezed)
            return;

        var mouse = Mouse.GetState();
        var scrollWheelOffset = mouse.ScrollWheelValue - _scrollWheelValue;
        _scrollWheelValue = mouse.ScrollWheelValue;
        _radius += scrollWheelOffset * WheelSpeed;
        _radius = MathHelper.Clamp(_radius, MinRadius, MaxRadius);

        var (dx, dy) = (0,0);
        if (mouse.X <= viewport.X + ScrollBorder)
            dx = -1;
        else if (mouse.X >= viewport.X + viewport.Width - ScrollBorder)
            dx = 1;

        if (mouse.Y <= viewport.Y + ScrollBorder)
            dy = -1;
        else if (mouse.Y >= viewport.Y + viewport.Height - ScrollBorder)
            dy = 1;

        ScrollState = MoveStateMap.GetValueOrDefault((dx, dy));

        var step = ScrollSpeed * dt;
        if (dx * dy != 0)
            step *= MathF.Sqrt(2) / 2;

        _lookAt.X += step * dx;
        _lookAt.Y -= step * dy;
    }
}

