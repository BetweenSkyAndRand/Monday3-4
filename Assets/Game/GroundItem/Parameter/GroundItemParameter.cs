using UnityEngine;

namespace Game.GroundItem
{
    [CreateAssetMenu(fileName = "GroundItemParameter", menuName = "Game/Item/Ground Item Parameter")]
    public class GroundItemParameter : ScriptableObject
    {
        [Tooltip("地面に落ちてから消えるまでの秒数。0 以下で消えない（既定 600 秒 = 10 分）")]
        [SerializeField] private float despawnSeconds = 600f;

        [Tooltip("ドロップ位置を散らす半径。0 で指定位置に完全に重なる")]
        [SerializeField] private float scatterRadius = 0.4f;

        [Tooltip("床から浮かせる高さ")]
        [SerializeField] private float dropHeightOffset = 0.2f;

        [Tooltip("床の高さ補正のレイキャスト対象レイヤー")]
        [SerializeField] private LayerMask groundLayerMask = ~0;

        [Tooltip("プレイヤーが投棄したアイテムを、向いている方向のどれだけ先に置くか")]
        [SerializeField] private float playerDropDistance = 1.5f;

        [Tooltip("インタラクト判定用トリガーの半径")]
        [SerializeField] private float pickupRadius = 0.4f;

        [Tooltip("壁などの遮蔽物とみなすレイヤー。投棄位置の補正に使う（床・プレイヤーは除くのが望ましい）")]
        [SerializeField] private LayerMask obstacleLayerMask = ~0;

        [Tooltip("壁からどれだけ手前に置くか")]
        [SerializeField] private float wallMargin = 0.3f;

        [Tooltip("プレイヤー死亡時にインベントリの中身を地面へ落とすか（ポーズ画面の切り替えの初期値）")]
        [SerializeField] private bool dropItemsOnDeath = true;

        public float DespawnSeconds => despawnSeconds;
        public float ScatterRadius => scatterRadius;
        public float DropHeightOffset => dropHeightOffset;
        public LayerMask GroundLayerMask => groundLayerMask;
        public float PickupRadius => pickupRadius;
        public float PlayerDropDistance => playerDropDistance;
        public LayerMask ObstacleLayerMask => obstacleLayerMask;
        public float WallMargin => wallMargin;
        public bool DropItemsOnDeath => dropItemsOnDeath;
    }
}