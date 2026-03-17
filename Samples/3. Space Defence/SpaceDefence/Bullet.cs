using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace SpaceDefence
{
    public class Bullet : GameObject
    {
        private Texture2D _texture;
        private readonly CircleCollider _circleCollider;
        private readonly Vector2 _velocity;

        public float Damage { get; }

        public Bullet(Vector2 location, Vector2 direction, float speed) : this(location, direction, speed, 1f)
        {
        }

        public Bullet(Vector2 location, Vector2 direction, float speed, float damage)
        {
            _circleCollider = new CircleCollider(location, 4);
            SetCollider(_circleCollider);
            _velocity = direction * speed;
            Damage = damage;
        }

        public override void Load(ContentManager content)
        {
            _texture = content.Load<Texture2D>("Bullet");
            base.Load(content);
        }

        public override void Update(GameTime gameTime)
        {
            _circleCollider.Center += _velocity * (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (!GameManager.GetGameManager().IsInsideWorld(_circleCollider.Center, 20f))
                GameManager.GetGameManager().RemoveGameObject(this);

            base.Update(gameTime);
        }

        public override void OnCollision(GameObject other)
        {
            if (other is Alien alien)
            {
                alien.TakeDamage(Damage);
                GameManager.GetGameManager().RemoveGameObject(this);
            }
            else if (other is Supply || other is Asteroid)
            {
                GameManager.GetGameManager().RemoveGameObject(this);
            }
        }

        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(_texture, _circleCollider.GetBoundingBox(), Color.Red);
            base.Draw(gameTime, spriteBatch);
        }
    }
}
