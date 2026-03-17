using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace SpaceDefence
{
    public abstract class Weapon
    {
        private readonly string _textureName;
        protected Texture2D turretTexture;
        protected Color turretColor = Color.White;
        protected float cooldown;
        protected float cooldownTimer;

        protected Weapon(Ship owner, string textureName, float cooldown)
        {
            Owner = owner;
            _textureName = textureName;
            this.cooldown = cooldown;
        }

        protected Ship Owner { get; }
        public abstract string Name { get; }

        public virtual void Load(ContentManager content)
        {
            if (content != null && !string.IsNullOrEmpty(_textureName))
                turretTexture = content.Load<Texture2D>(_textureName);
        }

        public virtual void Update(GameTime gameTime)
        {
            if (cooldownTimer > 0)
                cooldownTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
        }

        public bool TryFire(Vector2 targetPosition)
        {
            if (cooldownTimer > 0)
                return false;

            Fire(targetPosition);
            cooldownTimer = cooldown;
            return true;
        }

        public virtual void Draw(GameTime gameTime, SpriteBatch spriteBatch, Rectangle shipBounds, Vector2 targetPosition)
        {
            if (turretTexture == null)
                return;

            float aimAngle = LinePieceCollider.GetAngle(Owner.GetAimDirection(targetPosition));
            Vector2 shipCenter = shipBounds.Center.ToVector2();

            spriteBatch.Draw(
                turretTexture,
                shipCenter,
                null,
                turretColor,
                aimAngle,
                turretTexture.Bounds.Size.ToVector2() / 2f,
                1f,
                SpriteEffects.None,
                0);
        }

        protected Vector2 GetAimDirection(Vector2 targetPosition)
        {
            Vector2 direction = Owner.GetAimDirection(targetPosition);
            return direction.LengthSquared() < 0.001f ? Owner.GetFacingDirection() : direction;
        }

        protected Vector2 GetTurretExit(Vector2 targetPosition)
        {
            Vector2 direction = GetAimDirection(targetPosition);
            float offset = turretTexture == null ? 16f : turretTexture.Height / 2f;
            return Owner.GetPosition().Center.ToVector2() + direction * offset;
        }

        protected abstract void Fire(Vector2 targetPosition);
    }
}
