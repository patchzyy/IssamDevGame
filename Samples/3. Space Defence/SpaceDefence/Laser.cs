using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace SpaceDefence
{
    public class Laser : GameObject
    {
        private readonly HashSet<GameObject> _hitObjects;
        private readonly LinePieceCollider linePiece;
        private Texture2D sprite;
        private double lifespan = 0.15f;

        public float Damage { get; }

        public Laser(LinePieceCollider linePiece) : this(linePiece, linePiece.Length, 3f)
        {
        }

        public Laser(LinePieceCollider linePiece, float length) : this(linePiece, length, 3f)
        {
        }

        public Laser(LinePieceCollider linePiece, float length, float damage)
        {
            this.linePiece = linePiece;
            this.linePiece.Length = length;
            Damage = damage;
            _hitObjects = new HashSet<GameObject>();
            SetCollider(this.linePiece);
        }

        public override void Load(ContentManager content)
        {
            sprite = content.Load<Texture2D>("Laser");
            base.Load(content);
        }

        public override void Update(GameTime gameTime)
        {
            lifespan -= gameTime.ElapsedGameTime.TotalSeconds;
            if (lifespan <= 0)
                GameManager.GetGameManager().RemoveGameObject(this);

            base.Update(gameTime);
        }

        public override void OnCollision(GameObject other)
        {
            if (!_hitObjects.Add(other))
                return;

            if (other is Alien alien)
                alien.TakeDamage(Damage);
        }

        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            Rectangle target = new Rectangle((int)linePiece.Start.X, (int)linePiece.Start.Y, sprite.Width, (int)linePiece.Length);
            spriteBatch.Draw(sprite, target, null, Color.White, linePiece.GetAngle(), new Vector2(sprite.Width / 2f, sprite.Height), SpriteEffects.None, 0);
            base.Draw(gameTime, spriteBatch);
        }
    }
}
