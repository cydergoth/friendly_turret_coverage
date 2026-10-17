using System.Collections.Generic;
using UnityEngine;

namespace FriendlyTurretCoverage
{
    /// <summary>
    /// Builds the meshes the overlay draws. They are white, with the opacity in the vertex
    /// colours, so one mesh can be drawn in any colour by tinting the material.
    /// </summary>
    internal static class CoverageMesh
    {
        /// <summary>
        /// Height the overlay is drawn at. Ships and stations sit on the plane y = 0, so drawing a
        /// little below it keeps them on top of the overlay.
        /// </summary>
        public const float PlaneHeight = -2f;

        // The soft edge along the end of a field of fire, as a fraction of the weapon's range.
        private const float EdgeFraction = 0.05f;
        // The traverse sweep: its line's width in pixels, how many degrees its fading trail
        // covers per degree-per-second of turn rate, and the most it covers.
        private const float SweepPixels = 2f;
        private const float TrailSeconds = 0.12f;
        private const float MaxTrailDegrees = 40f;
        private const int TrailSegments = 8;
        // The out-of-ammunition warning: its height and its gap above the turret, in pixels.
        private const float WarningPixels = 18f;
        private const float WarningGapPixels = 6f;

        private static readonly List<Vector3> vertices = new List<Vector3>();
        private static readonly List<Color32> colors = new List<Color32>();
        private static readonly List<int> triangles = new List<int>();

        /// <summary>
        /// A fan of rays from the turret, out to the measured radius of each, filled faintly with
        /// a brighter band along the far end. A limited arc also gets bright side edges. The mesh is
        /// built around the turret, so it is drawn with a translation to <see cref="Center"/>.
        /// </summary>
        public static void Build(FieldOfFire field, bool closed)
        {
            vertices.Clear();
            colors.Clear();
            triangles.Clear();

            Color32 fill = White(Plugin.FillOpacity.Value);
            Color32 edge = White(Plugin.EdgeOpacity.Value);
            float edgeWidth = field.Range * EdgeFraction;

            vertices.Add(Vector3.zero);
            colors.Add(fill);
            int count = field.Radii.Length;
            for (int i = 0; i < count; i++)
            {
                Vector3 heading = FieldOfFire.Heading(field.StartAngle + i * field.StepAngle);
                float radius = field.Radii[i];
                bool side = !closed && (i == 0 || i == count - 1);
                // Inner and outer vertex of the edge band on this ray.
                vertices.Add(heading * Mathf.Max(0f, radius - edgeWidth));
                colors.Add(side ? edge : fill);
                vertices.Add(heading * radius);
                colors.Add(edge);
            }

            int segments = closed ? count : count - 1;
            for (int i = 0; i < segments; i++)
            {
                int a = 1 + 2 * i;
                int b = 1 + 2 * ((i + 1) % count);
                triangles.Add(0);
                triangles.Add(a);
                triangles.Add(b);

                triangles.Add(a);
                triangles.Add(a + 1);
                triangles.Add(b + 1);

                triangles.Add(a);
                triangles.Add(b + 1);
                triangles.Add(b);
            }

            Mesh mesh = field.Mesh;
            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
        }

        /// <summary>Where a field of fire's mesh is drawn: under its turret, on the overlay plane.</summary>
        public static Vector3 Center(FieldOfFire field)
        {
            return new Vector3(field.Origin.x, PlaneHeight, field.Origin.z);
        }

        /// <summary>Starts a batch of sweeps or warnings, which <see cref="EndSweeps"/> puts in a mesh.</summary>
        public static void BeginSweeps()
        {
            vertices.Clear();
            colors.Clear();
            triangles.Clear();
        }

        /// <summary>
        /// Adds the targeting line of one hovered field of fire: a line from the turret to the end
        /// of its reach, swinging across the field at the turret's turn rate, with a fading trail.
        /// </summary>
        public static void AddSweep(FieldOfFire field, Color color, float time, Camera camera)
        {
            float angle;
            float direction = 1f;
            float rate = field.TurnRate;
            bool closed = field.FullCircle || field.Unrestricted;
            if (rate <= 0f)
            {
                // A turret that can't turn only fires where it points.
                angle = closed ? field.Turret.transform.eulerAngles.y : field.StartAngle + field.Span * 0.5f;
            }
            else if (closed)
            {
                angle = field.StartAngle + Mathf.Repeat(time * rate, 360f);
            }
            else
            {
                // Back and forth across the arc, like the turret tracking from one limit to the other.
                float travel = Mathf.Repeat(time * rate, 2f * field.Span);
                direction = travel < field.Span ? 1f : -1f;
                angle = field.StartAngle + Mathf.PingPong(time * rate, field.Span);
            }

            Vector3 center = Center(field);
            float halfWidth = 0.5f * SweepPixels * WorldPerPixel(camera, center);
            Vector3 heading = FieldOfFire.Heading(angle);
            Vector3 tip = center + heading * field.RadiusAt(angle);
            Vector3 side = new Vector3(heading.z, 0f, -heading.x) * halfWidth;

            Color32 line = color;
            line.a = 240;
            int start = vertices.Count;
            vertices.Add(center - side);
            vertices.Add(center + side);
            vertices.Add(tip + side);
            vertices.Add(tip - side);
            for (int i = 0; i < 4; i++)
            {
                colors.Add(line);
            }
            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
            triangles.Add(start);
            triangles.Add(start + 2);
            triangles.Add(start + 3);

            float trail = Mathf.Min(rate * TrailSeconds, MaxTrailDegrees, closed ? 360f : field.Span);
            if (trail <= 0f)
            {
                return;
            }
            int hub = vertices.Count;
            vertices.Add(center);
            colors.Add(WithAlpha(color, 0.3f));
            for (int i = 0; i <= TrailSegments; i++)
            {
                float t = (float)i / TrailSegments;
                float a = angle - direction * trail * t;
                if (!closed)
                {
                    a = Mathf.Clamp(a, field.StartAngle, field.StartAngle + field.Span);
                }
                vertices.Add(center + FieldOfFire.Heading(a) * field.RadiusAt(a));
                colors.Add(WithAlpha(color, 0.3f * (1f - t)));
                if (i > 0)
                {
                    triangles.Add(hub);
                    triangles.Add(hub + i);
                    triangles.Add(hub + i + 1);
                }
            }
        }

