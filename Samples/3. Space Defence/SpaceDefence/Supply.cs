using SpaceDefence.Collision;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace SpaceDefence
{
    public class Supply : GameObject
    {
        private readonly WeaponPickupType _pickupType;
        private RectangleCollider _rectangleCollider;
        private Texture2D _texture;

        public Supply() : this(WeaponPickupType.Laser)
        {
        }

        public Supply(WeaponPickupType pickupType)
        {
            _pickupType = pickupType;
        }

        public override void Load(ContentManager content)
        {
            _texture = content.Load<Texture2D>("Crate");
            _rectangleCollider = new RectangleCollider(_texture.Bounds);
            SetCollider(_rectangleCollider);
            RandomMove();
            base.Load(content);
        }

        public override void OnCollision(GameObject other)
        {
            if (other is not Bullet && other is not Laser && other is not Ship)
                return;

            Ship player = GameManager.GetGameManager().Player;
            if (_pickupType == WeaponPickupType.Laser)
                player.Buff();
            else
                player.EquipLightningWeapon();

            RandomMove();
            base.OnCollision(other);
        }

        public void RandomMove()
        {
            GameManager gm = GameManager.GetGameManager();
            Vector2 playerPosition = gm.Player.GetPosition().Center.ToVector2();
            _rectangleCollider.shape.Location = Point.Zero;

            for (int i = 0; i < 50; i++)
            {
                Vector2 candidate = gm.FindOpenWorldLocation(150f, playerPosition, 350f);
                Rectangle candidateRectangle = new Rectangle((candidate - _texture.Bounds.Size.ToVector2() / 2f).ToPoint(), _texture.Bounds.Size);
                if (!candidateRectangle.Intersects(gm.Player.GetPosition()))
                {
                    _rectangleCollider.shape.Location = candidateRectangle.Location;
                    return;
                }
            }

            _rectangleCollider.shape.Location = (gm.RandomWorldLocation() - _texture.Bounds.Size.ToVector2() / 2f).ToPoint();
        }

        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            Color tint = _pickupType == WeaponPickupType.Laser ? Color.White : Color.Cyan;
            spriteBatch.Draw(_texture, _rectangleCollider.shape, tint);
            base.Draw(gameTime, spriteBatch);
        }
    }
}
