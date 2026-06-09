using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SpaceDefence
{
    public class Camera
    {
        public Matrix Transform { get; private set; } = Matrix.Identity;

        public void Follow(Rectangle target, Viewport viewport, Rectangle worldBounds)
        {
            var cameraX = target.Center.X - viewport.Width / 2f;
            var cameraY = target.Center.Y - viewport.Height / 2f;

            cameraX = MathHelper.Clamp(cameraX, worldBounds.Left, worldBounds.Right - viewport.Width);
            cameraY = MathHelper.Clamp(cameraY, worldBounds.Top, worldBounds.Bottom - viewport.Height);

            Transform = Matrix.CreateTranslation(-cameraX, -cameraY, 0f);
        }

        public Vector2 ScreenToWorld(Vector2 screenPosition)
        {
            return Vector2.Transform(screenPosition, Matrix.Invert(Transform));
        }
    }
}
