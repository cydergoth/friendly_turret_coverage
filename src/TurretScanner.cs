using System.Collections.Generic;
using UnityEngine;

namespace FriendlyTurretCoverage
{
    /// <summary>
    /// Keeps the list of friendly station turrets in the sector and measures what each can hit.
    /// The measurements copy the checks <see cref="WeaponTurret"/> makes before it picks a target:
    /// the weapon's range, the turret's arc, and a clear line of fire.
    /// </summary>
    internal sealed class TurretScanner
    {
        // WeaponTurret.ClearLOF checks layers 8 (obstacles), 9 (ships), 10 (asteroids) and 13
        // (stations). Ships are left out here: they come and go, and a shadow behind every passing
        // ship would only flicker.
        private const int BlockingLayers = (1 << 8) | (1 << 10) | (1 << 13);
        // Rays cast plus mesh columns built per frame.
        private const int MaxRaysPerFrame = 2000;

        private readonly Dictionary<long, FieldOfFire> byKey = new Dictionary<long, FieldOfFire>();
        private readonly RaycastHit[] hits = new RaycastHit[32];
        private readonly float[] kindRange = new float[DamageKinds.Count];
        private readonly bool[] kindUnrestricted = new bool[DamageKinds.Count];
        private SpaceShip player;
        private int serial;

        /// <summary>Every field of fire found at the last <see cref="Scan"/>.</summary>
        public readonly List<FieldOfFire> Fields = new List<FieldOfFire>();

        /// <summary>Finds the friendly station turrets and the weapons on them.</summary>
        public void Scan()
        {
            if (player == null)
            {
                GameObject go = GameObject.FindGameObjectWithTag("Player");
                player = go != null ? go.GetComponent<SpaceShip>() : null;
                if (player == null)
                {
                    return;
                }
            }

            serial++;
            foreach (WeaponTurret turret in Object.FindObjectsOfType<WeaponTurret>())
            {
                AddTurret(turret);
            }

            for (int i = Fields.Count - 1; i >= 0; i--)
            {
                FieldOfFire field = Fields[i];
                if (field.Seen != serial)
                {
                    Forget(field);
                    Fields.RemoveAt(i);
                }
            }
        }

        private void AddTurret(WeaponTurret turret)
        {
            // The game finds a turret's ship the same way.
            Transform station = turret.transform.parent != null ? turret.transform.parent.parent : null;
            SpaceShip ss = station != null ? station.GetComponent<SpaceShip>() : null;
            if (ss == null || !ss.IsStation || ss.IsInactiveStation || ss.weapons == null)
            {
                return;
            }
            if (!turret.active && !Plugin.IncludeUnpowered.Value)
            {
                return;
            }
            if (player.ffSys == null || ss.ffSys == null || !player.ffSys.TargetIsFriendly(ss.ffSys))
            {
                return;
            }

            for (int k = 0; k < DamageKinds.Count; k++)
            {
                kindRange[k] = 0f;
                kindUnrestricted[k] = false;
            }
            foreach (Weapon weapon in ss.weapons)
            {
                if (weapon == null || weapon.wRef == null || weapon.turretMounted != turret.turretIndex)
                {
                    continue;
                }
                int k = (int)DamageKinds.Of(weapon.wRef);
                kindRange[k] = Mathf.Max(kindRange[k], weapon.range);
                kindUnrestricted[k] |= DamageKinds.IsMissile(weapon.wRef);
            }

            bool fullCircle = turret.type == WeaponTurretType.Rotating || turret.degreesLimit >= 360f;
            for (int k = 0; k < DamageKinds.Count; k++)
            {
                if (kindRange[k] <= 0f)
                {
                    continue;
                }
                long key = ((long)turret.GetInstanceID() << 3) | (long)k;
                if (!byKey.TryGetValue(key, out FieldOfFire field))
                {
                    field = new FieldOfFire { Key = key, Turret = turret, Station = station, Kind = (DamageKind)k };
                    byKey[key] = field;
                    Fields.Add(field);
                }
                field.Seen = serial;
                // A turret refitted with other weapons or moved is measured again straight away.
                if (field.Range != kindRange[k] || field.Unrestricted != kindUnrestricted[k] || field.FullCircle != fullCircle
                    || (field.Origin - turret.transform.position).sqrMagnitude > 1f)
                {
                    field.Dirty = true;
                }
                field.Range = kindRange[k];
                field.Unrestricted = kindUnrestricted[k];
                field.FullCircle = fullCircle;
                // WeaponTurret turns by Time.deltaTime * 10 * turnSpeed degrees a frame.
                field.TurnRate = 10f * turret.turnSpeed;
            }
        }

