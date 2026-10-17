using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FriendlyTurretCoverage
{
    /// <summary>
    /// The on-screen toggles, just above the scanner: "Turret coverage" switches the overlay on,
    /// and while it is on, a toggle for each damage type and one for "Colour by type" appear above
    /// it. They show in space and on the sector map.
    ///
    /// The toggles are clones of the sector map's "Show grid" toggle, so they look like the game's
    /// own. Their state is kept in the mod's config, so they are remembered between games.
    /// </summary>
    internal sealed class CoveragePanel : MonoBehaviour
    {
        private const string RootName = "FriendlyTurretCoverageToggles";
        private const string ScannerImagePath = "Scanner/Images/MaskedImage/Image";
        private const float Width = 190f;
        private const float RowGap = 2f;
        private const float Indent = 16f;
        private const float Margin = 6f;
        private const float LayoutSeconds = 1f;

        private readonly List<Row> rows = new List<Row>();
        private Row master;
        private Row colorByType;
        private readonly Row[] kinds = new Row[DamageKinds.Count];
        private RectTransform canvas;
        private RectTransform scannerImage;
        private float rowHeight;
        private float nextLayout;
        private bool blockedControls;

        private sealed class Row
        {
            public Toggle Toggle;
            public Text Label;
            public Color LabelColor;
            public ConfigEntry<bool> Setting;
            public int TextCode;
            public bool Sub;
        }

        public static CoveragePanel Create(CoverageOverlay overlay)
        {
            GameObject canvasGO = GameObject.FindGameObjectWithTag("MainCanvas");
            Toggle template = FindTemplate();
            if (canvasGO == null || template == null)
            {
                Plugin.Log.LogError("The sector map has changed; can't add the turret coverage toggles."
                    + (Plugin.ToggleKey.Value.MainKey != KeyCode.None ? "" : " Set ToggleKey in the config to switch the overlay with a key."));
                return null;
            }
            Transform existing = canvasGO.transform.Find(RootName);
            if (existing != null)
            {
                Destroy(existing.gameObject);
            }

            var go = new GameObject(RootName, typeof(RectTransform));
            go.layer = canvasGO.layer;
            go.transform.SetParent(canvasGO.transform, false);
            CoveragePanel panel = go.AddComponent<CoveragePanel>();
            panel.Build((RectTransform)canvasGO.transform, template);
            return panel;
        }

        /// <summary>The sector map's "Show grid" toggle. It may be inactive, so look through everything loaded.</summary>
        private static Toggle FindTemplate()
        {
            foreach (ScannerUIControl control in Resources.FindObjectsOfTypeAll<ScannerUIControl>())
            {
                if (control.gameObject.scene.IsValid() && control.GridToggle != null)
                {
                    return control.GridToggle;
                }
            }
            return null;
        }

        private void Build(RectTransform canvasRT, Toggle template)
        {
            canvas = canvasRT;
            Transform image = canvas.Find(ScannerImagePath);
            scannerImage = image != null ? (RectTransform)image : null;

            var rt = (RectTransform)transform;
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = Vector2.zero;

            rowHeight = ((RectTransform)template.transform).rect.height;
            if (rowHeight < 12f)
            {
                rowHeight = 20f;
            }

            master = AddRow(template, Plugin.ShowCoverage, Loc.TurretCoverage, false);
            colorByType = AddRow(template, Plugin.ColorByType, Loc.ColorByType, true);
            // Listed top to bottom in the enum's order, so added bottom-up in reverse.
            for (int i = DamageKinds.Count - 1; i >= 0; i--)
            {
                kinds[i] = AddRow(template, Plugin.KindShown[i], Loc.Beam + i, true);
            }
            Refresh();
        }

        private Row AddRow(Toggle template, ConfigEntry<bool> setting, int textCode, bool sub)
        {
            GameObject go = Instantiate(template.gameObject, transform, false);
            go.name = "Toggle" + setting.Definition.Key;
            go.SetActive(true);
            foreach (LangText lang in go.GetComponentsInChildren<LangText>(true))
            {
                // It would put the template's text back on language changes.
                DestroyImmediate(lang);
            }
            foreach (ScannerUIControl control in go.GetComponentsInChildren<ScannerUIControl>(true))
            {
                DestroyImmediate(control);
            }
            ReplaceHoverTrigger(go);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = Vector2.zero;
            rt.sizeDelta = new Vector2(Width - (sub ? Indent : 0f), rowHeight);

            var row = new Row { Setting = setting, TextCode = textCode, Sub = sub };
            row.Toggle = go.GetComponent<Toggle>();
            // Drop the template's "show grid" handler, which is wired up in the scene.
            row.Toggle.onValueChanged = new Toggle.ToggleEvent();
            row.Toggle.onValueChanged.AddListener(on => setting.Value = on);
            row.Label = go.GetComponentInChildren<Text>(true);
            if (row.Label != null)
            {
                row.Label.horizontalOverflow = HorizontalWrapMode.Overflow;
                row.LabelColor = row.Label.color;
            }
            rows.Add(row);
            return row;
        }

        /// <summary>
        /// Stops a click on the toggles from also firing the player's weapons, the way the game's
        /// own HUD panels do: controls are blocked while the mouse is over them.
        /// </summary>
        private void ReplaceHoverTrigger(GameObject go)
        {
            EventTrigger old = go.GetComponent<EventTrigger>();
            if (old != null)
            {
                DestroyImmediate(old);
            }
            EventTrigger trigger = go.AddComponent<EventTrigger>();
            AddTrigger(trigger, EventTriggerType.PointerEnter, BlockControls);
            AddTrigger(trigger, EventTriggerType.PointerExit, ReleaseControls);
        }

        private static void AddTrigger(EventTrigger trigger, EventTriggerType type, UnityAction action)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(_ => action());
            trigger.triggers.Add(entry);
        }

        private void BlockControls()
        {
            // The sector map already blocks the controls, and releasing them afterwards would let
            // the ship fly while the map is open.
            if (PlayerControl.inst != null && MinimapControl.instance != null && !MinimapControl.instance.isSectorMap)
            {
                PlayerControl.inst.BlockControls(allowSteering: true);
                blockedControls = true;
            }
        }

        private void ReleaseControls()
        {
            if (blockedControls)
            {
                blockedControls = false;
                if (PlayerControl.inst != null && MinimapControl.instance != null && !MinimapControl.instance.isSectorMap)
                {
                    PlayerControl.inst.ReleaseControls(unpause: false);
                }
            }
        }

        public void Show(bool visible)
        {
            if (gameObject.activeSelf != visible)
            {
                if (!visible)
                {
                    ReleaseControls();
                }
                gameObject.SetActive(visible);
                if (visible)
                {
                    Refresh();
                }
            }
        }

        private void OnDisable()
        {
            ReleaseControls();
        }

        /// <summary>Brings the toggles in line with the config and the game's language.</summary>
        public void Refresh()
        {
            bool expanded = Plugin.ShowCoverage.Value;
            float y = 0f;
            foreach (Row row in rows)
            {
                bool shown = !row.Sub || expanded;
                row.Toggle.gameObject.SetActive(shown);
                row.Toggle.SetIsOnWithoutNotify(row.Setting.Value);
                if (row.Label != null)
                {
                    row.Label.text = Loc.Get(row.TextCode);
                    row.Label.color = row.LabelColor;
                }
                if (shown)
                {
                    ((RectTransform)row.Toggle.transform).anchoredPosition = new Vector2(row.Sub ? Indent : 0f, y);
                    y += rowHeight + RowGap;
                }
            }
            // With colour by type on, each damage type's label is in its colour, as a legend.
            if (Plugin.ColorByType.Value)
            {
                for (int i = 0; i < DamageKinds.Count; i++)
                {
                    if (kinds[i].Label != null)
                    {
                        kinds[i].Label.color = CoverageOverlay.ColorOf((DamageKind)i);
                    }
                }
            }
            ((RectTransform)transform).sizeDelta = new Vector2(Width, Mathf.Max(0f, y - RowGap));
            Layout();
        }

        private void Update()
        {
            // The game language or the screen size can change while the toggles are up.
            if (Time.unscaledTime >= nextLayout)
            {
                nextLayout = Time.unscaledTime + LayoutSeconds;
                Loc.Validate();
                if (master.Label != null && master.Label.text != Loc.Get(master.TextCode))
                {
                    Refresh();
                }
                Layout();
            }
        }

        /// <summary>Puts the toggles just above the scanner's top left corner, plus the configured offset.</summary>
        private void Layout()
        {
            Vector2 position = new Vector2(Margin, 300f);
            if (scannerImage != null)
            {
                // The scanner image keeps its layout while the sector map hides it.
                var corners = new Vector3[4];
                scannerImage.GetWorldCorners(corners);
                Vector3 topLeft = canvas.InverseTransformPoint(corners[1]);
                // Canvas space has its origin in the middle; the toggles are anchored bottom left.
                position = new Vector2(topLeft.x + canvas.rect.width * canvas.pivot.x,
                    topLeft.y + canvas.rect.height * canvas.pivot.y + Margin);
            }
            position += new Vector2(Plugin.PanelOffsetX.Value, Plugin.PanelOffsetY.Value);
            ((RectTransform)transform).anchoredPosition = position;
            transform.SetAsLastSibling();
        }
    }
}
