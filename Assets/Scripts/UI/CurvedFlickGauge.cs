using System;
using UnityEngine;
using UnityEngine.UI;
using TeksMaster.Visuals;

namespace TeksMaster.UI
{
    public class CurvedFlickGauge : MonoBehaviour
    {
        private RawImage _arcImage;
        private RawImage _arrowImage;
        private RectTransform _needlePivot;
        private Button _flickButton;
        private Text _promptLabel;

        private Texture2D _arcTex;
        private float _lastSweetSpot = -1f;
        private float _maxAngleDeg = 50f;

        public event Action OnFlickClicked;

        public static CurvedFlickGauge Create(Transform parent, Font font)
        {
            var root = new GameObject("CurvedFlickGauge", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var rt = root.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(540f, 235f);

            var gauge = root.AddComponent<CurvedFlickGauge>();
            gauge.BuildVisuals(font);
            return gauge;
        }

        private void BuildVisuals(Font font)
        {
            // Curved yellow double-headed arrow above the gauge
            var arrowGo = new GameObject("CurvedArrow", typeof(RectTransform), typeof(RawImage));
            arrowGo.transform.SetParent(transform, false);
            var arrowRt = arrowGo.GetComponent<RectTransform>();
            arrowRt.anchorMin = new Vector2(0.5f, 0.5f);
            arrowRt.anchorMax = new Vector2(0.5f, 0.5f);
            arrowRt.anchoredPosition = new Vector2(0f, 64f);
            arrowRt.sizeDelta = new Vector2(360f, 90f);
            _arrowImage = arrowGo.GetComponent<RawImage>();
            _arrowImage.texture = GenerateYellowArrowTexture();
            _arrowImage.raycastTarget = false;

            // Main curved timing arc
            var arcGo = new GameObject("ArcMeter", typeof(RectTransform), typeof(RawImage));
            arcGo.transform.SetParent(transform, false);
            var arcRt = arcGo.GetComponent<RectTransform>();
            arcRt.anchorMin = new Vector2(0.5f, 0.5f);
            arcRt.anchorMax = new Vector2(0.5f, 0.5f);
            arcRt.anchoredPosition = new Vector2(0f, -6f);
            arcRt.sizeDelta = new Vector2(520f, 170f);
            _arcImage = arcGo.GetComponent<RawImage>();
            _arcImage.raycastTarget = true;

            // Clicking the gauge itself also triggers flick
            var arcBtn = arcGo.AddComponent<Button>();
            arcBtn.transition = Selectable.Transition.None;
            arcBtn.onClick.AddListener(() => OnFlickClicked?.Invoke());

            // Top & bottom green sweet-spot marker triangles
            CreateTriangleMarker("SweetSpotTopMarker", arcRt, new Vector2(0f, 58f), pointingDown: true);
            CreateTriangleMarker("SweetSpotBotMarker", arcRt, new Vector2(0f, -6f), pointingDown: false);

            // Pivoting needle centered at geometric arc origin (offset downward by 268px in UI space)
            var pivotGo = new GameObject("NeedlePivot", typeof(RectTransform));
            pivotGo.transform.SetParent(arcRt, false);
            _needlePivot = pivotGo.GetComponent<RectTransform>();
            _needlePivot.anchorMin = new Vector2(0.5f, 0.5f);
            _needlePivot.anchorMax = new Vector2(0.5f, 0.5f);
            _needlePivot.anchoredPosition = new Vector2(0f, -268f);
            _needlePivot.sizeDelta = new Vector2(14f, 680f);

            var needleBar = new GameObject("NeedleBar", typeof(RectTransform), typeof(Image));
            needleBar.transform.SetParent(_needlePivot, false);
            var barRt = needleBar.GetComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0.5f, 0.5f);
            barRt.anchorMax = new Vector2(0.5f, 0.5f);
            barRt.anchoredPosition = new Vector2(0f, 294f);
            barRt.sizeDelta = new Vector2(8f, 64f);
            var barImg = needleBar.GetComponent<Image>();
            barImg.color = Color.white;
            barImg.raycastTarget = false;
            var outline = needleBar.AddComponent<Outline>();
            outline.effectColor = new Color(0.05f, 0.08f, 0.14f, 0.95f);
            outline.effectDistance = new Vector2(2f, 2f);

            // "TAP [SPACE] TO FLICK!" button pill below
            var btnGo = new GameObject("TapSpaceButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(transform, false);
            var btnRt = btnGo.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0.5f, 0.5f);
            btnRt.anchorMax = new Vector2(0.5f, 0.5f);
            btnRt.anchoredPosition = new Vector2(0f, -86f);
            btnRt.sizeDelta = new Vector2(290f, 44f);
            var btnImg = btnGo.GetComponent<Image>();
            btnImg.sprite = CardArtGenerator.GetRoundedPanelSprite();
            btnImg.type = Image.Type.Sliced;
            btnImg.color = new Color(0.09f, 0.13f, 0.22f, 0.94f);
            var btnOutline = btnGo.AddComponent<Outline>();
            btnOutline.effectColor = new Color(0.96f, 0.78f, 0.24f, 0.95f);
            btnOutline.effectDistance = new Vector2(2f, 2f);

            _flickButton = btnGo.GetComponent<Button>();
            _flickButton.onClick.AddListener(() => OnFlickClicked?.Invoke());

            var textGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(btnGo.transform, false);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            _promptLabel = textGo.GetComponent<Text>();
            _promptLabel.font = font;
            _promptLabel.fontSize = 20;
            _promptLabel.fontStyle = FontStyle.Bold;
            _promptLabel.alignment = TextAnchor.MiddleCenter;
            _promptLabel.color = Color.white;
            _promptLabel.text = "TAP [SPACE] TO FLICK!";

            SetSweetSpotHalfWidth(0.16f);
            SetNeedleValue(0f);
        }

