using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace SpaceDefence
{
    public class SpaceDefence : Game
    {
        private enum GameScreen
        {
            Start,
            Playing,
            Paused,
            GameOver
        }

        private SpriteBatch _spriteBatch;
        private GraphicsDeviceManager _graphics;
        private GameManager _gameManager;
        private SpriteFont _font;
        private Texture2D _pixel;
        private GameScreen _screen = GameScreen.Start;

        public SpaceDefence()
        {
            _graphics = new GraphicsDeviceManager(this);
            _graphics.IsFullScreen = false;
            _graphics.PreferredBackBufferWidth = 2000;
            _graphics.PreferredBackBufferHeight = 1200;

            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            _gameManager = GameManager.GetGameManager();
            Ship player = new Ship(new Point(_graphics.PreferredBackBufferWidth / 2, _graphics.PreferredBackBufferHeight / 2));
            _gameManager.Initialize(Content, this, player);
            _gameManager.StartNewGame();
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _pixel = new Texture2D(GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });
            _font = Content.Load<SpriteFont>("HudFont");
            _gameManager.Load(Content);
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                Exit();

            _gameManager.InputManager.Update();

            switch (_screen)
            {
                case GameScreen.Start:
                    UpdateStartScreen();
                    break;
                case GameScreen.Playing:
                    UpdateGame(gameTime);
                    break;
                case GameScreen.Paused:
                    UpdatePauseScreen();
                    break;
                case GameScreen.GameOver:
                    UpdateGameOver(gameTime);
                    break;
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            _gameManager.Draw(gameTime, _spriteBatch);

            _spriteBatch.Begin();
            if (_screen != GameScreen.Start)
                DrawHud();

            if (_screen == GameScreen.Start)
                DrawOverlay("Space Defence", "Enter: Start\nQ: Quit\n\nWASD to accelerate\nClick to fire\nEsc to pause\n\nExtra feature: health bars for the player and enemies.");
            else if (_screen == GameScreen.Paused)
                DrawOverlay("Paused", "Enter or Esc: Continue\nQ: Quit");
            else if (_screen == GameScreen.GameOver)
                DrawOverlay("Game Over", "Enter: Restart\nQ: Quit");

            _spriteBatch.End();
            base.Draw(gameTime);
        }

        private void UpdateStartScreen()
        {
            if (_gameManager.InputManager.IsKeyPress(Keys.Enter))
            {
                _gameManager.StartNewGame();
                _screen = GameScreen.Playing;
            }
            else if (_gameManager.InputManager.IsKeyPress(Keys.Q))
            {
                Exit();
            }
        }

        private void UpdateGame(GameTime gameTime)
        {
            if (_gameManager.InputManager.IsKeyPress(Keys.Escape) || _gameManager.InputManager.IsKeyPress(Keys.P))
            {
                _screen = GameScreen.Paused;
                return;
            }

            _gameManager.Update(gameTime);
            if (_gameManager.GameOverRequested)
                _screen = GameScreen.GameOver;
        }

        private void UpdatePauseScreen()
        {
            if (_gameManager.InputManager.IsKeyPress(Keys.Enter) || _gameManager.InputManager.IsKeyPress(Keys.Escape))
            {
                _screen = GameScreen.Playing;
            }
            else if (_gameManager.InputManager.IsKeyPress(Keys.Q))
            {
                Exit();
            }
        }

        private void UpdateGameOver(GameTime gameTime)
        {
            _gameManager.UpdateEffects(gameTime);

            if (_gameManager.InputManager.IsKeyPress(Keys.Enter))
            {
                _gameManager.StartNewGame();
                _screen = GameScreen.Playing;
            }
            else if (_gameManager.InputManager.IsKeyPress(Keys.Q))
            {
                Exit();
            }
        }

        private void DrawHud()
        {
            Ship player = _gameManager.Player;
            if (player == null)
                return;

            _spriteBatch.DrawString(_font, $"Score: {_gameManager.Score}", new Vector2(24, 18), Color.White);
            _spriteBatch.DrawString(_font, $"Cargo: {(player.HasCargo ? "Loaded" : "Empty")}", new Vector2(24, 52), Color.White);
            _spriteBatch.DrawString(_font, $"Weapon: {player.CurrentWeaponName}", new Vector2(24, 86), Color.White);
        }

        private void DrawOverlay(string title, string body)
        {
            Rectangle panel = new Rectangle(GraphicsDevice.Viewport.Width / 2 - 340, GraphicsDevice.Viewport.Height / 2 - 160, 680, 320);
            _spriteBatch.Draw(_pixel, panel, Color.Black * 0.75f);

            Vector2 titleSize = _font.MeasureString(title);
            _spriteBatch.DrawString(_font, title, new Vector2(panel.Center.X - titleSize.X / 2f, panel.Y + 28), Color.Gold);
            _spriteBatch.DrawString(_font, body, new Vector2(panel.X + 32, panel.Y + 92), Color.White);
        }
    }
}
