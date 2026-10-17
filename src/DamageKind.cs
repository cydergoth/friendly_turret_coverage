namespace FriendlyTurretCoverage
{
    /// <summary>The kinds of turret weapon the overlay can show separately. The values index the config arrays.</summary>
    internal enum DamageKind
    {
        Beam,
        Kinetic,
        Missile,
        PointDefence,
        Repair,
    }

    internal static class DamageKinds
    {
        public const int Count = 5;

        /// <summary>
        /// Sorts a weapon into one kind. A weapon can fit several (a point-defence beam, a repair
        /// beam), so the most specific role wins: repair, then missile, then point defence, then
        /// beam, and everything else fires projectiles.
        /// </summary>
        public static DamageKind Of(TWeapon weapon)
        {
            if (weapon.damageType == DamageType.Repair)
            {
                return DamageKind.Repair;
            }
            if (IsMissile(weapon) || weapon.type == WeaponType.Torpedo || weapon.compType == WeaponCompType.MissileObject)
            {
                return DamageKind.Missile;
            }
            if (weapon.canHitProjectiles)
            {
                return DamageKind.PointDefence;
            }
            if (weapon.compType == WeaponCompType.BeamWeaponObject)
            {
                return DamageKind.Beam;
            }
            return DamageKind.Kinetic;
        }

        /// <summary>
        /// Weapons the game lets a turret fire at anything in range, whichever way the turret
        /// faces (the guided missiles steer themselves). Torpedoes still need the turret's arc.
        /// </summary>
        public static bool IsMissile(TWeapon weapon)
        {
            return weapon.type == WeaponType.Missile || weapon.type == WeaponType.EnergyOrb;
        }

        public static string Describe(DamageKind kind)
        {
            switch (kind)
            {
                case DamageKind.Beam:
                    return "beam weapons";
                case DamageKind.Kinetic:
                    return "projectile (kinetic) weapons";
                case DamageKind.Missile:
                    return "missiles and torpedoes";
                case DamageKind.PointDefence:
                    return "point defence";
                default:
                    return "repair beams";
            }
        }
    }
}
