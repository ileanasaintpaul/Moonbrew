using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Moonbrew.Rendering
{
    // While the interface is pixelated, its texts are made invisible there and drawn again on a second
    // panel at screen resolution, at the same place and in the same style, so they stay smooth and sharp.
    [RequireComponent(typeof(UIDocument))]
    public class SharpTextOverlay : MonoBehaviour
    {
        [SerializeField] private PixelRenderer pixelRenderer;

        private readonly List<TextElement> _texts = new List<TextElement>();
        private readonly Dictionary<TextElement, (string Original, string Hidden)> _tracked = new Dictionary<TextElement, (string, string)>();
        private readonly List<TextElement> _gone = new List<TextElement>();
        private readonly List<VisualElement> _clips = new List<VisualElement>();
        private UIDocument _document;
        private PanelSettings _overlayPanel;
        private GameObject _overlayObject;
        private VisualElement _layer;

        // The sharp copy of a text, or null when it is not drawn. For tests.
        public Label SharpCopyOf(TextElement text)
        {
            var original = _tracked.TryGetValue(text, out var entry) ? entry.Original : null;
            foreach (var clip in _clips)
            {
                var label = (Label)clip[0];
                if (clip.resolvedStyle.display != DisplayStyle.None && label.userData == text)
                {
                    return label.text == original ? label : null;
                }
            }

            return null;
        }

        // For objects built from code: call on an inactive GameObject, before it is activated.
        public void Init(PixelRenderer renderer)
        {
            pixelRenderer = renderer;
        }

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
            if (pixelRenderer == null)
            {
                Debug.LogError($"{nameof(SharpTextOverlay)} on {name} needs the {nameof(PixelRenderer)}.", this);
                enabled = false;
            }
        }

        private void OnDisable()
        {
            Restore();
        }

        private void OnDestroy()
        {
            if (_overlayObject != null)
            {
                Destroy(_overlayObject);
            }

            if (_overlayPanel != null)
            {
                Destroy(_overlayPanel);
            }
        }

        // Texts move, change and appear every frame (counts, clock, menus), so the copy follows them each frame.
        private void LateUpdate()
        {
            if (!pixelRenderer.IsUiPixelated || _document.rootVisualElement == null)
            {
                Restore();
                return;
            }

            EnsureOverlay();
            Sync();
        }

        private void EnsureOverlay()
        {
            if (_overlayObject != null)
            {
                return;
            }

            _overlayPanel = Instantiate(_document.panelSettings);
            _overlayPanel.name = "SharpTextPanel";
            _overlayPanel.targetTexture = null;
            _overlayPanel.clearColor = false;
            _overlayPanel.scaleMode = PanelScaleMode.ConstantPixelSize;
            _overlayPanel.sortingOrder = _document.panelSettings.sortingOrder + 1;

            // Not a child of this object: a UIDocument under another one joins its visual tree,
            // and the copies would then be copied again.
            _overlayObject = new GameObject("SharpTextOverlay");
            _overlayObject.SetActive(false);
            var overlay = _overlayObject.AddComponent<UIDocument>();
            overlay.panelSettings = _overlayPanel;
            _overlayObject.SetActive(true);

            var root = overlay.rootVisualElement;
            root.pickingMode = PickingMode.Ignore;
            _layer = new VisualElement { pickingMode = PickingMode.Ignore };
            _layer.style.position = Position.Absolute;
            root.Add(_layer);
        }

        private void Sync()
        {
            // One overlay pixel per image pixel, and the overlay starts where the image does.
            var scale = Mathf.Max(1, pixelRenderer.Scale);
            var viewport = pixelRenderer.UiViewport;
            _overlayPanel.scale = scale;
            _layer.style.display = DisplayStyle.Flex;
            _layer.style.left = (float)viewport.x / scale;
            _layer.style.top = (float)(Screen.height - viewport.yMax) / scale;

            _texts.Clear();
            _document.rootVisualElement.Query<TextElement>().ToList(_texts);

            var shown = 0;
            foreach (var text in _texts)
            {
                var original = Track(text);
                if (!IsVisible(text, out var opacity, out var clipRect))
                {
                    continue;
                }

                var content = ContentRect(text);
                if (content.width <= 0f || content.height <= 0f)
                {
                    continue;
                }

                Draw(Clip(shown++), text, original, opacity, clipRect, content);
            }

            for (var i = shown; i < _clips.Count; i++)
            {
                _clips[i].style.display = DisplayStyle.None;
            }

            Forget();
        }

        // Hides the text in the pixelated panel and returns what it really says.
        private string Track(TextElement text)
        {
            var current = text.text;
            if (_tracked.TryGetValue(text, out var entry) && current == entry.Hidden)
            {
                return entry.Original;
            }

            var hidden = SharpText.Hide(current);
            _tracked[text] = (current, hidden);
            text.text = hidden;
            return current;
        }

        private static bool IsVisible(TextElement text, out float opacity, out Rect clipRect)
        {
            opacity = 1f;
            clipRect = new Rect(float.MinValue / 2f, float.MinValue / 2f, float.MaxValue, float.MaxValue);
            for (VisualElement element = text; element != null; element = element.parent)
            {
                var style = element.resolvedStyle;
                if (style.display == DisplayStyle.None || style.visibility == Visibility.Hidden)
                {
                    return false;
                }

                opacity *= style.opacity;

                // Lists that scroll are the only place the interface clips its content.
                if (element is ScrollView scroll)
                {
                    clipRect = Intersect(clipRect, scroll.contentViewport.worldBound);
                }
            }

            return opacity > 0f && clipRect.width > 0f && clipRect.height > 0f;
        }

        private static Rect ContentRect(TextElement text)
        {
            var bounds = text.worldBound;
            if (float.IsNaN(bounds.x) || float.IsNaN(bounds.width))
            {
                return Rect.zero;
            }

            var style = text.resolvedStyle;
            var left = style.borderLeftWidth + style.paddingLeft;
            var top = style.borderTopWidth + style.paddingTop;
            var right = style.borderRightWidth + style.paddingRight;
            var bottom = style.borderBottomWidth + style.paddingBottom;
            return new Rect(bounds.x + left, bounds.y + top, bounds.width - left - right, bounds.height - top - bottom);
        }

        private static Rect Intersect(Rect a, Rect b)
        {
            var xMin = Mathf.Max(a.xMin, b.xMin);
            var yMin = Mathf.Max(a.yMin, b.yMin);
            var xMax = Mathf.Min(a.xMax, b.xMax);
            var yMax = Mathf.Min(a.yMax, b.yMax);
            return Rect.MinMaxRect(xMin, yMin, Mathf.Max(xMin, xMax), Mathf.Max(yMin, yMax));
        }

        // A clipping box holding one sharp label; boxes are reused from frame to frame.
        private VisualElement Clip(int index)
        {
            if (index < _clips.Count)
            {
                return _clips[index];
            }

            var clip = new VisualElement { pickingMode = PickingMode.Ignore };
            clip.style.position = Position.Absolute;
            var label = new Label { pickingMode = PickingMode.Ignore };
            label.style.position = Position.Absolute;
            label.style.marginLeft = label.style.marginRight = label.style.marginTop = label.style.marginBottom = 0f;
            label.style.paddingLeft = label.style.paddingRight = label.style.paddingTop = label.style.paddingBottom = 0f;
            clip.Add(label);
            _layer.Add(clip);
            _clips.Add(clip);
            return clip;
        }

        private static void Draw(VisualElement clip, TextElement text, string original, float opacity, Rect clipRect, Rect content)
        {
            // Only texts in a scrolling list are clipped; the others may spill past their box, as glyphs often do.
            var clipped = clipRect.width < float.MaxValue / 2f;
            var box = clipped ? Intersect(clipRect, Expand(content)) : content;
            clip.style.overflow = clipped ? Overflow.Hidden : Overflow.Visible;
            clip.style.display = DisplayStyle.Flex;
            clip.style.left = box.x;
            clip.style.top = box.y;
            clip.style.width = box.width;
            clip.style.height = box.height;

            var label = (Label)clip[0];
            label.userData = text;
            if (label.text != original)
            {
                label.text = original;
            }

            label.style.left = content.x - box.x;
            label.style.top = content.y - box.y;
            label.style.width = content.width;
            label.style.height = content.height;

            var from = text.resolvedStyle;
            var to = label.style;
            to.opacity = opacity;
            to.unityFontDefinition = from.unityFontDefinition;
            to.fontSize = from.fontSize;
            to.color = from.color;
            to.unityTextAlign = from.unityTextAlign;
            to.unityFontStyleAndWeight = from.unityFontStyleAndWeight;
            to.whiteSpace = from.whiteSpace;
            to.letterSpacing = from.letterSpacing;
            to.wordSpacing = from.wordSpacing;
            to.unityTextOutlineWidth = from.unityTextOutlineWidth;
            to.unityTextOutlineColor = from.unityTextOutlineColor;
        }

        // Outlines reach a little past the text box.
        private static Rect Expand(Rect rect)
        {
            const float margin = 2f;
            return new Rect(rect.x - margin, rect.y - margin, rect.width + margin * 2f, rect.height + margin * 2f);
        }

        // Drops texts that left the interface (menu rows rebuilt), so they are not kept alive.
        private void Forget()
        {
            if (_tracked.Count <= _texts.Count)
            {
                return;
            }

            _gone.Clear();
            foreach (var text in _tracked.Keys)
            {
                if (text.panel == null)
                {
                    _gone.Add(text);
                }
            }

            foreach (var text in _gone)
            {
                _tracked.Remove(text);
            }
        }

        // Back to the texts drawn by the interface itself, as when the image is not pixelated.
        private void Restore()
        {
            foreach (var pair in _tracked)
            {
                if (pair.Key.text == pair.Value.Hidden)
                {
                    pair.Key.text = pair.Value.Original;
                }
            }

            _tracked.Clear();
            if (_layer != null)
            {
                _layer.style.display = DisplayStyle.None;
            }
        }
    }
}
