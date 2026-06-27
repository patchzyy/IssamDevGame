using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using SpaceDefence.Engine;

namespace SpaceDefence
{
    public class GameManager
    {
        private static GameManager gameManager;

        private FPSCounter counter;
        private List<GameObject> _gameObjects;
        private List<GameObject> _toBeRemoved;
        private List<GameObject> _toBeAdded;
        private List<GameObject> _collidableObjects;
        private List<Rectangle> _collidableBounds;
        private List<Ship> _ships;
        private ContentManager _content;
        public Matrix WorldMatrix { get; set; }

        public Random RNG { get; private set; }
        public InputManager InputManager { get; private set; }
        public Game Game { get; private set; }

        public static GameManager GetGameManager()
        {
            if(gameManager == null)
                gameManager = new GameManager();
            return gameManager;
        }
        public GameManager()
        {
            _gameObjects = new List<GameObject>();
            _toBeRemoved = new List<GameObject>();
            _toBeAdded = new List<GameObject>();
            _collidableObjects = new List<GameObject>();
            _collidableBounds = new List<Rectangle>();
            _ships = new List<Ship>();
            InputManager = new InputManager();
            RNG = new Random();
            WorldMatrix = Matrix.CreateScale(0.2f);
        }

        public void Initialize(ContentManager content, Game game)
        {
            Game = game;
            _content = content;
        }

        public void Load(ContentManager content)
        {
            foreach (GameObject gameObject in _gameObjects)
            {
                gameObject.Load(content);
            }
            counter = new FPSCounter(content.Load<SpriteFont>("Font"));
        }

        public void HandleInput(InputManager inputManager)
        {
            foreach (GameObject gameObject in _gameObjects)
            {
                gameObject.HandleInput(this.InputManager);
            }
        }

        public void CheckCollision()
        {
            _collidableObjects.Clear();
            _collidableBounds.Clear();
            foreach (var gameObject in _gameObjects)
            {
                if (!gameObject.HasCollider)
                    continue;

                var bounds = gameObject.GetCollisionBounds();
                bounds.Inflate(1, 1); //jsut to be safe
                _collidableObjects.Add(gameObject);
                _collidableBounds.Add(bounds);
            }
            
            for (var i = 0; i < _collidableObjects.Count; i++)
            {
                var first = _collidableObjects[i];
                var firstBounds = _collidableBounds[i];

                for (var j = i + 1; j < _collidableObjects.Count; j++)
                {
                    if (!firstBounds.Intersects(_collidableBounds[j]))
                        continue;

                    var second = _collidableObjects[j];
                    if (!first.CheckCollision(second)) continue;
                    first.OnCollision(second);
                    second.OnCollision(first);
                }
            }
            
        }
        
        public void Update(GameTime gameTime) 
        {
            InputManager.Update();
            // Handle input
            HandleInput(InputManager);


            // Update
            foreach (GameObject gameObject in _gameObjects)
            {
                gameObject.Update(gameTime);
            }

            // Check Collission
            CheckCollision();

            foreach (GameObject gameObject in _toBeAdded)
            {
                gameObject.Load(_content);
                if(gameObject is Ship ship)
                    _ships.Add(ship);
                _gameObjects.Add(gameObject);
            }
            _toBeAdded.Clear();

            foreach (GameObject gameObject in _toBeRemoved)
            {
                gameObject.Destroy();
                if(gameObject is Ship ship)
                    _ships.Remove(ship);
                _gameObjects.Remove(gameObject);
            }
            _toBeRemoved.Clear();
        }

        public void Draw(GameTime gameTime, SpriteBatch spriteBatch) 
        {
            spriteBatch.Begin(transformMatrix: WorldMatrix);
            foreach (GameObject gameObject in _gameObjects)
            {
                gameObject.Draw(gameTime, spriteBatch);
            }
            spriteBatch.End();
            spriteBatch.Begin();
            counter.Draw(gameTime, spriteBatch);
            spriteBatch.End();
        }

        /// <summary>
        /// Add a new GameObject to the GameManager. 
        /// The GameObject will be added at the start of the next Update step. 
        /// Once it is added, the GameManager will ensure all steps of the game loop will be called on the object automatically. 
        /// </summary>
        /// <param name="gameObject"> The GameObject to add. </param>
        public void AddGameObject(GameObject gameObject)
        {
            _toBeAdded.Add(gameObject);
        }

        /// <summary>
        /// Remove GameObject from the GameManager. 
        /// The GameObject will be removed at the start of the next Update step and its Destroy() mehtod will be called.
        /// After that the object will no longer receive any updates.
        /// </summary>
        /// <param name="gameObject"> The GameObject to Remove. </param>
        public void RemoveGameObject(GameObject gameObject)
        {
            _toBeRemoved.Add(gameObject);
        }

        public List<GameObject> GetGameObjects()
        {
            return _gameObjects;
        }

        public List<Ship> GetShips()
        {
            return _ships;
        }

        /// <summary>
        /// Get a random location on the screen.
        /// </summary>
        public Vector2 RandomScreenLocation()
        {
            return new Vector2(
                RNG.Next(0, Game.GraphicsDevice.Viewport.Width),
                RNG.Next(0, Game.GraphicsDevice.Viewport.Height));
        }
    }
}
