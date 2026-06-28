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
        private HashSet<GameObject> _toBeRemovedSet;
        private List<GameObject> _toBeAdded;
        private List<GameObject> _colObjects;
        private List<Rectangle> _colBounds;
        private Dictionary<Point, List<int>> _colBuckets;
        private List<Point> _activeColBuckets;
        private int[] _seenColCandid;
        private int _collCandidateMarker;
        private List<Ship> _ships;
        private List<Ship> _team1Ships;
        private List<Ship> _team2Ships;
        private Dictionary<Point, List<Ship>> _avoidanceGrid;
        private Dictionary<Ship, Point> _shipAvoidanceCell;
        private List<Ship> _nearShips;
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
            _toBeRemovedSet = new HashSet<GameObject>();
            _toBeAdded = new List<GameObject>();
            _colObjects = new List<GameObject>();
            _colBounds = new List<Rectangle>();
            _colBuckets = new Dictionary<Point, List<int>>();
            _activeColBuckets = new List<Point>();
            _seenColCandid = Array.Empty<int>();
            _ships = new List<Ship>();
            _team1Ships = new List<Ship>();
            _team2Ships = new List<Ship>();
            _avoidanceGrid = new Dictionary<Point, List<Ship>>();
            _shipAvoidanceCell = new Dictionary<Ship, Point>();
            _nearShips = new List<Ship>();
            InputManager = new InputManager();
            RNG = new Random();
            //WorldMatrix = Matrix.CreateScale(.3f);
            WorldMatrix = Matrix.CreateScale(0.8f) * Matrix.CreateTranslation(0, -600, 0);
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
            RebuildAvoidanceGrid();
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
                {
                    _ships.Add(ship);
                    GetTeamShips(ship.CollisionType).Add(ship);
                }
                _gameObjects.Add(gameObject);
            }
            _toBeAdded.Clear();

            RemoveQueuedGameObjects();
        }

        private void RemoveQueuedGameObjects()
        {
            if (_toBeRemoved.Count == 0)
                return;

            foreach (var gameObject in _toBeRemoved)
                gameObject.Destroy();

            _gameObjects.RemoveAll(gameObject => _toBeRemovedSet.Contains(gameObject));
            _ships.RemoveAll(ship => _toBeRemovedSet.Contains(ship));
            _team1Ships.RemoveAll(ship => _toBeRemovedSet.Contains(ship));
            _team2Ships.RemoveAll(ship => _toBeRemovedSet.Contains(ship));
            foreach (var gameObject in _toBeRemoved)
            {
                if (gameObject is Ship ship)
                    _shipAvoidanceCell.Remove(ship);
            }
            _toBeRemoved.Clear();
            _toBeRemovedSet.Clear();
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
            if (_toBeRemovedSet.Add(gameObject))
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

        public List<Ship> GetShipsNear(Point center, float range)
        {
            _nearShips.Clear();

            var left = (int)(center.X - range);
            var top = (int)(center.Y - range);
            var right = (int)(center.X + range);
            var bottom = (int)(center.Y + range);

            var topLeftCell = GetAvoidanceCell(new Point(left, top));
            var bottomRightCell = GetAvoidanceCell(new Point(right, bottom));

            for (var x = topLeftCell.X; x <= bottomRightCell.X; x++)
            {
                for (var y = topLeftCell.Y; y <= bottomRightCell.Y; y++)
                {
                    var cell = new Point(x, y);
                    if (_avoidanceGrid.TryGetValue(cell, out var shipsInCell))
                        _nearShips.AddRange(shipsInCell);
                }
            }

            return _nearShips;
        }

        public void UpdateShipInAvoidanceGrid(Ship ship)
        {
            var newCell = GetAvoidanceCell(ship.Center);
            if (_shipAvoidanceCell.TryGetValue(ship, out var oldCell))
            {
                if (oldCell == newCell)
                    return;

                if (_avoidanceGrid.TryGetValue(oldCell, out var oldShips))
                    oldShips.Remove(ship);
            }

            AddShipToAvoidanceGrid(ship, newCell);
        }

        public List<Ship> GetEnemyShips(CollisionType collisionType)
        {
            var isTeam1 = (collisionType & CollisionType.Team1) != 0;
            if (isTeam1)
                return _team2Ships;

            return _team1Ships;
        }

        private List<Ship> GetTeamShips(CollisionType collisionType)
        {
            var isTeam1 = (collisionType & CollisionType.Team1) != 0;
            if (isTeam1)
                return _team1Ships;

            return _team2Ships;
        }

        private void RebuildAvoidanceGrid()
        {
            foreach (var shipsInCell in _avoidanceGrid.Values)
                shipsInCell.Clear();

            _shipAvoidanceCell.Clear();

            foreach (var ship in _ships)
                AddShipToAvoidanceGrid(ship, GetAvoidanceCell(ship.Center));
        }

        private void AddShipToAvoidanceGrid(Ship ship, Point cell)
        {
            if (!_avoidanceGrid.TryGetValue(cell, out var shipsInCell))
            {
                shipsInCell = new List<Ship>();
                _avoidanceGrid.Add(cell, shipsInCell);
            }

            shipsInCell.Add(ship);
            _shipAvoidanceCell[ship] = cell;
        }

        private static Point GetAvoidanceCell(Point point)
        {
            var x = (int)Math.Floor(point.X / (double)CollisionGridSize);
            var y = (int)Math.Floor(point.Y / (double)CollisionGridSize);
            return new Point(x, y);
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
