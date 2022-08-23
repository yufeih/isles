// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Runtime.InteropServices;
using Isles.Graphics;

using static ImGuiNET.ImGui;

Directory.SetCurrentDirectory(AppContext.BaseDirectory);

using var sandbox = new MoveSandbox();
sandbox.Run();

class MoveSandbox : Game
{
    private const float WorldScale = 4f;

    private readonly List<Movable> _movables = new();
    private readonly List<int> _selection = new();
    private readonly PathGrid[] _grids;
    private readonly Move _move = new();
    private readonly Random _random = new();

    private Point _selectStart, _selectEnd;

    private TextureLoader _textureLoader = default!;
    private SpriteBatch _spriteBatch = default!;
    private ImGuiRenderer _imguiRenderer = default!;

    private int _gridIndex;
    private bool _showFlowField = true;
    private bool _showSpeed;
    private int _spawnCount = 1;
    private System.Numerics.Vector2 _spawnSpeed = new(50, 50);
    private System.Numerics.Vector2 _spawnRotateSpeed = new(50, 50);
    private System.Numerics.Vector2 _spawnRadius = new(4, 4);
    private System.Numerics.Vector2 _spawnAcceleration = new(300, 300);
    private MouseState _lastMouseState;

    public MoveSandbox()
    {
        Window.AllowUserResizing = true;
        IsMouseVisible = true;
        IsFixedTimeStep = true;

        new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = 1280,
            PreferredBackBufferHeight = 768,
        };

