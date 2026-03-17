using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace SpaceDefence
{
    public class Alien : GameObject
    {
        private CircleCollider _circleCollider;
        private Texture2D _texture;
        private bool _isDead;

        public float Speed { get; }
        public float MaxHealth { get; } = 3f;
        public float Health { get; private set; } = 3f;

        public Alien() : this(Vector2.Zero, GameManager.GetGameManager().RandomWorldLocation(), 120f)
        {
        }

        public Alien(float speed) : this(GameManager.GetGameManager().RandomWorldLocation(), speed)
        {
        }

        public Alien(Vector2 spawnPosition, float speed) : this(Vector2.Zero, spawnPosition, speed)
        {
        }

        private Alien(Vector2 unused, Vector2 spawnPosition, float speed)
        {
            _circleCollider = new CircleCollider(spawnPosition, 24);
            SetCollider(_circleCollider);
            Speed = speed;
        }

        public override void Load(ContentManager content)
        {
            _texture = content.Load<Texture2D>("Alien");
            _circleCollider.Radius = _texture.Width / 2f;
            Health = MaxHealth;
            _isDead = false;
            base.Load(content);
        }

        public override void Update(GameTime gameTime)
        {
            Ship player = GameManager.GetGameManager().Player;
            if (player == null)
                return;

            Vector2 direction = player.GetPosition().Center.ToVector2() - _circleCollider.Center;
            if (direction.LengthSquared() > 0.001f)
            {
                direction.Normalize();
                _circleCollider.Center += direction * Speed * (float)gameTime.ElapsedGameTime.TotalSeconds;
            }

            if (Vector2.Distance(_circleCollider.Center, player.GetPosition().Center.ToVector2()) < 90f)
                player.Kill();

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(_texture, _circleCollider.GetBoundingBox(), Color.White);
            DrawHealthBar(spriteBatch);
            base.Draw(gameTime, spriteBatch);
        }

        public void TakeDamage(float damage)
        {
            if (_isDead)
                return;

            Health -= damage;
            if (Health <= 0)
                Kill();
        }

        public void Kill()
        {
            if (_isDead)
                return;

            _isDead = true;
            GameManager.GetGameManager().SpawnExplosion(_circleCollider.Center, 1f);
            GameManager.GetGameManager().RemoveGameObject(this);
            GameManager.GetGameManager().NotifyAlienDestroyed(Speed);
        }

        private void DrawHealthBar(SpriteBatch spriteBatch)
        {
            Texture2D pixel = GameManager.GetGameManager().Pixel;
            if (pixel == null)
                return;

            Rectangle bounds = _circleCollider.GetBoundingBox();
            Rectangle barBackground = new Rectangle(bounds.X, bounds.Y - 14, bounds.Width, 6);
            Rectangle barFill = new Rectangle(barBackground.X + 1, barBackground.Y + 1, (int)((barBackground.Width - 2) * (Health / MaxHealth)), barBackground.Height - 2);
            spriteBatch.Draw(pixel, barBackground, Color.Black);
            spriteBatch.Draw(pixel, barFill, Color.OrangeRed);
        }
    }
}
