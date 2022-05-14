// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Isles;

public class CameraSettings
{
    public float MinRadius { get; set; } = 80;
    public float MaxRadius { get; set; } = 250;
    public float DefaultRadius { get; set; } = 180;
    public float WheelSpeed { get; set; } = 0.1f;
    public float ScrollSpeed { get; set; } = 0.3f;
}

public enum CameraScrollState
{
    None, N, NE, E, SE, S, SW, W, NW,
}

public class Camera
{
    private const float TerrainBorder = 100;
    private const int ScrollBorder = 10;

    private static readonly Dictionary<(int, int), CameraScrollState> MoveStateMap = new()
    {
        [(0, -1)] = CameraScrollState.N,
        [(1, -1)] = CameraScrollState.NE,
        [(1, 0)] = CameraScrollState.E,
        [(1, 1)] = CameraScrollState.SE,
        [(0, 1)] = CameraScrollState.S,
        [(-1, 1)] = CameraScrollState.SW,
        [(-1, 0)] = CameraScrollState.W,
        [(-1, -1)] = CameraScrollState.NW,
    };

    private Vector3 _lookAt;
    private float _radius = 100.0f;

    private int _scrollWheelValue;

    public Matrix View { get; private set; } = Matrix.Identity;

    public CameraSettings Settings { get; set; }

    public bool Freezed { get; set; }

    public CameraScrollState ScrollState { get; private set; }

    public Camera(CameraSettings settings)
    {
        Settings = settings;
        _radius = settings.DefaultRadius;
    }

    public Matrix GetProjection(Rectangle viewport)
    {
        return Matrix.CreatePerspectiveFieldOfView(
            MathHelper.PiOver4, (float)viewport.Width / viewport.Height, 1, 5000);
    }

    public void SetTarget(Vector3 position)
    {
        _lookAt = position;
    }

    public void Update(ILandscape terrain)
    {
        if (terrain != null)
            _lookAt = ClampToTerrain(_lookAt, terrain, TerrainBorder);

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
        _radius -= scrollWheelOffset * Settings.WheelSpeed;
        _radius = MathHelper.Clamp(_radius, Settings.MinRadius, Settings.MaxRadius);

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

        var step = Settings.ScrollSpeed * dt;
        if (dx * dy != 0)
            step *= MathF.Sqrt(2) / 2;

        _lookAt.X += step * dx;
        _lookAt.Y -= step * dy;
    }

    private static Vector3 ClampToTerrain(Vector3 v, ILandscape terrain, float border)
    {
        if (v.X < border)
            v.X = border;
        else if (v.X > terrain.Size.X - border)
            v.X = terrain.Size.X - border;

        if (v.Y < border)
            v.Y = border;
        else if (v.Y > terrain.Size.Y - border)
            v.Y = terrain.Size.Y - border;

        v.Z = terrain.GetHeight(v.X, v.Y);
        return v;
    }
}

