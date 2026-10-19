using UnityEngine;
using UnityEngine.UI;

// ReSharper disable once CheckNamespace
namespace Game.Dungeon
{
    /// <summary>
    /// ダンジョン崩壊の警告を「画面の縁を囲む円環グロー」で表示する UI ビュー。
    /// 円環は多数のセグメント画像で構成し、崩壊方向の角度からの差分をガウス関数で重み付けすることで、
    /// 見た目が連続したひとつの光の帯になるようにしている（段階的な方向表示ではない）。
    /// ScreenEffectManager と同じシングルトンパターンを踏襲し、Inspector 未設定でも動的生成で動作する。
    /// </summary>
    public class DungeonCollapseWarningUIView : MonoBehaviour
    {
        /// <summary>
        /// セグメント同士の隙間を埋めるための長さ倍率。
        /// 大きくしすぎると重なり部分だけが明るくなり縞模様に見えるため、隙間が消える最小限に留めている。
        /// </summary>
        private const float SegmentOverlapRatio = 1.15f;

        /// <summary>
        /// ガウス減衰の裾を切り捨てる閾値。
        /// これを下回るほど弱い光は 0 として扱い、円環全体がうっすら光ってしまうのを防ぐ。
        /// </summary>
        private const float SegmentWeightCutoff = 0.08f;

        /// <summary>グロー用グラデーションテクスチャの解像度。</summary>
        private const int GlowTextureHeight = 64;

        /// <summary>セグメント数の許容範囲（極端な値でレイアウトが破綻しないようにする）。</summary>
        private const int MinSegmentCount = 8;
        private const int MaxSegmentCount = 128;

        private static DungeonCollapseWarningUIView instance;
        private static bool isDestroying;

        public static DungeonCollapseWarningUIView Instance
        {
            get
            {
                if (isDestroying || !Application.isPlaying)
                {
                    return null;
                }

                if (instance == null)
                {
                    var go = new GameObject("DungeonCollapseWarningUIView");
                    instance = go.AddComponent<DungeonCollapseWarningUIView>();
                    DontDestroyOnLoad(go);
                }

                return instance;
            }
        }

        [SerializeField] private Canvas warningCanvas;

        private RectTransform canvasRectTransform;
        private RectTransform ringRoot;
        private Image[] segmentImages;
        private float[] segmentAngles;
        private Sprite glowSprite;

        private DungeonCollapseParameter activeParameter;
        private Vector2 builtCanvasSize;
        private int builtSegmentCount;
        private float builtThickness;
        private float pulsePhase;
        private bool isShowing;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }

            InitializeUI();
        }

        private void InitializeUI()
        {
            if (warningCanvas == null)
            {
                var canvasGo = new GameObject("DungeonCollapseWarningCanvas");
                canvasGo.transform.SetParent(transform);

                warningCanvas = canvasGo.AddComponent<Canvas>();
                warningCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                warningCanvas.sortingOrder = 500;

                var canvasGroup = canvasGo.AddComponent<CanvasGroup>();
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            canvasRectTransform = warningCanvas.GetComponent<RectTransform>();

            if (ringRoot == null)
            {
                var ringGo = new GameObject("WarningRing");
                ringGo.transform.SetParent(warningCanvas.transform, false);

                ringRoot = ringGo.AddComponent<RectTransform>();
                ringRoot.anchorMin = Vector2.zero;
                ringRoot.anchorMax = Vector2.one;
                ringRoot.offsetMin = Vector2.zero;
                ringRoot.offsetMax = Vector2.zero;
            }

            glowSprite = CreateGlowSprite();
            ringRoot.gameObject.SetActive(false);
        }

        // ─── 公開API ────────────────────────────────────────────────────

        /// <summary>
        /// 警告表示を開始する。以降 <see cref="UpdateWarning"/> を毎フレーム呼ぶこと。
        /// </summary>
        public void ShowWarning(DungeonCollapseParameter parameter)
        {
            if (parameter == null)
            {
                return;
            }

            activeParameter = parameter;
            pulsePhase = 0f;
            isShowing = true;

            EnsureRingLayout();
            ringRoot.gameObject.SetActive(true);
        }

        /// <summary>
        /// 警告の方向・距離に応じて円環グローを更新する。プレイヤーが移動するため毎フレーム呼ばれる想定。
        /// </summary>
        public void UpdateWarning(DungeonCollapseWarningEntity warning)
        {
            if (!isShowing || activeParameter == null)
            {
                return;
            }

            EnsureRingLayout();

            var pulseSpeed = warning.IsPlayerInsideTargetRoom
                ? activeParameter.WarningInsidePulseSpeed
                : Mathf.Lerp(activeParameter.WarningNearPulseSpeed, activeParameter.WarningFarPulseSpeed, warning.NormalizedDistance);

            // 速度が変化しても明滅が飛ばないよう、位相を積算する
            pulsePhase += Time.deltaTime * pulseSpeed;

            var pulse = (Mathf.Sin(pulsePhase) + 1f) * 0.5f;
            var pulseAlpha = Mathf.Lerp(activeParameter.WarningMinAlpha, activeParameter.WarningMaxAlpha, pulse);

            var glowColor = warning.IsPlayerInsideTargetRoom
                ? activeParameter.WarningNearColor
                : EvaluateDistanceColor(warning.NormalizedDistance);

            var spreadDegrees = Mathf.Lerp(
                activeParameter.WarningNearSpreadDegrees,
                activeParameter.WarningFarSpreadDegrees,
                warning.NormalizedDistance);

            ApplySegmentColors(warning, glowColor, pulseAlpha, spreadDegrees);
        }

        /// <summary>警告表示を終了する。</summary>
        public void HideWarning()
        {
            isShowing = false;
            activeParameter = null;

            if (ringRoot != null)
            {
                ringRoot.gameObject.SetActive(false);
            }
        }

        // ─── 内部処理 ───────────────────────────────────────────────────

        /// <summary>
        /// 各セグメントの色と不透明度を更新する。
        /// 崩壊方向からの角度差をガウス関数で減衰させることで、連続した光の帯として見えるようにしている。
        /// </summary>
        private void ApplySegmentColors(DungeonCollapseWarningEntity warning, Color glowColor, float pulseAlpha, float spreadDegrees)
        {
            // 標準偏差が 0 になると全セグメントが消えるため下限を設ける
            var sigma = Mathf.Max(spreadDegrees, 1f);
            var doubleSigmaSquared = 2f * sigma * sigma;

            for (var i = 0; i < segmentImages.Length; i++)
            {
                var segmentImage = segmentImages[i];
                if (segmentImage == null)
                {
                    continue;
                }

                float weight;
                if (warning.IsPlayerInsideTargetRoom)
                {
                    // 崩壊部屋の中にいる場合は方向を示さず全周を最大輝度で点滅させる
                    weight = 1f;
                }
                else
                {
                    var angleDelta = Mathf.DeltaAngle(segmentAngles[i], warning.ScreenAngleDegrees);
                    var rawWeight = Mathf.Exp(-(angleDelta * angleDelta) / doubleSigmaSquared);

                    // 裾を切り捨てたうえで再正規化し、光る範囲を絞りつつピーク輝度は保つ
                    weight = Mathf.Max(0f, (rawWeight - SegmentWeightCutoff) / (1f - SegmentWeightCutoff));
                }

                segmentImage.color = new Color(glowColor.r, glowColor.g, glowColor.b, pulseAlpha * weight);
            }
        }

        /// <summary>正規化距離（0=近い / 1=遠い）から 赤→黄→緑 のグラデーション色を求める。</summary>
        private Color EvaluateDistanceColor(float normalizedDistance)
        {
            if (normalizedDistance < 0.5f)
            {
                return Color.Lerp(activeParameter.WarningNearColor, activeParameter.WarningMidColor, normalizedDistance * 2f);
            }

            return Color.Lerp(activeParameter.WarningMidColor, activeParameter.WarningFarColor, (normalizedDistance - 0.5f) * 2f);
        }

        /// <summary>
        /// 解像度変更やパラメーター変更があった場合のみ円環レイアウトを再構築する。
        /// </summary>
        private void EnsureRingLayout()
        {
            var canvasSize = canvasRectTransform.rect.size;
            var segmentCount = Mathf.Clamp(activeParameter.WarningRingSegmentCount, MinSegmentCount, MaxSegmentCount);
            var thickness = Mathf.Max(activeParameter.WarningRingThickness, 1f);

            var isLayoutValid = segmentImages != null
                && builtSegmentCount == segmentCount
                && Mathf.Approximately(builtThickness, thickness)
                && Mathf.Approximately(builtCanvasSize.x, canvasSize.x)
                && Mathf.Approximately(builtCanvasSize.y, canvasSize.y);

            if (isLayoutValid)
            {
                return;
            }

            BuildRing(canvasSize, segmentCount, thickness);

            builtCanvasSize = canvasSize;
            builtSegmentCount = segmentCount;
            builtThickness = thickness;
        }

        /// <summary>
        /// 画面外周に沿ってセグメントを等間隔に並べる。
        /// 角度で等分すると縦横比によって密度が偏るため、外周の長さで等分している。
        /// </summary>
        private void BuildRing(Vector2 canvasSize, int segmentCount, float thickness)
        {
            ClearRing();

            segmentImages = new Image[segmentCount];
            segmentAngles = new float[segmentCount];

            var width = canvasSize.x;
            var height = canvasSize.y;
            var perimeter = 2f * (width + height);
            var segmentLength = perimeter / segmentCount * SegmentOverlapRatio;

            for (var i = 0; i < segmentCount; i++)
            {
                var distanceAlongPerimeter = (i + 0.5f) / segmentCount * perimeter;
                CalculateBorderPlacement(distanceAlongPerimeter, width, height, out var borderPosition, out var rotation);

                var segmentGo = new GameObject($"WarningSegment_{i}");
                segmentGo.transform.SetParent(ringRoot, false);

                var segmentImage = segmentGo.AddComponent<Image>();
                segmentImage.sprite = glowSprite;
                segmentImage.type = Image.Type.Simple;
                segmentImage.raycastTarget = false;
                segmentImage.color = Color.clear;

                var rectTransform = segmentImage.rectTransform;
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                // ピボットを上辺に置くことで、外周の点を基準に画面内側へ向かって光が伸びる
                rectTransform.pivot = new Vector2(0.5f, 1f);
                rectTransform.sizeDelta = new Vector2(segmentLength, thickness);
                rectTransform.anchoredPosition = borderPosition;
                rectTransform.localRotation = Quaternion.Euler(0f, 0f, rotation);

                segmentImages[i] = segmentImage;
                segmentAngles[i] = CalculateAngleFromCenter(borderPosition);
            }
        }

        /// <summary>
        /// 外周上の距離から、画面中心を原点とした配置座標と回転角（外側が常に上を向くように）を求める。
        /// </summary>
        private static void CalculateBorderPlacement(float distanceAlongPerimeter, float width, float height, out Vector2 borderPosition, out float rotation)
        {
            var halfWidth = width * 0.5f;
            var halfHeight = height * 0.5f;

            if (distanceAlongPerimeter < width)
            {
                // 上辺：左 → 右
                borderPosition = new Vector2(-halfWidth + distanceAlongPerimeter, halfHeight);
                rotation = 0f;
                return;
            }

            if (distanceAlongPerimeter < width + height)
            {
                // 右辺：上 → 下
                var offset = distanceAlongPerimeter - width;
                borderPosition = new Vector2(halfWidth, halfHeight - offset);
                rotation = -90f;
                return;
            }

            if (distanceAlongPerimeter < width * 2f + height)
            {
                // 下辺：右 → 左
                var offset = distanceAlongPerimeter - width - height;
                borderPosition = new Vector2(halfWidth - offset, -halfHeight);
                rotation = 180f;
                return;
            }

            // 左辺：下 → 上
            var leftOffset = distanceAlongPerimeter - width * 2f - height;
            borderPosition = new Vector2(-halfWidth, -halfHeight + leftOffset);
            rotation = 90f;
        }

        /// <summary>画面中心から見たセグメントの角度（画面上方向を 0 度、時計回りを正）を求める。</summary>
        private static float CalculateAngleFromCenter(Vector2 positionFromCenter)
        {
            var angle = Mathf.Atan2(positionFromCenter.x, positionFromCenter.y) * Mathf.Rad2Deg;
            return angle < 0f ? angle + 360f : angle;
        }

        private void ClearRing()
        {
            if (segmentImages == null)
            {
                return;
            }

            foreach (var segmentImage in segmentImages)
            {
                if (segmentImage != null)
                {
                    Destroy(segmentImage.gameObject);
                }
            }

            segmentImages = null;
            segmentAngles = null;
        }

        /// <summary>
        /// 外側（上端）が最も濃く、内側へ向かって透明になるグラデーションスプライトを生成する。
        /// 画面のフチが柔らかく光っているように見せるために使用する。
        /// </summary>
        private static Sprite CreateGlowSprite()
        {
            var texture = new Texture2D(1, GlowTextureHeight, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            for (var y = 0; y < GlowTextureHeight; y++)
            {
                var ratio = y / (float)(GlowTextureHeight - 1);
                // 二乗で減衰させることで外周に光が集中し、内側は自然にフェードする
                texture.SetPixel(0, y, new Color(1f, 1f, 1f, ratio * ratio));
            }

            texture.Apply();

            return Sprite.Create(texture, new Rect(0f, 0f, 1f, GlowTextureHeight), new Vector2(0.5f, 0.5f));
        }

        private void OnApplicationQuit()
        {
            isDestroying = true;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }
    }
}