        public static void EndSweeps(Mesh mesh)
        {
            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
        }

        /// <summary>
        /// Adds a warning sign above a turret: a triangle with a dark border and an exclamation
        /// mark, yellow for low ammunition and red for none, facing the camera at a fixed size on screen.
        /// </summary>
        public static void AddWarning(Vector3 turret, bool severe, float alpha, Camera camera)
        {
            float pixel = WorldPerPixel(camera, turret);
            Vector3 right = camera.transform.right * pixel;
            Vector3 up = camera.transform.up * pixel;
            // Above the turret, so the turret itself stays visible for hovering.
            Vector3 center = turret + up * (WarningPixels * 0.5f + WarningGapPixels);
            float h = WarningPixels;
            float w = h * 1.15f;

            // Drawn in order, each part over the last.
            AddTriangle(center, right, up, w * 1.35f, h * 1.3f, -h * 0.05f, Fade(new Color(0.08f, 0.06f, 0f), alpha));
            AddTriangle(center, right, up, w, h, 0f, Fade(severe ? new Color(0.95f, 0.15f, 0.1f) : new Color(1f, 0.85f, 0.1f), alpha));
            Color32 glyph = Fade(severe ? Color.white : new Color(0.12f, 0.08f, 0f), alpha);
            AddRect(center, right, up, h * 0.13f, -h * 0.12f, h * 0.2f, glyph);
            AddRect(center, right, up, h * 0.13f, -h * 0.36f, -h * 0.23f, glyph);
        }

        /// <summary>An upright triangle <paramref name="w"/> wide and <paramref name="h"/> high, centred on <paramref name="center"/> raised by <paramref name="lift"/>.</summary>
        private static void AddTriangle(Vector3 center, Vector3 right, Vector3 up, float w, float h, float lift, Color32 color)
        {
            Vector3 middle = center + up * lift;
            AddPolygon(color,
                middle + up * (h * 0.5f),
                middle - up * (h * 0.5f) + right * (w * 0.5f),
                middle - up * (h * 0.5f) - right * (w * 0.5f));
        }

        /// <summary>A rectangle <paramref name="w"/> wide from <paramref name="bottom"/> to <paramref name="top"/> above <paramref name="center"/>.</summary>
        private static void AddRect(Vector3 center, Vector3 right, Vector3 up, float w, float bottom, float top, Color32 color)
        {
            Vector3 half = right * (w * 0.5f);
            AddPolygon(color, center + up * top - half, center + up * top + half, center + up * bottom + half, center + up * bottom - half);
        }

        /// <summary>A convex polygon, as a fan from its first corner.</summary>
        private static void AddPolygon(Color32 color, params Vector3[] corners)
        {
            int start = vertices.Count;
            foreach (Vector3 corner in corners)
            {
                vertices.Add(corner);
                colors.Add(color);
            }
            for (int i = 1; i < corners.Length - 1; i++)
            {
                triangles.Add(start);
                triangles.Add(start + i);
                triangles.Add(start + i + 1);
            }
        }

        private static Color32 Fade(Color color, float alpha)
        {
            color.a = Mathf.Clamp01(alpha);
            return color;
        }

        /// <summary>The size of one screen pixel in world units, at <paramref name="point"/>.</summary>
        private static float WorldPerPixel(Camera camera, Vector3 point)
        {
            float height = Mathf.Max(1, camera.pixelHeight);
            if (camera.orthographic)
            {
                return 2f * camera.orthographicSize / height;
            }
            float distance = Mathf.Max(0.01f, Vector3.Dot(point - camera.transform.position, camera.transform.forward));
            return 2f * distance * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad) / height;
        }

        private static Color32 White(float alpha)
        {
            return new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));
        }

        private static Color32 WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}
