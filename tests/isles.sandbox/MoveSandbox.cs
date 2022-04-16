// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Isles.Graphics;

Directory.SetCurrentDirectory(AppContext.BaseDirectory);

using var sandbox = new MoveSandbox();
sandbox.Run();

class MoveSandbox : Game
{
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
                Radius = 20 + 20 * random.NextSingle(),
                Position = new(random.NextSingle() * Window.ClientBounds.Width, random.NextSingle() * Window.ClientBounds.Height),
                Speed = 200 + 200 * random.NextSingle(),
                Facing = new(random.NextSingle(), random.NextSingle()),
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

        void UpdateSelection()
        {
            var mouse = Mouse.GetState();
            if (mouse.RightButton == ButtonState.Pressed)
            {
                foreach (var i in _selection)
                {
                    _units[i].Target = new(mouse.X, mouse.Y);
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
                    if (rectangle.Contains((int)unit.Position.X, (int)unit.Position.Y))
                    {
                        _selection.Add(i);
                    }
                }
            }
            else
            {
                _selectStart = _selectEnd = default;
            }
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

            _spriteBatch.Draw(
                arrow,
                unit.Position,
                null,
                color: _selection.Contains(i) ? Color.Orange : Color.DarkSlateBlue,
                rotation: MathF.Atan2(unit.Facing.Y, unit.Facing.X),
                origin: new(arrow.Width / 2, arrow.Height / 2),
                scale: unit.Radius * 2 / arrow.Width,
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