        var colors = TextureLoader.ReadAllPixels("data/grid.png", out var w, out var h);
        var bits = new BitArray(colors.ToArray().Select(c => c.R < 100).ToArray());
        _grids = new PathGrid[]
        {
            new(w, h, 10, new(w * h)),
            new(w, h, 10, bits),
        };
    }

    protected override void LoadContent()
    {
        _textureLoader = new(GraphicsDevice);
        _spriteBatch = new(GraphicsDevice);
        _imguiRenderer = new(GraphicsDevice);
    }

    protected override void Update(GameTime gameTime)
    {
        if (!GetIO().WantCaptureMouse)
            UpdateSelection();

        _move.Update(
            (float)gameTime.ElapsedGameTime.TotalSeconds,
            CollectionsMarshal.AsSpan(_movables),
            _grids[_gridIndex]);

        _imguiRenderer.Update((float)gameTime.ElapsedGameTime.TotalSeconds);
    }

    private float Random(System.Numerics.Vector2 v)
    {
        return v.X + (v.Y - v.X) * _random.NextSingle();
    }

    private void UpdateSelection()
    {
        var movables = CollectionsMarshal.AsSpan(_movables);

        var mouse = Mouse.GetState();
        if (mouse.RightButton == ButtonState.Pressed)
        {
            if (_selection.Count == 0 && _lastMouseState.RightButton != ButtonState.Pressed)
            {
                for (var i = 0; i < _spawnCount; i++)
                {
                    _movables.Add(new()
                    {
                        Radius = Random(_spawnRadius),
                        Position = new(mouse.X / WorldScale, mouse.Y / WorldScale),
                        Speed = Random(_spawnSpeed),
                        Acceleration = Random(_spawnAcceleration),
                        Decceleration = 400 + 400 * _random.NextSingle(),
                        RotationSpeed = Random(_spawnRotateSpeed),
                        Rotation = _random.NextSingle() * MathF.PI * 2 - MathF.PI,
                    });
                }
            }

            var target = new Vector2(mouse.X / WorldScale, mouse.Y / WorldScale);
            foreach (var i in _selection)
            {
                movables[i].Target = target;
            }
        }

        if (mouse.LeftButton == ButtonState.Pressed)
        {
            if (_selectStart == default)
                _selectStart = _selectEnd = new(mouse.X, mouse.Y);
            else
                _selectEnd = new(mouse.X, mouse.Y);

            var rectangle = GetSelectionRectangle();
            _selection.Clear();
            for (var i = 0; i < movables.Length; i++)
            {
                ref readonly var m = ref movables[i];
                if (rectangle.Contains((int)(m.Position.X * WorldScale), (int)(m.Position.Y * WorldScale)))
                {
                    _selection.Add(i);
                }
            }
        }
        else
        {
            _selectStart = _selectEnd = default;
        }

        _lastMouseState = mouse;
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.White);

        var arrow = _textureLoader.LoadTexture("data/unit.svg");
        var selection = _textureLoader.LoadTexture("data/pixel.svg");

        _imguiRenderer.Begin();
        _spriteBatch.Begin();

        DrawGrid(_grids[_gridIndex]);

        Text($"FPS: {GetIO().Framerate}");
        SliderInt("Grid", ref _gridIndex, 0, _grids.Length - 1);
        Checkbox("Show FlowField", ref _showFlowField);
        Checkbox("Show Speed", ref _showSpeed);

        SliderInt("Spawn Count", ref _spawnCount, 1, 20);
        SliderFloat2("Spawn Speed", ref _spawnSpeed, 10, 50);
        SliderFloat2("Spawn Rotation Speed", ref _spawnRotateSpeed, 5, 50);
        SliderFloat2("Spawn Radius", ref _spawnRadius, 1, 6);
        SliderFloat2("Spawn Accel.", ref _spawnAcceleration, 10, 300);


        if (_selection.Count > 0 && Button("Delete movables"))
        {
            foreach (var i in _selection.OrderByDescending(x => x))
            {
                _movables.RemoveAt(i);
            }
            _selection.Clear();
        }

        var movables = CollectionsMarshal.AsSpan(_movables);

        if (_selection.Count == 1)
        {
            ref readonly var m = ref movables[_selection[0]];
            Text($"pos: {m.Position.X:000.00}, {m.Position.Y:000.00}");
            Text($"vel: {m.Velocity.X:000.00}, {m.Velocity.Y:000.00}");
            Text($"force: {m.Force.X:000.00}, {m.Force.Y:000.00}");
            Text($"speed: {m.Velocity.Length():00.00} / {m.Speed:00.00}");
            Text($"flags: {m.Flags}");
        }

        if (_showFlowField && _selection.Count > 0)
        {
            var flowField = movables[_selection[0]].FlowField;
            if (flowField != null)
                DrawFlowField(flowField);
        }

        for (var i = 0; i < movables.Length; i++)
        {
            ref readonly var m = ref movables[i];

            var color = m.Target != null ? Color.Green : _selection.Contains(i) ? Color.Orange : Color.DarkSlateBlue;
            if (!m.Flags.HasFlag(MovableFlags.Awake))
                color *= 0.5f;

            if (_showSpeed)
            {
                var r = MathHelper.Clamp(m.Velocity.Length() / m.Speed, 0, 1);
                color = r > 0.95f ? new(r, 0, 0) : (r > 0.9f ? new(0,r,0) : new(0,0,r));
            }

            _spriteBatch.Draw(
                arrow,
                m.Position * WorldScale,
                null,
                color: color,
                rotation: m.Rotation,
                origin: new(arrow.Width / 2, arrow.Height / 2),
                scale: m.Radius * 2 * WorldScale / arrow.Width,
                SpriteEffects.None,
                0);
        }

        if (_selectStart != _selectEnd)
        {
            _spriteBatch.Draw(selection, GetSelectionRectangle(), Color.Green * 0.5f);
        }

        _spriteBatch.End();
        _imguiRenderer.End();

        void DrawLine(Vector2 a, Vector2 b, float width, Color color)
        {
            var rotation = MathF.Atan2(b.Y - a.Y, b.X - a.X);
            var scale = new Vector2(Vector2.Distance(a, b), width);
            _spriteBatch.Draw(selection, a, null, color, rotation, default, scale, SpriteEffects.None, 0);
        }
        
        void DrawGrid(PathGrid grid)
        {
            for (var y = 0; y <= grid.Height; y++)
                DrawLine(
                    new(0, y * grid.Step * WorldScale),
                    new(grid.Width * grid.Step * WorldScale, y * grid.Step * WorldScale),
                    1, Color.LightGray);

            for (var x = 0; x <= grid.Height; x++)
                DrawLine(
                    new(x * grid.Step * WorldScale, 0),
                    new(x * grid.Step * WorldScale, grid.Height * grid.Step * WorldScale),
                    1, Color.LightGray);

            for (var i = 0; i < grid.Bits.Length; i++)
            {
                if (grid.Bits[i])
                {
                    var x = i % grid.Width;
                    var y = i / grid.Width;
                    _spriteBatch.Draw(
                        selection,
                        new Rectangle(
                            (int)(x * grid.Step * WorldScale), (int)(y * grid.Step * WorldScale),
                            (int)(grid.Step * WorldScale), (int)(grid.Step * WorldScale)),
                        Color.DarkCyan);
                }
            }
        }

        void DrawFlowField(PathGridFlowField flowfield)
        {
            var arrow = _textureLoader.LoadTexture("data/arrow.svg");
            var grid = flowfield.Grid;
            for (var y = 0; y < grid.Height; y++)
            for (var x = 0; x < grid.Width; x++)
            {
                var position = new Vector2((x + 0.5f) * grid.Step, (y + 0.5f) * grid.Step);
                var v = flowfield.GetVector(position);
                if (v == default)
                    continue;
                var rotation = MathF.Atan2(v.Y, v.X);
                var scale = Math.Min(v.Length() / grid.Step, 1);
                var color = Color.Gray;
                if (!flowfield.FlowField.Vectors[x + y * grid.Width].IsTurnPoint)
                    color *= 0.2f;

                _spriteBatch.Draw(
                    arrow,
                    position * WorldScale,
                    null,
                    color,
                    rotation,
                    new Vector2(arrow.Width / 2, arrow.Height / 2),
                    grid.Step * WorldScale / arrow.Width * scale,
                    default,
                    default);
            }
        }
    }

    private Rectangle GetSelectionRectangle()
    {
        return new Rectangle(
            Math.Min(_selectStart.X, _selectEnd.X),
            Math.Min(_selectStart.Y, _selectEnd.Y),
            Math.Abs(_selectStart.X - _selectEnd.X),
            Math.Abs(_selectStart.Y - _selectEnd.Y));
    }
}