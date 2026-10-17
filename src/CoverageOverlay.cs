using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace FriendlyTurretCoverage
{
    /// <summary>Where the player is looking, which decides the camera the overlay is drawn for.</summary>
    internal enum View
    {
        /// <summary>Docked, in a menu, or anything else without a view of space.</summary>
        None,
        Space,
        SectorMap,
    }

    /// <summary>
    /// Draws the fields of fire of friendly station turrets in space and on the sector map, and a
    /// sweeping targeting line on the turret under the mouse.
    ///
    /// The fields are meshes on a plane just below the ships, drawn with
    /// <see cref="Graphics.DrawMesh(Mesh, Matrix4x4, Material, int, Camera, int, MaterialPropertyBlock, bool, bool)"/>
    /// for the one camera that shows the current view, so nothing is added to the scene. They are
    /// translucent, so where fields overlap the colour builds up, and fields of different colours
    /// mix.
    /// </summary>
    internal sealed class CoverageOverlay : MonoBehaviour
    {
        private const string RootName = "FriendlyTurretCoverage";
        private const float ScanSeconds = 1f;
        // Turrets this close together (two weapon slots on one mount) sweep together on hover.
        private const float SameMountDistance = 2f;

        public static CoverageOverlay Instance;

        private readonly TurretScanner scanner = new TurretScanner();
        private readonly List<FieldOfFire> hovered = new List<FieldOfFire>();
        private readonly MaterialPropertyBlock[] kindTint = new MaterialPropertyBlock[DamageKinds.Count];
        private MaterialPropertyBlock singleTint;
        private MaterialPropertyBlock sweepTint;
        private MinimapControl minimap;
        private Material material;
        private Mesh sweepMesh;
        private CoveragePanel panel;
        private float nextScan;

        public static void Create(MinimapControl minimap)
        {
            if (Instance != null)
            {
                if (Instance.minimap == minimap)
                {
                    return;
                }
                Destroy(Instance.gameObject);
            }
            var go = new GameObject(RootName);
            Instance = go.AddComponent<CoverageOverlay>();
            Instance.Build(minimap);
        }

        private void Build(MinimapControl minimapControl)
        {
            minimap = minimapControl;
            for (int i = 0; i < DamageKinds.Count; i++)
            {
                kindTint[i] = new MaterialPropertyBlock();
            }
            singleTint = new MaterialPropertyBlock();
            sweepTint = new MaterialPropertyBlock();
            sweepMesh = new Mesh { name = "TurretCoverageSweep" };
            sweepMesh.MarkDynamic();
            panel = CoveragePanel.Create(this);
            ApplyConfig();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
            scanner.Clear();
            Destroy(sweepMesh);
            Destroy(material);
            if (panel != null)
            {
                Destroy(panel.gameObject);
            }
        }

        /// <summary>Picks up config changes. Called when the overlay is built and when a setting changes.</summary>
        public void ApplyConfig()
        {
            Destroy(material);
            material = CreateMaterial(Plugin.AdditiveBlending.Value);
            for (int i = 0; i < DamageKinds.Count; i++)
            {
                SetTint(kindTint[i], Plugin.KindColor[i].Value);
            }
            SetTint(singleTint, Plugin.SingleColor.Value);
            SetTint(sweepTint, Color.white);
            // Opacity, blocking and resolution are baked into the shapes.
            scanner.Invalidate();
            if (panel != null)
            {
                panel.Refresh();
            }
        }

        private static void SetTint(MaterialPropertyBlock block, Color color)
        {
            color.a = 1f;
            block.SetColor("_Color", color);
            // Used by the fallback sprite shader. Harmless for the others.
            block.SetColor("_RendererColor", Color.white);
        }

        private static Material CreateMaterial(bool additive)
        {
            // Hidden/Internal-Colored is Unity's built-in shader for plain coloured geometry (lines,
            // debug shapes). It is always in a player build and lets the blending be set per material.
            Shader shader = Shader.Find("Hidden/Internal-Colored") ?? Shader.Find("Sprites/Default");
            var mat = new Material(shader) { name = "TurretCoverage", hideFlags = HideFlags.HideAndDontSave };
            mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
            mat.SetInt("_Cull", (int)CullMode.Off);
            mat.SetInt("_ZWrite", 0);
            mat.SetInt("_ZTest", (int)CompareFunction.LessEqual);
            mat.renderQueue = (int)RenderQueue.Transparent;
            return mat;
        }

        /// <summary>The colour a damage kind is drawn in with the current settings.</summary>
        public static Color ColorOf(DamageKind kind)
        {
            Color color = Plugin.ColorByType.Value ? Plugin.KindColor[(int)kind].Value : Plugin.SingleColor.Value;
            color.a = 1f;
            return color;
        }

        public View CurrentView(out Camera camera)
        {
            camera = null;
            if (minimap == null)
            {
                return View.None;
            }
            if (minimap.isSectorMap)
            {
                camera = GameAccess.SectorMapCam(minimap);
                return camera != null && camera.enabled ? View.SectorMap : View.None;
            }
            camera = GameAccess.MainCam(minimap);
            GameObject hud = GameAccess.PlayerUI(minimap);
            return camera != null && camera.enabled && hud != null && hud.activeInHierarchy ? View.Space : View.None;
        }

        private void Update()
        {
            try
            {
                Step();
            }
            catch (Exception e)
            {
                // Leave the game running normally rather than log an error every frame.
                Plugin.Log.LogError("The turret coverage overlay failed and is switched off: " + e);
                enabled = false;
                if (panel != null)
                {
                    panel.gameObject.SetActive(false);
                }
            }
        }

        private void Step()
        {
            if (Plugin.ToggleKey.Value.IsDown())
            {
                Plugin.ShowCoverage.Value = !Plugin.ShowCoverage.Value;
            }

            View view = CurrentView(out Camera camera);
            if (panel != null)
            {
                panel.Show(view != View.None);
            }
            if (view == View.None || !Plugin.ShowCoverage.Value)
            {
                // Look again as soon as the overlay is back, rather than show what was there before.
                nextScan = 0f;
                return;
            }

            float now = Time.time;
            if (now >= nextScan)
            {
                nextScan = now + ScanSeconds;
                scanner.Scan();
            }
            scanner.Measure(now);

            int layer = FirstLayer(camera.cullingMask);
            List<FieldOfFire> fields = scanner.Fields;
            for (int i = 0; i < fields.Count; i++)
            {
                FieldOfFire field = fields[i];
                // A destroyed turret is dropped at the next scan; until then, don't draw it.
                if (field.HasShape && field.Turret != null && Plugin.KindShown[(int)field.Kind].Value)
                {
                    MaterialPropertyBlock tint = Plugin.ColorByType.Value ? kindTint[(int)field.Kind] : singleTint;
                    Graphics.DrawMesh(field.Mesh, Matrix4x4.Translate(CoverageMesh.Center(field)), material, layer, camera, 0, tint, false, false);
                }
            }

            FindHovered(camera);
            if (hovered.Count > 0)
            {
                CoverageMesh.BeginSweeps();
                foreach (FieldOfFire field in hovered)
                {
                    CoverageMesh.AddSweep(field, ColorOf(field.Kind), now, camera);
                }
                CoverageMesh.EndSweeps(sweepMesh);
                Graphics.DrawMesh(sweepMesh, Matrix4x4.identity, material, layer, camera, 0, sweepTint, false, false);
            }
        }

        /// <summary>The fields of fire of the turret nearest the mouse, if it is close enough.</summary>
        private void FindHovered(Camera camera)
        {
            hovered.Clear();
            // The sector map camera would only be off-screen if it drew into a texture.
            if (camera.targetTexture != null)
            {
                return;
            }
            Vector2 mouse = Input.mousePosition;
            float best = Mathf.Max(1f, Plugin.HoverPixels.Value);
            FieldOfFire nearest = null;
            List<FieldOfFire> fields = scanner.Fields;
            for (int i = 0; i < fields.Count; i++)
            {
                FieldOfFire field = fields[i];
                if (!field.HasShape || field.Turret == null || !Plugin.KindShown[(int)field.Kind].Value)
                {
                    continue;
                }
                Vector3 screen = camera.WorldToScreenPoint(field.Turret.transform.position);
                if (screen.z <= 0f)
                {
                    continue;
                }
                float distance = Vector2.Distance(mouse, screen);
                if (distance < best)
                {
                    best = distance;
                    nearest = field;
                }
            }
            if (nearest == null)
            {
                return;
            }
            Vector3 mount = nearest.Turret.transform.position;
            for (int i = 0; i < fields.Count; i++)
            {
                FieldOfFire field = fields[i];
                if (field.HasShape && field.Turret != null && Plugin.KindShown[(int)field.Kind].Value
                    && (field.Turret.transform.position - mount).sqrMagnitude <= SameMountDistance * SameMountDistance)
                {
                    hovered.Add(field);
                }
            }
        }

        private static int FirstLayer(int cullingMask)
        {
            for (int layer = 0; layer < 32; layer++)
            {
                if ((cullingMask & (1 << layer)) != 0)
                {
                    return layer;
                }
            }
            return 0;
        }
    }
}
