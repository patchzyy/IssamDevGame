using SpaceDefence.Collision;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace SpaceDefence
{
    public class Ship : GameObject
    {
        private readonly RectangleCollider _rectangleCollider;
        private Texture2D ship_body;
        private Vector2 _velocity;
        private Vector2 _lastAccelerationDirection;
        private float _shipRotation;
        private Vector2 _target;
        private Weapon _defaultWeapon;
        private Weapon _currentWeapon;
        private float _temporaryWeaponTimer;
        private Planet _lastVisitedPlanet;
        private bool _isDead;

        public float MaxHealth { get; } = 5f;
        public float Health { get; private set; } = 5f;
        public bool HasCargo { get; private set; }
        public string CurrentWeaponName => _currentWeapon == null ? "Cannon" : _currentWeapon.Name;

        /// <summary>
        /// The player character
        /// </summary>
        /// <param name="Position">The ship's starting position</param>
        public Ship(Point Position)
        {
            _rectangleCollider = new RectangleCollider(new Rectangle(Position, Point.Zero));
            SetCollider(_rectangleCollider);
            _lastAccelerationDirection = -Vector2.UnitY;
            _target = Position.ToVector2() - Vector2.UnitY * 100;
        }

        public override void Load(ContentManager content)
        {
            ship_body = content.Load<Texture2D>("ship_body");
            _rectangleCollider.shape.Size = ship_body.Bounds.Size;
            _rectangleCollider.shape.Location -= new Point(ship_body.Width / 2, ship_body.Height / 2);

            _defaultWeapon = new CannonWeapon(this);
            _defaultWeapon.Load(content);
            _currentWeapon = _defaultWeapon;
            Health = MaxHealth;
            HasCargo = false;
            _isDead = false;

            base.Load(content);
        }

        public override void HandleInput(InputManager inputManager)
        {
            _target = GameManager.GetGameManager().ScreenToWorld(inputManager.CurrentMouseState.Position.ToVector2());

            if (inputManager.LeftMousePress() && _currentWeapon != null)
                _currentWeapon.TryFire(_target);
        }

        public override void Update(GameTime gameTime)
        {
            var elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
            var inputDirection = Vector2.Zero;

            var inputManager = GameManager.GetGameManager().InputManager;
            if (inputManager.IsKeyDown(Keys.W))
                inputDirection.Y -= 1;
            if (inputManager.IsKeyDown(Keys.S))
                inputDirection.Y += 1;
            if (inputManager.IsKeyDown(Keys.A))
                inputDirection.X -= 1;
            if (inputManager.IsKeyDown(Keys.D))
                inputDirection.X += 1;

            if (inputDirection != Vector2.Zero)
            {
                inputDirection.Normalize();
                _velocity += inputDirection * 420f * elapsedSeconds;
                if (_velocity.Length() > 430f)
                {
                    _velocity.Normalize();
                    _velocity *= 430f;
                }

                _lastAccelerationDirection = inputDirection;
                _shipRotation = LinePieceCollider.GetAngle(_lastAccelerationDirection);
            }

            _rectangleCollider.shape.Location += (_velocity * elapsedSeconds).ToPoint();
            _rectangleCollider.shape = GameManager.GetGameManager().ClampToWorld(_rectangleCollider.shape);

            if (_rectangleCollider.shape.Left == GameManager.GetGameManager().WorldBounds.Left && _velocity.X < 0)
                _velocity.X = 0;
            if (_rectangleCollider.shape.Right == GameManager.GetGameManager().WorldBounds.Right && _velocity.X > 0)
                _velocity.X = 0;
            if (_rectangleCollider.shape.Top == GameManager.GetGameManager().WorldBounds.Top && _velocity.Y < 0)
                _velocity.Y = 0;
            if (_rectangleCollider.shape.Bottom == GameManager.GetGameManager().WorldBounds.Bottom && _velocity.Y > 0)
                _velocity.Y = 0;

            if (_temporaryWeaponTimer > 0)
            {
                _temporaryWeaponTimer -= elapsedSeconds;
                if (_temporaryWeaponTimer <= 0)
                    _currentWeapon = _defaultWeapon;
            }

            _currentWeapon?.Update(gameTime);

            if (_lastVisitedPlanet != null)
            {
                var releaseDistance = _lastVisitedPlanet.Radius + _rectangleCollider.shape.Width;
                if (Vector2.Distance(_rectangleCollider.shape.Center.ToVector2(), _lastVisitedPlanet.Center) > releaseDistance)
                    _lastVisitedPlanet = null;
            }

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            var shipCenter = _rectangleCollider.shape.Center.ToVector2();

            spriteBatch.Draw(
                ship_body,
                shipCenter,
                null,
                Color.White,
                _shipRotation,
                ship_body.Bounds.Size.ToVector2() / 2f,
                1f,
                SpriteEffects.None,
                0);

            _currentWeapon?.Draw(gameTime, spriteBatch, _rectangleCollider.shape, _target);
            DrawHealthBar(spriteBatch);
            base.Draw(gameTime, spriteBatch);
        }

        public void Buff()
        {
            EquipTemporaryWeapon(new LaserWeapon(this), 10f);
        }

        public void EquipLightningWeapon()
        {
            EquipTemporaryWeapon(new LightningWeapon(this), 8f);
        }

        public void VisitPlanet(Planet planet)
        {
            if (_lastVisitedPlanet == planet)
                return;

            _lastVisitedPlanet = planet;
            if (planet.IsPickupPlanet)
            {
                if (!HasCargo)
                    HasCargo = true;
            }
            else if (HasCargo)
            {
                HasCargo = false;
                GameManager.GetGameManager().AddScore(10);
            }
        }

        public void Kill()
        {
            if (_isDead)
                return;

            _isDead = true;
            Health = 0;
            GameManager.GetGameManager().SpawnExplosion(_rectangleCollider.shape.Center.ToVector2(), 1.2f);
            GameManager.GetGameManager().RemoveGameObject(this);
            GameManager.GetGameManager().TriggerGameOver();
        }

        public Rectangle GetPosition()
        {
            return _rectangleCollider.shape;
        }

        public Vector2 GetAimDirection(Vector2 targetPosition)
        {
            return LinePieceCollider.GetDirection(GetPosition().Center.ToVector2(), targetPosition);
        }

        public Vector2 GetFacingDirection()
        {
            return _lastAccelerationDirection;
        }

        private void EquipTemporaryWeapon(Weapon weapon, float duration)
        {
            weapon.Load(GameManager.GetGameManager().Content);
            _currentWeapon = weapon;
            _temporaryWeaponTimer = duration;
        }

        private void DrawHealthBar(SpriteBatch spriteBatch)
        {
            var pixel = GameManager.GetGameManager().Pixel;
            if (pixel == null)
                return;

            var barBackground = new Rectangle(_rectangleCollider.shape.X, _rectangleCollider.shape.Y - 18, _rectangleCollider.shape.Width, 8);
            var barFill = new Rectangle(barBackground.X + 1, barBackground.Y + 1, (int)((barBackground.Width - 2) * (Health / MaxHealth)), barBackground.Height - 2);
            spriteBatch.Draw(pixel, barBackground, Color.Black);
            spriteBatch.Draw(pixel, barFill, Color.LimeGreen);
        }
    }
}
