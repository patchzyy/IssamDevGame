using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace SpaceDefence
{
    public class LightningStrike : GameObject
    {
        private readonly List<Vector2> _points;
        private Texture2D _texture;
        private SpriteSheetAnimation _animation;
        private float _lifespan = 0.12f;

        public LightningStrike(List<Vector2> points)
        {
            _points = points;
        }

        public override void Load(ContentManager content)
        {
            _texture = content.Load<Texture2D>("lightning");
            _animation = new(_texture, 32, 32, _texture.Width / 32, 0.02f, true);
            base.Load(content);
        }

        public override void Update(GameTime gameTime)
        {
            _lifespan -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            _animation.Update(gameTime);
            if (_lifespan <= 0)
                GameManager.GetGameManager().RemoveGameObject(this);

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            var source = _animation.GetSourceRectangle();

            for (var i = 0; i < _points.Count - 1; i++)
            {
                var start = _points[i];
                var end = _points[i + 1];
                var difference = end - start;
                var length = difference.Length();
                if (length <= 0)
                    continue;

                var target = new Rectangle((int)start.X, (int)start.Y, 18, (int)length);
                spriteBatch.Draw(_texture, target, source, Color.White, LinePieceCollider.GetAngle(Vector2.Normalize(difference)), new Vector2(source.Width / 2f, source.Height), SpriteEffects.None, 0);
            }

            base.Draw(gameTime, spriteBatch);
        }
    }
}