        private void CreateTriangleMarker(string name, Transform parent, Vector2 pos, bool pointingDown)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(22f, 16f);
            var img = go.GetComponent<Image>();
            img.sprite = CardArtGenerator.GetTriangleSprite(pointingDown);
            img.color = new Color(0.24f, 0.92f, 0.42f);
            img.raycastTarget = false;
        }

        public void SetSweetSpotHalfWidth(float sweetSpotHalfWidth)
        {
            if (Mathf.Abs(sweetSpotHalfWidth - _lastSweetSpot) < 0.004f && _arcTex != null) return;
            _lastSweetSpot = sweetSpotHalfWidth;
            RebuildArcTexture(sweetSpotHalfWidth);
        }

        public void SetNeedleValue(float needleValue)
        {
            float clamped = Mathf.Clamp(needleValue, -1f, 1f);
            if (_needlePivot != null)
            {
                _needlePivot.localRotation = Quaternion.Euler(0f, 0f, -clamped * _maxAngleDeg);
            }
        }

        public void SetInteractive(bool interactive, string customLabel = null)
        {
            if (_flickButton != null) _flickButton.interactable = interactive;
            if (_promptLabel != null)
            {
                _promptLabel.text = string.IsNullOrEmpty(customLabel) ? "TAP [SPACE] TO FLICK!" : customLabel;
            }
        }

