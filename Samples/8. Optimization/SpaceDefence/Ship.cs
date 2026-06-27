using System;
using SpaceDefence.Collision;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace SpaceDefence
{
    public class Ship : GameObject
    {
        public Vector2 Velocity { get; private set; }
        public float speed = 100;
        public float Range = 500;

        public float AvoidanceRange = 100;
        public float cooldown = 1;
        public float health = 100;
        private Texture2D ship_body;
        private Color[] bodyData;
        private Texture2D fadedBody;
        private Texture2D base_turret;
        private Color[] turretData;
        private Texture2D fadedTurret;
        private Color[] bodyTintData;
        private Color[] turretTintData;
        private float _cachedHealthPercent = -1f;
        private RectangleCollider _rectangleCollider;
        private Point target;
        private Color teamColor;

        /// <summary>
        /// The player character
        /// </summary>
        /// <param name="Position">The ship's starting position</param>
        public Ship(Point Position, CollisionType collisionType, Color teamColor)
        {
            _rectangleCollider = new RectangleCollider(new Rectangle(Position, Point.Zero));
            SetCollider(_rectangleCollider);
            CollisionType = collisionType | CollisionType.Solid;
            this.teamColor = teamColor;
        }

        public override void Load(ContentManager content)
        {
            // Original ship sprites from: https://zintoki.itch.io/space-breaker

            // Setting up the texture data so we can apply our colouring later
            ship_body = content.Load<Texture2D>("ship_body");
            fadedBody = new Texture2D(ship_body.GraphicsDevice, ship_body.Width, ship_body.Height);
            bodyData = new Color[ship_body.Width * ship_body.Height];
            bodyTintData = new Color[bodyData.Length];
            ship_body.GetData<Color>(bodyData);

            base_turret = content.Load<Texture2D>("base_turret");
            turretData = new Color[base_turret.Width * base_turret.Height];
            turretTintData = new Color[turretData.Length];
            base_turret.GetData<Color>(turretData);
            fadedTurret = new Texture2D(base_turret.GraphicsDevice, base_turret.Width, base_turret.Height);
            
            _rectangleCollider.shape.Size = ship_body.Bounds.Size;
            _rectangleCollider.shape.Location -= new Point(ship_body.Width/2, ship_body.Height/2);
            base.Load(content);
        }

        public override void HandleInput(InputManager inputManager)
        {
            base.HandleInput(inputManager);
            if(inputManager.LeftMousePress())
            {
                Shoot();
            }
        }

        public override void OnCollision(GameObject other)
        {
            base.OnCollision(other);
            
            if (other is Bullet && (other.CollisionType & CollisionType) == 0)
            {
                health -= 1;
                if (health < 0)
                {
                    GameManager.GetGameManager().RemoveGameObject(this);
                    var data = new ParticleData();
                    data.lifespan = 5;
                    data.particleCount = 40;
                    data.maxScale = .6f;
                    data.minScale = .2f;
                    new ParticleEmitter(GetPosition().Center.ToVector2(), data).Emit();
                }
            }
        }

        
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            cooldown -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            var nearest = FindNearestEnemy();
            target = nearest == null ? Point.Zero : nearest.GetPosition().Center;

            if( (target -GetPosition().Center).ToVector2().Length() < Range)
            {
                if(cooldown < 0)
                {
                    _rectangleCollider.shape.Location += Shoot();
                }
            }
            else
            {
                _rectangleCollider.shape.Location += (Vector2.Normalize((target -GetPosition().Center).ToVector2()) * speed  * (float)gameTime.ElapsedGameTime.TotalSeconds).ToPoint();
            }
            _rectangleCollider.shape.Location += (AvoidObstacles()* (float)gameTime.ElapsedGameTime.TotalSeconds).ToPoint();

        }

        public Point Shoot()
        {
            cooldown = 0.5f;
            var aimDirection = LinePieceCollider.GetDirection(GetPosition().Center, target);
            var turretExit = _rectangleCollider.shape.Center.ToVector2() + aimDirection * base_turret.Height / 2f;
            GameManager.GetGameManager().AddGameObject(new Bullet(turretExit, aimDirection, 150, CollisionType));

            return (-aimDirection * 20).ToPoint();
        }

        public Vector2 AvoidObstacles()
        {
            var avoidance = Vector2.Zero;
            var myCenter = GetPosition().Center;
            var avoidanceRangeSquared = AvoidanceRange * AvoidanceRange;
            var avoidanceStrength = (float)Math.Sqrt(AvoidanceRange) * speed;
            foreach(var other in GameManager.GetGameManager().GetShips())
            {
                if(other == this)
                    continue;

                var difference = (myCenter - other.GetPosition().Center).ToVector2();
                var distanceSquared = difference.LengthSquared();
                if(distanceSquared >= avoidanceRangeSquared)
                    continue;

                var distance = (float)Math.Sqrt(distanceSquared);
                avoidance += avoidanceStrength * difference/(distance * (float)Math.Sqrt(distance));
            }
            return avoidance;
        }

        public Ship FindNearestEnemy()
        {
            Ship nearest = null;
            var nearestDistance = float.MaxValue;
            var myPosition = GetPosition().Center.ToVector2();
            
            foreach(var othership in GameManager.GetGameManager().GetShips())
            {
                if(othership == this)
                    continue;
                if((othership.CollisionType & CollisionType.Teams) == (CollisionType & CollisionType.Teams))
                    continue;
                
                //quick to find nearest
                var newDistance = Vector2.DistanceSquared(othership.GetPosition().Center.ToVector2(), myPosition);
                if (newDistance >= nearestDistance)
                    continue;
                nearest = othership;
                nearestDistance = newDistance;
            }
            return nearest;
        }

        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            var percentage = health * 0.01f;
            if (Math.Abs(_cachedHealthPercent - percentage) > 0.001) //my rider was complaining i cant compare floats 
            {
                ReplaceAndFadeTexture(bodyData, bodyTintData, fadedBody, teamColor, percentage);
                ReplaceAndFadeTexture(turretData, turretTintData, fadedTurret, teamColor, percentage);
                _cachedHealthPercent = percentage;
            }

            spriteBatch.Draw(fadedBody, _rectangleCollider.shape, Color.White);
            var aimAngle = LinePieceCollider.GetAngle(LinePieceCollider.GetDirection(GetPosition().Center, target));
            var turretLocation = base_turret.Bounds;
            turretLocation.Location = _rectangleCollider.shape.Center;
            spriteBatch.Draw(fadedTurret, turretLocation, null, Color.White, aimAngle, turretLocation.Size.ToVector2() / 2f, SpriteEffects.None, 0);

            base.Draw(gameTime, spriteBatch);
        }

        /// <summary>
        /// Add team colors to the shipa and slowly fade them as they grow weaker.
        /// </summary>
        /// <param name="textureData">An array with the original ship texture data</param>
        /// <param name="target">The buffer on the graphics card to write the data to</param>
        /// <param name="color">The color to make the ship (alpha is ignored)</param>
        /// <param name="percentage">The percentage of health left</param>
        public static void ReplaceAndFadeTexture(
            Color[] textureData,
            Color[] targetData,
            Texture2D target,
            Color color,
            float percentage)
        {
            percentage = MathHelper.Clamp(percentage, 0f, 1f);

            var redM = (color.R * percentage) / 255;
            var greenM = (color.G * percentage) / 255;
            var blueM = (color.B * percentage) / 255;

            for (var i = 0; i < targetData.Length; i++)
            {
                var sourcePixel = textureData[i];

                if (sourcePixel.R == sourcePixel.B &&
                    sourcePixel.G == 0 &&
                    sourcePixel.R != 0)
                {
                    targetData[i].R = (byte)(sourcePixel.R * redM);
                    targetData[i].G = (byte)(sourcePixel.R * greenM);
                    targetData[i].B = (byte)(sourcePixel.R * blueM);
                    targetData[i].A = sourcePixel.A;
                }
                else targetData[i] = sourcePixel;
                
            }

            target.SetData(targetData);
        }

    }
}

