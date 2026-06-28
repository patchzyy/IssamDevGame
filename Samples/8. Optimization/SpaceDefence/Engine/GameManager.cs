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
        private List<GameObject> _colObjects;
        private List<Rectangle> _colBounds;
        private Dictionary<Point, List<int>> _colBuckets;
        private List<Point> _activeColBuckets;
        private int[] _seenColCandid;
        private int _collCandidateMarker;
        private List<Ship> _ships;
        private ContentManager _content;
        private const int CollisionGridSize = 128;
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
            _colObjects = new List<GameObject>();
            _colBounds = new List<Rectangle>();
            _colBuckets = new Dictionary<Point, List<int>>();
            _activeColBuckets = new List<Point>();
            _seenColCandid = Array.Empty<int>();
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
            ClearCollisionGrid();
            CollectCollidersForCollision();
            SweepCollisionGrid();
        }

        private void CollectCollidersForCollision()
        {
            _colObjects.Clear();
            _colBounds.Clear();
            foreach (var gameObject in _gameObjects)
            {
                if (!gameObject.HasCollider)
                    continue;

                var bounds = gameObject.GetCollisionBounds();
                bounds.Inflate(1, 1); //jsut to be safe
                _colObjects.Add(gameObject);
                _colBounds.Add(bounds);
            }

            if (_seenColCandid.Length < _colObjects.Count)
                Array.Resize(ref _seenColCandid, _colObjects.Count);
        }

        private void SweepCollisionGrid()
        {
            for (var i = 0; i < _colObjects.Count; i++)
            {
                var bounds = _colBounds[i];
                var minCell = GetCollisionCell(bounds.Left, bounds.Top);
                var maxCell = GetCollisionCell(bounds.Right - 1, bounds.Bottom - 1);

                StartCollisionCandidatePass();
                for (var x = minCell.X; x <= maxCell.X; x++)
                {
                    for (var y = minCell.Y; y <= maxCell.Y; y++)
                    {
                        var bucket = GetCollisionBucket(new Point(x, y));
                        var wasEmpty = bucket.Count == 0;

                        foreach (var candidateIndex in bucket)
                        {
                            if (_seenColCandid[candidateIndex] == _collCandidateMarker)
                                continue;

                            _seenColCandid[candidateIndex] = _collCandidateMarker;
                            CheckCollisionPair(candidateIndex, i);
                        }

                        bucket.Add(i);
                        if (wasEmpty)
                            _activeColBuckets.Add(new Point(x, y));
                    }
                }
            }
            
        }

        private void ClearCollisionGrid()
        {
            foreach (var cell in _activeColBuckets)
                _colBuckets[cell].Clear();

            _activeColBuckets.Clear();
        }

        private List<int> GetCollisionBucket(Point cell)
        {
            if (_colBuckets.TryGetValue(cell, out var bucket))
                return bucket;

            bucket = new List<int>();
            _colBuckets.Add(cell, bucket);
            return bucket;
        }

        private void StartCollisionCandidatePass()
        {
            _collCandidateMarker++;
            if (_collCandidateMarker != int.MaxValue)
                return;

            Array.Clear(_seenColCandid, 0, _seenColCandid.Length);
            _collCandidateMarker = 1;
        }

        private void CheckCollisionPair(int firstIndex, int secondIndex)
        {
            var first = _colObjects[firstIndex];
            var second = _colObjects[secondIndex];
            
            if ((first.CollisionType & second.CollisionType) != 0)
                return;
            
            if (first is Bullet && second is Bullet)
                return;
            
            if (!_colBounds[firstIndex].Intersects(_colBounds[secondIndex]))
                return;

            if (!first.CheckCollision(second)) 
                return;
            
            first.OnCollision(second);
            second.OnCollision(first);
        }

        private static Point GetCollisionCell(int x, int y)
        {
            var newx = (int)Math.Floor(x / (double)CollisionGridSize);
            var newy = (int)Math.Floor(y / (double)CollisionGridSize);
            return new  Point(newx, newy);
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
