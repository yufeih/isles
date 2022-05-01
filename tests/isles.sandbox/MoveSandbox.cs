// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Isles.Graphics;

Directory.SetCurrentDirectory(AppContext.BaseDirectory);

using var sandbox = new MoveSandbox();
sandbox.Run();

class MoveSandbox : Game
{
    private const float WorldScale = 20f;

    private readonly Movable[] _units = new Movable[20];
    private readonly List<int> _selection = new();
    private readonly Move _move = new();

    private Point _selectStart, _selectEnd;

    private TextureLoader _textureLoader;
    private SpriteBatch _spriteBatch;

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

        var random = new Random();
        for (var i = 0; i < _units.Length; i++)
        {
            _units[i] = new()
            {
                Radius = 1 + random.NextSingle(),
                Position = new(random.NextSingle() * Window.ClientBounds.Width / WorldScale, random.NextSingle() * Window.ClientBounds.Height / WorldScale),
                Speed = 10 + 10 * random.NextSingle(),
                Acceleration = 20 + 20 * random.NextSingle(),
                Decceleration = 400 + 400 * random.NextSingle(),
                RotationSpeed = MathF.PI * 2 + random.NextSingle() * MathF.PI * 4,
                Rotation = random.NextSingle() * MathF.PI * 2 - MathF.PI,
            };
        }
    }

    protected override void LoadContent()
    {
        _textureLoader = new(GraphicsDevice);
        _spriteBatch = new(GraphicsDevice);
    }

    protected override void Update(GameTime gameTime)
    {
        UpdateSelection();

        _move.Update((float)gameTime.ElapsedGameTime.TotalSeconds, _units);
    }

    private void UpdateSelection()
    {
        var mouse = Mouse.GetState();
        if (mouse.RightButton == ButtonState.Pressed)
        {
            foreach (var i in _selection)
            {
                _units[i].Target = new(mouse.X / WorldScale, mouse.Y / WorldScale);
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
            for (var i = 0; i < _units.Length; i++)
            {
                ref readonly var unit = ref _units[i];
                if (rectangle.Contains((int)(unit.Position.X * WorldScale), (int)(unit.Position.Y * WorldScale)))
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
            var unit = _units[_selection[0]];
            Window.Title = $"[{_selection[0]}] r: {(int)unit.Radius} a: {(int)unit.Acceleration}/{(int)unit.Decceleration} s: {(int)unit.Velocity.Length()}/{(int)unit.Speed} ";
        }
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.White);

        var arrow = _textureLoader.LoadTexture("data/arrow.svg");
        var selection = _textureLoader.LoadTexture("data/selection.svg");

        _spriteBatch.Begin();

        for (var i = 0; i < _units.Length; i++)
        {
            ref readonly var unit = ref _units[i];

            var color = unit.Flags.HasFlag(MovableFlags.HasContact)
                ? Color.IndianRed
                : _selection.Contains(i) ? Color.Orange : Color.DarkSlateBlue;

            _spriteBatch.Draw(
                arrow,
                unit.Position * WorldScale,
                null,
                color: color,
                rotation: unit.Rotation,
                origin: new(arrow.Width / 2, arrow.Height / 2),
                scale: unit.Radius * 2 * WorldScale / arrow.Width,
                SpriteEffects.None,
                0);
        }

        if (_selectStart != _selectEnd)
        {
            _spriteBatch.Draw(selection, GetSelectionRectangle(), Color.Green * 0.5f);
        }

        _spriteBatch.End();
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