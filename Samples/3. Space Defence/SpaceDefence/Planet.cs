using SpaceDefence.Collision;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace SpaceDefence
{
    public class Planet : GameObject
    {
        private readonly Vector2 _center;
        private readonly string _textureName;
        private Texture2D _texture;
        private SpriteSheetAnimation _animation;
        private CircleCollider _circleCollider;

        public Planet(Vector2 center, string textureName, string name, bool isPickupPlanet)
        {
            _center = center;
            _textureName = textureName;
            DisplayName = name;
            IsPickupPlanet = isPickupPlanet;
        }

        public string DisplayName { get; }
        public bool IsPickupPlanet { get; }
        public Vector2 Center => _circleCollider.Center;
        public float Radius => _circleCollider.Radius;

        public override void Load(ContentManager content)
        {
            _texture = content.Load<Texture2D>(_textureName);
            _animation = new SpriteSheetAnimation(_texture, 96, 96, _texture.Width / 96, 0.05f, true);
            _circleCollider = new CircleCollider(_center, 60f);
            SetCollider(_circleCollider);
            base.Load(content);
        }

        public override void Update(GameTime gameTime)
        {
            _animation.Update(gameTime);
            base.Update(gameTime);
        }

        public override void OnCollision(GameObject other)
        {
            if (other is Ship ship)
                ship.VisitPlanet(this);

            base.OnCollision(other);
        }

        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            var source = _animation.GetSourceRectangle();
            var destination = new Rectangle((int)(_center.X - 80), (int)(_center.Y - 80), 160, 160);
            spriteBatch.Draw(_texture, destination, source, Color.White);
            base.Draw(gameTime, spriteBatch);
        }
    }
}
