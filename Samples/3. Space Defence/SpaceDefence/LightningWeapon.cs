using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

namespace SpaceDefence
{
    public class LightningWeapon : Weapon
    {
        public LightningWeapon(Ship owner) : base(owner, "laser_turret", 0.7f)
        {
            turretColor = Color.Cyan;
        }

        public override string Name => "Lightning";

        public override void Load(ContentManager content)
        {
            base.Load(content);
        }

        protected override void Fire(Vector2 targetPosition)
        {
            Alien firstTarget = null;
            float bestDistance = 180f * 180f;

            foreach (GameObject gameObject in GameManager.GetGameManager().GetGameObjects())
            {
                if (gameObject is not Alien alien)
                    continue;

                float distance = Vector2.DistanceSquared(alien.GetBounds().Center.ToVector2(), targetPosition);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    firstTarget = alien;
                }
            }

            if (firstTarget == null)
                return;

            List<Vector2> strikePoints = new List<Vector2>
            {
                GetTurretExit(targetPosition),
                firstTarget.GetBounds().Center.ToVector2()
            };

            firstTarget.TakeDamage(2f);

            Alien previousTarget = firstTarget;
            int jumps = 0;
            foreach (GameObject gameObject in GameManager.GetGameManager().GetGameObjects())
            {
                if (jumps >= 2)
                    break;
                if (gameObject is not Alien alien || alien == firstTarget)
                    continue;

                float distance = Vector2.Distance(previousTarget.GetBounds().Center.ToVector2(), alien.GetBounds().Center.ToVector2());
                if (distance > 220f)
                    continue;

                alien.TakeDamage(1f);
                strikePoints.Add(alien.GetBounds().Center.ToVector2());
                previousTarget = alien;
                jumps++;
            }

            GameManager.GetGameManager().AddGameObject(new LightningStrike(strikePoints));
        }
    }
}
