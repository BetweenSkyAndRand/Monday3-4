using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Game.Dungeon
{
    /// <summary>
    /// ダンジョン崩壊システムの調整可能パラメーターを保持する ScriptableObject。
    /// 崩壊間隔は「調整できる余地を残す」ため数値として公開するが、
    /// 初期値は仕様どおり毎回同じ間隔（固定値）として扱う。
    /// </summary>
    [CreateAssetMenu(fileName = "DungeonCollapseParameter", menuName = "Game/Dungeon/DungeonCollapseParameter")]
    public class DungeonCollapseParameter : ScriptableObject
    {
        [Header("タイミング設定")]
        [Tooltip("探索開始から最初の崩壊が始まるまでの秒数。")]
        [SerializeField] private float firstDelaySeconds = 60f;

        [Tooltip("崩壊と崩壊の間隔（秒）。現状は毎回同じ値を使用する。")]
        [SerializeField] private float intervalSeconds = 30f;

        [Tooltip("崩壊が実際に発生する何秒前から警告演出を開始するか。")]
        [SerializeField] private float warningDurationSeconds = 5f;

        [Header("警告演出設定")]
        [Tooltip("崩壊対象の部屋を光らせる色。")]
        [SerializeField] private Color warningGlowColor = new Color(1f, 0.3f, 0.1f);

        [Tooltip("警告演出中の発光の点滅速度。")]
        [SerializeField] private float warningPulseSpeed = 4f;

        [Tooltip("警告時の画面揺れの強さ。")]
        [SerializeField] private float screenShakeMagnitude = 0.15f;

        [Header("演出アセット")]
        [SerializeField] private AudioClip warningSe;
        [SerializeField] private AudioClip collapseSe;
        [SerializeField] private GameObject collapseVfxPrefab;

        [Header("警告UI（画面周囲の円環グロー）")]
        [Tooltip("円環を構成するセグメント数。多いほど滑らかになる（32〜64程度を推奨）。")]
        [SerializeField] private int warningRingSegmentCount = 48;

        [Tooltip("円環グローの太さ（ピクセル）。画面の縁からどれだけ内側へ光が伸びるか。")]
        [SerializeField] private float warningRingThickness = 140f;

        [Tooltip("この距離以上離れている場合は「最も遠い」扱いになる（ワールド単位）。")]
        [SerializeField] private float maxWarningDistance = 90f;

        [Tooltip("崩壊部屋が最も近いときの色。")]
        [SerializeField] private Color warningNearColor = new Color(1f, 0.15f, 0.1f);

        [Tooltip("崩壊部屋が中距離のときの色。")]
        [SerializeField] private Color warningMidColor = new Color(1f, 0.85f, 0.15f);

        [Tooltip("崩壊部屋が最も遠いときの色。")]
        [SerializeField] private Color warningFarColor = new Color(0.2f, 1f, 0.35f);

        [Tooltip("最も近いときの光の帯の広がり（度）。小さいほど方向がピンポイントになる。")]
        [SerializeField] private float warningNearSpreadDegrees = 12f;

        [Tooltip("最も遠いときの光の帯の広がり（度）。大きいほど大まかな方角表示になる。")]
        [SerializeField] private float warningFarSpreadDegrees = 32f;

        [Tooltip("最も近いときの点滅速度。")]
        [SerializeField] private float warningNearPulseSpeed = 11f;

        [Tooltip("最も遠いときの点滅速度。")]
        [SerializeField] private float warningFarPulseSpeed = 3f;

        [Tooltip("崩壊対象部屋の中にいるときの点滅速度（全周が赤で点滅する）。")]
        [SerializeField] private float warningInsidePulseSpeed = 15f;

        [Tooltip("点滅の最も暗い瞬間の不透明度。")]
        [SerializeField] private float warningMinAlpha = 0.1f;

        [Tooltip("点滅の最も明るい瞬間の不透明度。")]
        [SerializeField] private float warningMaxAlpha = 0.85f;

        public float FirstDelaySeconds => firstDelaySeconds;
        public float IntervalSeconds => intervalSeconds;
        public float WarningDurationSeconds => warningDurationSeconds;
        public Color WarningGlowColor => warningGlowColor;
        public float WarningPulseSpeed => warningPulseSpeed;
        public float ScreenShakeMagnitude => screenShakeMagnitude;
        public AudioClip WarningSe => warningSe;
        public AudioClip CollapseSe => collapseSe;
        public GameObject CollapseVfxPrefab => collapseVfxPrefab;
        public int WarningRingSegmentCount => warningRingSegmentCount;
        public float WarningRingThickness => warningRingThickness;
        public float MaxWarningDistance => maxWarningDistance;
        public Color WarningNearColor => warningNearColor;
        public Color WarningMidColor => warningMidColor;
        public Color WarningFarColor => warningFarColor;
        public float WarningNearSpreadDegrees => warningNearSpreadDegrees;
        public float WarningFarSpreadDegrees => warningFarSpreadDegrees;
        public float WarningNearPulseSpeed => warningNearPulseSpeed;
        public float WarningFarPulseSpeed => warningFarPulseSpeed;
        public float WarningInsidePulseSpeed => warningInsidePulseSpeed;
        public float WarningMinAlpha => warningMinAlpha;
        public float WarningMaxAlpha => warningMaxAlpha;
    }
}

