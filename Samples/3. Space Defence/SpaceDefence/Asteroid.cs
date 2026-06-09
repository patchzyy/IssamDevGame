using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace SpaceDefence
{
    public class Asteroid : GameObject
    {
        private readonly CircleCollider _circleCollider;
        private Texture2D _texture;

        public Asteroid(Vector2 center)
        {
            _circleCollider = new CircleCollider(center, 40f);
            SetCollider(_circleCollider);
        }

        public override void Load(ContentManager content)
        {
            _texture = content.Load<Texture2D>("Asteroid");
            _circleCollider.Radius = _texture.Width / 2.5f;
            base.Load(content);
        }

        public override void OnCollision(GameObject other)
        {
            if (other is Ship ship)
                ship.Kill();
            else if (other is Alien alien)
                alien.Kill();

            base.OnCollision(other);
        }

        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(_texture, _circleCollider.GetBoundingBox(), Color.White);
            base.Draw(gameTime, spriteBatch);
        }
    }
}
