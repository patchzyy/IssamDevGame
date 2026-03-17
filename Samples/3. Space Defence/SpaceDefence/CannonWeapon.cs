using Microsoft.Xna.Framework;

namespace SpaceDefence
{
    public class CannonWeapon : Weapon
    {
        public CannonWeapon(Ship owner) : base(owner, "base_turret", 0.25f)
        {
        }

        public override string Name => "Cannon";

        protected override void Fire(Vector2 targetPosition)
        {
            Vector2 direction = GetAimDirection(targetPosition);
            Vector2 turretExit = GetTurretExit(targetPosition);
            GameManager.GetGameManager().AddGameObject(new Bullet(turretExit, direction, 700f));
        }
    }
}
