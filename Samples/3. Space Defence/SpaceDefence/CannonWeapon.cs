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
            var direction = GetAimDirection(targetPosition);
            var turretExit = GetTurretExit(targetPosition);
            GameManager.GetGameManager().AddGameObject(new Bullet(turretExit, direction, 700f));
        }
    }
}
