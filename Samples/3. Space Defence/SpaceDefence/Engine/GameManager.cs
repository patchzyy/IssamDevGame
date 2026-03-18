using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace SpaceDefence
{
    public class GameManager
    {
        private static GameManager gameManager;

        private readonly List<GameObject> _gameObjects;
        private readonly List<GameObject> _toBeRemoved;
        private readonly List<GameObject> _toBeAdded;
        private ContentManager _content;
        private Texture2D _backgroundTexture;
        private float _enemySpawnTimer;
        private float _enemySpawnInterval;
        private float _nextAlienSpeed;
        private bool _contentLoaded;

        public Random RNG { get; private set; }
        public Ship Player { get; private set; }
        public InputManager InputManager { get; private set; }
        public Game Game { get; private set; }
        public Camera Camera { get; private set; }
        public Rectangle WorldBounds { get; private set; }
        public int Score { get; private set; }
        public bool GameOverRequested { get; private set; }
        public Texture2D Pixel { get; private set; }
        public ContentManager Content => _content;

        public static GameManager GetGameManager()
        {
            if (gameManager == null)
                gameManager = new();
            return gameManager;
        }

        public GameManager()
        {
            _gameObjects = new();
            _toBeRemoved = new();
            _toBeAdded = new();
            InputManager = new();
            RNG = new();
            Camera = new();
            WorldBounds = new(0, 0, 4800, 3200);
        }

        public void Initialize(ContentManager content, Game game, Ship player)
        {
            Game = game;
            _content = content;
            Player = player;
        }

        public void Load(ContentManager content)
        {
            _content = content;
            _backgroundTexture = content.Load<Texture2D>("stars_texture");
            Pixel = new(Game.GraphicsDevice, 1, 1);
            Pixel.SetData([Color.White]);
            _contentLoaded = true;
            ApplyPendingChanges();
        }

        public void StartNewGame()
        {
            _gameObjects.Clear();
            _toBeRemoved.Clear();
            _toBeAdded.Clear();

            Score = 0;
            GameOverRequested = false;
            _enemySpawnInterval = 6f;
            _enemySpawnTimer = _enemySpawnInterval;
            _nextAlienSpeed = 120f;

            var playerPosition = new Point(WorldBounds.Center.X, WorldBounds.Center.Y);
            Player = new(playerPosition);

            AddGameObject(Player);
            AddGameObject(new Planet(new(WorldBounds.Left + 650, WorldBounds.Center.Y - 350), "EarthPlanet", "Earth", true));
            AddGameObject(new Planet(new(WorldBounds.Right - 650, WorldBounds.Center.Y + 350), "AlienPlanet", "Outpost", false));
            AddGameObject(new Supply());
            AddGameObject(new Supply(WeaponPickupType.Lightning));

            for (var i = 0; i < 5; i++)
            {
                AddGameObject(new Asteroid(FindOpenWorldLocation(200f, Player.GetPosition().Center.ToVector2(), 550f)));
            }

            SpawnAlien(_nextAlienSpeed);

            if (_contentLoaded)
                ApplyPendingChanges();

            UpdateCamera();
        }

        public void HandleInput(InputManager inputManager)
        {
            foreach (var gameObject in _gameObjects)
            {
                gameObject.HandleInput(inputManager);
            }
        }

        public void CheckCollision()
        {
            for (var i = 0; i < _gameObjects.Count; i++)
            {
                for (var j = i + 1; j < _gameObjects.Count; j++)
                {
                    if (_gameObjects[i].CheckCollision(_gameObjects[j]))
                    {
                        _gameObjects[i].OnCollision(_gameObjects[j]);
                        _gameObjects[j].OnCollision(_gameObjects[i]);
                    }
                }
            }
        }

        public void Update(GameTime gameTime)
        {
            ApplyPendingChanges();
            HandleInput(InputManager);

            foreach (var gameObject in _gameObjects)
            {
                gameObject.Update(gameTime);
            }

            CheckCollision();
            UpdateEnemySpawning((float)gameTime.ElapsedGameTime.TotalSeconds);
            ApplyPendingChanges();
            UpdateCamera();
        }

        public void UpdateEffects(GameTime gameTime)
        {
            foreach (var gameObject in _gameObjects)
            {
                if (gameObject is Explosion || gameObject is LightningStrike)
                    gameObject.Update(gameTime);
            }

            ApplyPendingChanges();
        }

        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            spriteBatch.Begin(transformMatrix: Camera.Transform);
            DrawBackground(spriteBatch);

            foreach (var gameObject in _gameObjects)
            {
                gameObject.Draw(gameTime, spriteBatch);
            }

            spriteBatch.End();
        }

        public void AddGameObject(GameObject gameObject)
        {
            if (_gameObjects.Contains(gameObject) || _toBeAdded.Contains(gameObject))
                return;

            _toBeAdded.Add(gameObject);
        }

        public void RemoveGameObject(GameObject gameObject)
        {
            if (_toBeRemoved.Contains(gameObject))
                return;

            _toBeRemoved.Add(gameObject);
        }

        public List<GameObject> GetGameObjects()
        {
            return _gameObjects;
        }

        public void AddScore(int amount)
        {
            Score += amount;
        }

        public Vector2 RandomScreenLocation()
        {
            return RandomWorldLocation();
        }

        public Vector2 RandomWorldLocation(float margin = 120f)
        {
            var left = WorldBounds.Left + (int)margin;
            var right = WorldBounds.Right - (int)margin;
            var top = WorldBounds.Top + (int)margin;
            var bottom = WorldBounds.Bottom - (int)margin;

            return new(
                RNG.Next(left, right),
                RNG.Next(top, bottom));
        }

        public Vector2 FindOpenWorldLocation(float margin, Vector2 avoidPoint, float clearance)
        {
            var earthPosition = new Vector2(WorldBounds.Left + 650, WorldBounds.Center.Y - 350);
            var outpostPosition = new Vector2(WorldBounds.Right - 650, WorldBounds.Center.Y + 350);

            for (var i = 0; i < 50; i++)
            {
                var candidate = RandomWorldLocation(margin);
                if (Vector2.Distance(candidate, avoidPoint) < clearance)
                    continue;
                if (Vector2.Distance(candidate, earthPosition) < 300f)
                    continue;
                if (Vector2.Distance(candidate, outpostPosition) < 300f)
                    continue;

                return candidate;
            }

            return RandomWorldLocation(margin);
        }

        public Rectangle ClampToWorld(Rectangle rectangle)
        {
            var x = Math.Clamp(rectangle.X, WorldBounds.Left, WorldBounds.Right - rectangle.Width);
            var y = Math.Clamp(rectangle.Y, WorldBounds.Top, WorldBounds.Bottom - rectangle.Height);
            return new(x, y, rectangle.Width, rectangle.Height);
        }

        public bool IsInsideWorld(Vector2 position, float margin = 0f)
        {
            return position.X >= WorldBounds.Left - margin
                && position.X <= WorldBounds.Right + margin
                && position.Y >= WorldBounds.Top - margin
                && position.Y <= WorldBounds.Bottom + margin;
        }

        public Vector2 ScreenToWorld(Vector2 screenPosition)
        {
            return Camera.ScreenToWorld(screenPosition);
        }

        public void SpawnExplosion(Vector2 position, float scale = 1f)
        {
            AddGameObject(new Explosion(position, scale));
        }

        public void NotifyAlienDestroyed(float alienSpeed)
        {
            _nextAlienSpeed = Math.Max(_nextAlienSpeed, alienSpeed + 12f);

            if (!GameOverRequested)
                SpawnAlien(_nextAlienSpeed);
        }

        public void TriggerGameOver()
        {
            GameOverRequested = true;
        }

        private void SpawnAlien(float speed)
        {
            if (Player == null)
                return;

            var spawnPosition = FindOpenWorldLocation(200f, Player.GetPosition().Center.ToVector2(), 700f);
            AddGameObject(new Alien(spawnPosition, speed));
        }

        private void UpdateEnemySpawning(float elapsedSeconds)
        {
            if (GameOverRequested || Player == null)
                return;

            _enemySpawnTimer -= elapsedSeconds;
            if (_enemySpawnTimer > 0)
                return;

            _nextAlienSpeed += 5f;
            SpawnAlien(_nextAlienSpeed);
            _enemySpawnInterval = Math.Max(2.5f, _enemySpawnInterval - 0.2f);
            _enemySpawnTimer = _enemySpawnInterval;
        }

        private void ApplyPendingChanges()
        {
            if (_contentLoaded)
            {
                foreach (var gameObject in _toBeAdded)
                {
                    gameObject.Load(_content);
                    _gameObjects.Add(gameObject);
                }
            }

            _toBeAdded.Clear();

            foreach (var gameObject in _toBeRemoved)
            {
                gameObject.Destroy();
                _gameObjects.Remove(gameObject);
            }

            _toBeRemoved.Clear();
        }

        private void UpdateCamera()
        {
            if (Player == null || Game == null)
                return;

            Camera.Follow(Player.GetPosition(), Game.GraphicsDevice.Viewport, WorldBounds);
        }

        private void DrawBackground(SpriteBatch spriteBatch)
        {
            if (_backgroundTexture == null)
                return;

            for (var x = WorldBounds.Left; x < WorldBounds.Right; x += _backgroundTexture.Width)
            {
                for (var y = WorldBounds.Top; y < WorldBounds.Bottom; y += _backgroundTexture.Height)
                {
                    spriteBatch.Draw(_backgroundTexture, new Vector2(x, y), Color.White);
                }
            }
        }
    }
}
