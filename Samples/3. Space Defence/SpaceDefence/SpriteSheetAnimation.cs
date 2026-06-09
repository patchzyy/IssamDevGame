using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SpaceDefence
{
    public class SpriteSheetAnimation
    {
        private readonly Texture2D _texture;
        private readonly int _frameWidth;
        private readonly int _frameHeight;
        private readonly int _frameCount;
        private readonly float _frameDuration;
        private readonly bool _loops;
        private float _timer;
        private int _frameIndex;

        public SpriteSheetAnimation(Texture2D texture, int frameWidth, int frameHeight, int frameCount, float frameDuration, bool loops)
        {
            _texture = texture;
            _frameWidth = frameWidth;
            _frameHeight = frameHeight;
            _frameCount = frameCount;
            _frameDuration = frameDuration;
            _loops = loops;
        }

        public bool Finished { get; private set; }

        public void Update(GameTime gameTime)
        {
            if (Finished)
                return;

            _timer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            while (_timer >= _frameDuration)
            {
                _timer -= _frameDuration;
                _frameIndex++;

                if (_frameIndex < _frameCount)
                    continue;

                if (_loops)
                {
                    _frameIndex = 0;
                }
                else
                {
                    _frameIndex = _frameCount - 1;
                    Finished = true;
                    break;
                }
            }
        }

        public Rectangle GetSourceRectangle()
        {
            return new Rectangle(_frameIndex * _frameWidth, 0, _frameWidth, _frameHeight);
        }
    }
}
