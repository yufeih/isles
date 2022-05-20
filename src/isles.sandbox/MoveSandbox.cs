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

    private readonly List<Movable> _moveUnits = new();
    private readonly List<Unit> _units = new();
    private readonly List<int> _selection = new();
    private readonly PathGrid _grid;
    private readonly PathFinder _pathFinder = new();
    private readonly Move _move = new();
    private readonly Random _random = new();

    private Point _selectStart, _selectEnd;
    private PathGridFlowField? _flowField;

    private TextureLoader _textureLoader = default!;
    private SpriteBatch _spriteBatch = default!;
    private ImGuiRenderer _imguiRenderer = default!;

    private bool _showFlowField;
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
        _grid = new(w, h, 10, bits);
    }

    protected override void LoadContent()
    {
        _textureLoader = new(GraphicsDevice);
        _spriteBatch = new(GraphicsDevice);
        _imguiRenderer = new(GraphicsDevice);
    }

    protected override void Update(GameTime gameTime)
    {
        UpdateSelection();

        _move.Update(
            (float)gameTime.ElapsedGameTime.TotalSeconds,
            CollectionsMarshal.AsSpan(_moveUnits),
            CollectionsMarshal.AsSpan(_units),
            _grid);

        _imguiRenderer.Update((float)gameTime.ElapsedGameTime.TotalSeconds);
    }

    private void UpdateSelection()
    {
        var moveUnits = CollectionsMarshal.AsSpan(_moveUnits);
        var units = CollectionsMarshal.AsSpan(_units);

        var mouse = Mouse.GetState();
        if (mouse.RightButton == ButtonState.Pressed)
        {
            if (_selection.Count == 0 && _lastMouseState.RightButton != ButtonState.Pressed)
            {
                _moveUnits.Add(new()
                {
                    Radius = 2 + _random.NextSingle() * 2,
                    Position = new(mouse.X / WorldScale, mouse.Y / WorldScale),
                });
                _units.Add(new()
                {
                    Speed = 20 + 10 * _random.NextSingle(),
                    Acceleration = 20 + 20 * _random.NextSingle(),
                    Decceleration = 400 + 400 * _random.NextSingle(),
                    RotationSpeed = MathF.PI * 2 + _random.NextSingle() * MathF.PI * 4,
                    Rotation = _random.NextSingle() * MathF.PI * 2 - MathF.PI,
                });
            }

            var target = new Vector2(mouse.X / WorldScale, mouse.Y / WorldScale);
            _flowField = _pathFinder.GetFlowField(_grid, 4, target);
            foreach (var i in _selection)
            {
                units[i].Target = target;
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
            for (var i = 0; i < moveUnits.Length; i++)
            {
                ref readonly var m = ref moveUnits[i];
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

        if (_selection.Count == 1)
        {
            var m = _moveUnits[_selection[0]];
            var u = _units[_selection[0]];
            Window.Title = $"[{_selection[0]}] r: {(int)m.Radius} a: {(int)u.Acceleration}/{(int)u.Decceleration} s: {(int)m.Velocity.Length()}/{(int)u.Speed} ";
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

        DrawGrid(_grid);

        Checkbox("Show FlowField", ref _showFlowField);

        if (Button("Delete Unit") && _units.Count > 0)
        {
            _moveUnits.RemoveAt(_moveUnits.Count - 1);
            _units.RemoveAt(_units.Count - 1);
        }

        var moveUnits = CollectionsMarshal.AsSpan(_moveUnits);
        var units = CollectionsMarshal.AsSpan(_units);

        if (_flowField != null && _showFlowField)
            DrawFlowField(_flowField.Value);

        for (var i = 0; i < units.Length; i++)
        {
            ref readonly var m = ref moveUnits[i];
            ref readonly var u = ref units[i];

            var color = u.Flags.HasFlag(UnitFlags.HasContact) ? Color.IndianRed :
                u.Target != null ? Color.Green
                : _selection.Contains(i) ? Color.Orange : Color.DarkSlateBlue;

            _spriteBatch.Draw(
                arrow,
                m.Position * WorldScale,
                null,
                color: color,
                rotation: u.Rotation,
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
                var v = flowfield.GetDirection(position);
                if (v == default)
                    continue;
                var rotation = MathF.Atan2(v.Y, v.X);
                var color = flowfield.FlowField.Vectors[x + y * grid.Width].flags.HasFlag(FlowFieldFlags.TurnPoint)
                    ? Color.Gray : Color.Gray * 0.2f;

                _spriteBatch.Draw(
                    arrow,
                    position * WorldScale,
                    null,
                    color,
                    rotation,
                    new Vector2(arrow.Width / 2, arrow.Height / 2),
                    _grid.Step * WorldScale / arrow.Width,
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