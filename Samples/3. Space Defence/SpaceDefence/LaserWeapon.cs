using Microsoft.Xna.Framework;

namespace SpaceDefence
{
    public class LaserWeapon : Weapon
    {
        public LaserWeapon(Ship owner) : base(owner, "laser_turret", 0.45f)
        {
        }

        public override string Name => "Laser";

        protected override void Fire(Vector2 targetPosition)
        {
            Vector2 direction = GetAimDirection(targetPosition);
            Vector2 turretExit = GetTurretExit(targetPosition);
            GameManager.GetGameManager().AddGameObject(new Laser(new LinePieceCollider(turretExit, direction, 900f), 900f, 3f));
        }
    }
}
