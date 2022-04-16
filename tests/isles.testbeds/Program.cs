// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Isles.Graphics;

Directory.SetCurrentDirectory(AppContext.BaseDirectory);

using var testBeds = new MoveTestBeds();
testBeds.Run();

class MoveTestBeds : Game
{
    private readonly Movable[] _units = new Movable[20];
    private readonly List<int> _selection = new();
    private readonly Move _move = new();

    private TextureLoader _textureLoader;
    private SpriteBatch _spriteBatch;

    public MoveTestBeds()
    {
        var random = new Random();
        var size = 100;
        for (var i = 0; i < _units.Length; i++)
        {
            _units[i] = new()
            {
                Radius = 1.0f + 1.0f * random.NextSingle(),
                Position = new(random.NextSingle() * size, random.NextSingle() * size),
                Speed = 2.0f * random.NextSingle(),
            };
        }

        new GraphicsDeviceManager(this);
    }

    protected override void LoadContent()
    {
        _textureLoader = new(GraphicsDevice);
        _spriteBatch = new(GraphicsDevice);
    }

    protected override void Update(GameTime gameTime)
    {
        _move.Update((float)gameTime.ElapsedGameTime.TotalSeconds, _units);
    }

    protected override void Draw(GameTime gameTime)
    {
        var arrow = _textureLoader.LoadTexture("data/arrow.svg");
        _spriteBatch.Begin();

        for (var i = 0; i < _units.Length; i++)
        {
            ref readonly var unit = ref _units[i];

            _spriteBatch.Draw(arrow, unit.Position, Color.White);
        }

        _spriteBatch.End();
    }
}