        private void Forget(FieldOfFire field)
        {
            byKey.Remove(field.Key);
            Object.Destroy(field.Mesh);
            field.Mesh = null;
        }

        public void Clear()
        {
            foreach (FieldOfFire field in Fields)
            {
                Object.Destroy(field.Mesh);
            }
            Fields.Clear();
            byKey.Clear();
        }

        /// <summary>Marks every shape out of date, for example after a config change.</summary>
        public void Invalidate()
        {
            foreach (FieldOfFire field in Fields)
            {
                field.Dirty = true;
            }
        }

        /// <summary>
        /// Measures the shapes that are out of date or due a recheck. The work is spread over
        /// frames so a sector full of turrets doesn't stall the game.
        /// </summary>
        public void Measure(float now)
        {
            int rays = 0;
            // Changed shapes first, so a refit shows up before routine rechecks.
            for (int pass = 0; pass < 2 && rays < MaxRaysPerFrame; pass++)
            {
                foreach (FieldOfFire field in Fields)
                {
                    bool due = pass == 0 ? field.Dirty : now >= field.NextRefresh;
                    if (!due || field.Turret == null || field.Station == null)
                    {
                        continue;
                    }
                    rays += Measure(field);
                    field.Dirty = false;
                    // A little jitter keeps the rechecks from bunching up in one frame again.
                    field.NextRefresh = now + Mathf.Max(0.25f, Plugin.RefreshSeconds.Value) * Random.Range(0.9f, 1.1f);
                    if (rays >= MaxRaysPerFrame)
                    {
                        break;
                    }
                }
            }
        }

        private int Measure(FieldOfFire field)
        {
            WeaponTurret turret = field.Turret;
            field.Origin = turret.transform.position;
            if (field.FullCircle || field.Unrestricted)
            {
                field.StartAngle = 0f;
                field.Span = 360f;
            }
            else
            {
                // WeaponTurret.CanFireAgainst measures the arc from the station's heading.
                field.Span = turret.degreesLimit;
                field.StartAngle = field.Station.eulerAngles.y + turret.baseDegreeRaw - field.Span * 0.5f;
            }

            bool closed = field.FullCircle || field.Unrestricted;
            float step = Mathf.Clamp(Plugin.ArcStepDegrees.Value, 0.25f, 15f);
            int segments = Mathf.Max(1, Mathf.CeilToInt(field.Span / step));
            field.StepAngle = field.Span / segments;
            // A closed circle's last ray would be its first one again.
            int count = closed ? segments : segments + 1;
            if (field.Radii == null || field.Radii.Length != count)
            {
                field.Radii = new float[count];
            }

            // Guided missiles ignore what's in the way, so they get the plain range circle.
            bool cast = Plugin.ShowBlocking.Value && !field.Unrestricted;
            for (int i = 0; i < count; i++)
            {
                float angle = field.StartAngle + i * field.StepAngle;
                field.Radii[i] = cast ? LineOfFire(field.Origin, FieldOfFire.Heading(angle), field.Range, field.Station) : field.Range;
            }

            if (field.Mesh == null)
            {
                field.Mesh = new Mesh { name = "TurretCoverage" };
                field.Mesh.MarkDynamic();
            }
            CoverageMesh.Build(field, closed);
            // Building the mesh costs about as much as a ray per vertex column, so count those too.
            return count;
        }

        /// <summary>How far a shot can go from <paramref name="origin"/> before something other than its own station stops it.</summary>
        private float LineOfFire(Vector3 origin, Vector3 direction, float range, Transform station)
        {
            int count = Physics.RaycastNonAlloc(origin, direction, hits, range, BlockingLayers, QueryTriggerInteraction.Ignore);
            float nearest = range;
            for (int i = 0; i < count; i++)
            {
                if (hits[i].distance >= nearest)
                {
                    continue;
                }
                Transform hit = hits[i].transform;
                // Station parts carry a ColliderControl that points at the station (see WeaponTurret.ClearLOF).
                if (hit.CompareTag("Collider"))
                {
                    ColliderControl control = hit.GetComponent<ColliderControl>();
                    if (control != null && control.ownerEntity != null)
                    {
                        hit = control.ownerTransform;
                    }
                }
                if (hit == station || hit.IsChildOf(station))
                {
                    continue;
                }
                nearest = hits[i].distance;
            }
            return nearest;
        }
    }
}