        private void RebuildArcTexture(float sweetSpotHalf)
        {
            const int w = 520;
            const int h = 170;
            if (_arcTex == null)
            {
                _arcTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                _arcTex.filterMode = FilterMode.Bilinear;
            }

            Vector2 center = new Vector2(w * 0.5f, -183f);
            float innerR = 268f;
            float outerR = 320f;
            float borderThickness = 4.5f;

            Color borderCol = new Color(0.95f, 0.96f, 0.98f, 1f);
            Color darkBorder = new Color(0.08f, 0.10f, 0.15f, 1f);
            Color slateCol = new Color(0.27f, 0.30f, 0.36f, 1f);
            Color greenCol = new Color(0.18f, 0.85f, 0.34f, 1f);
            Color orangeCol = new Color(0.96f, 0.64f, 0.14f, 1f);
            Color redCol = new Color(0.90f, 0.22f, 0.16f, 1f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Vector2 d = new Vector2(x, y) - center;
                    float r = d.magnitude;
                    float angleDeg = Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg;
                    float absAng = Mathf.Abs(angleDeg);

                    if (r < innerR - borderThickness || r > outerR + borderThickness || absAng > _maxAngleDeg + 1.5f)
                    {
                        _arcTex.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    bool isOuterFrame = r < innerR || r > outerR || absAng > _maxAngleDeg;
                    if (isOuterFrame)
                    {
                        bool isInnerWhite = r >= innerR - 2.5f && r <= outerR + 2.5f && absAng <= _maxAngleDeg + 0.8f;
                        _arcTex.SetPixel(x, y, isInnerWhite ? borderCol : darkBorder);
                        continue;
                    }

                    float norm = angleDeg / _maxAngleDeg; // -1 .. +1
                    float absNorm = Mathf.Abs(norm);

                    Color segCol;
                    if (absNorm <= sweetSpotHalf)
                    {
                        segCol = greenCol;
                    }
                    else if (absNorm <= 0.44f)
                    {
                        segCol = orangeCol;
                    }
                    else if (norm > 0.44f)
                    {
                        segCol = redCol;
                    }
                    else
                    {
                        segCol = slateCol;
                    }

                    // Tick dividers every ~25% of arc
                    if (Mathf.Abs(absNorm - sweetSpotHalf) < 0.015f ||
                        Mathf.Abs(absNorm - 0.44f) < 0.015f ||
                        Mathf.Abs(absNorm - 0.72f) < 0.014f)
                    {
                        segCol = darkBorder;
                    }

                    // Top highlight sheen
                    float radialT = (r - innerR) / (outerR - innerR);
                    if (radialT > 0.72f) segCol = Color.Lerp(segCol, Color.white, 0.18f);

                    _arcTex.SetPixel(x, y, segCol);
                }
            }

            _arcTex.Apply();
            if (_arcImage != null) _arcImage.texture = _arcTex;
        }

        private Texture2D GenerateYellowArrowTexture()
        {
            const int w = 360;
            const int h = 90;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Vector2 center = new Vector2(w * 0.5f, -155f);
            float innerR = 205f;
            float outerR = 224f;
            float bodyHalfDeg = 32f;
            float tipHalfDeg = 42f;

            Color yellow = new Color(1.0f, 0.82f, 0.18f, 1f);
            Color outline = new Color(0.14f, 0.10f, 0.04f, 1f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Vector2 d = new Vector2(x, y) - center;
                    float r = d.magnitude;
                    float angleDeg = Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg;
                    float absAng = Mathf.Abs(angleDeg);

                    bool inShaft = absAng <= bodyHalfDeg && r >= innerR && r <= outerR;
                    bool inArrowHead = false;
                    if (absAng > bodyHalfDeg && absAng <= tipHalfDeg)
                    {
                        float t = (absAng - bodyHalfDeg) / (tipHalfDeg - bodyHalfDeg);
                        float midR = (innerR + outerR) * 0.5f;
                        float halfSpan = Mathf.Lerp(17f, 0f, t);
                        inArrowHead = Mathf.Abs(r - midR) <= halfSpan;
                    }

                    if (inShaft || inArrowHead)
                    {
                        bool edge = (inShaft && (r < innerR + 2.5f || r > outerR - 2.5f)) ||
                                    (inArrowHead && Mathf.Abs(r - (innerR + outerR) * 0.5f) > 12f * (1f - (absAng - bodyHalfDeg) / (tipHalfDeg - bodyHalfDeg)));
                        tex.SetPixel(x, y, edge ? outline : yellow);
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }

            tex.Apply();
            return tex;
        }
    }
}
