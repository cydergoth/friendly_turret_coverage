using UnityEngine;

namespace FriendlyTurretCoverage
{
    /// <summary>
    /// What one kind of weapon on one turret can hit. A turret with weapons of two kinds (say a
    /// beam and a point-defence cannon) gets one of these for each, drawn on top of each other.
    ///
    /// Angles are compass headings in degrees, the way Unity's <c>eulerAngles.y</c> measures them:
    /// 0 points along +Z and they increase clockwise seen from above.
    /// </summary>
    internal sealed class FieldOfFire
    {
        public long Key;
        public WeaponTurret Turret;
        public Transform Station;
        public DamageKind Kind;
        public float Range;
        /// <summary>Guided missiles: the game fires them at anything in range, with no arc or line-of-fire check.</summary>
        public bool Unrestricted;
        /// <summary>The turret turns all the way round.</summary>
        public bool FullCircle;
        /// <summary>Degrees per second the turret turns at.</summary>
        public float TurnRate;

        /// <summary>The scan that last found this turret.</summary>
        public int Seen;
        /// <summary>The shape is out of date and must be measured again soon.</summary>
        public bool Dirty = true;
        public float NextRefresh;

        // The measured shape: how far the turret can fire along each of a fan of rays.
        public Vector3 Origin;
        public float StartAngle;
        public float Span;
        public float StepAngle;
        public float[] Radii;
        public Mesh Mesh;

        public bool HasShape => Radii != null && Mesh != null;

        /// <summary>How far the turret can fire towards <paramref name="angle"/>, interpolated between the measured rays.</summary>
        public float RadiusAt(float angle)
        {
            float offset = FullCircle ? Mathf.Repeat(angle - StartAngle, 360f) : Mathf.Clamp(angle - StartAngle, 0f, Span);
            float index = offset / StepAngle;
            int count = Radii.Length;
            int below = Mathf.Min(Mathf.FloorToInt(index), count - 1);
            int above = below + 1;
            if (above >= count)
            {
                above = FullCircle ? 0 : count - 1;
            }
            return Mathf.Lerp(Radii[below], Radii[above], index - below);
        }

        public static Vector3 Heading(float angle)
        {
            float radians = angle * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(radians), 0f, Mathf.Cos(radians));
        }
    }
}
