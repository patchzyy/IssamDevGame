using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace SpaceDefence
{
    public class Explosion : GameObject
    {
        private readonly Vector2 _position;
        private readonly float _scale;
        private Texture2D _texture;
        private SpriteSheetAnimation _animation;

        public Explosion(Vector2 position, float scale)
        {
            _position = position;
            _scale = scale;
        }

        public override void Load(ContentManager content)
        {
            _texture = content.Load<Texture2D>("Explosion");
            _animation = new(_texture, 64, 64, _texture.Width / 64, 0.03f, false);
            base.Load(content);
        }

        public override void Update(GameTime gameTime)
        {
            _animation.Update(gameTime);
            if (_animation.Finished)
                GameManager.GetGameManager().RemoveGameObject(this);

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            var source = _animation.GetSourceRectangle();
            var destination = new Rectangle((int)(_position.X - source.Width * _scale / 2f), (int)(_position.Y - source.Height * _scale / 2f), (int)(source.Width * _scale), (int)(source.Height * _scale));
            spriteBatch.Draw(_texture, destination, source, Color.White);
            base.Draw(gameTime, spriteBatch);
        }
    }
}
