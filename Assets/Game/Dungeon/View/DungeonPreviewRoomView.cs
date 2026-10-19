using UnityEngine;

namespace Game.Dungeon
{
    /// <summary>
    /// プレビュー専用の部屋ビュー。1 インスタンスを使い回す。
    /// シングルの Renderer（例: Scale 13x1x13 の Cube）で部屋全体を表し、
    /// 配置可否に応じて色を変更する。
    /// 個別セルの色変更が不要なため DungeonCell 配列は使用しない。
    /// </summary>
    public class DungeonPreviewRoomView : MonoBehaviour
    {
        [SerializeField] private Renderer previewRenderer;

        // URP は "_BaseColor"、Built-in は "_Color" を使うため両方セットして対応する
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId     = Shader.PropertyToID("_Color");

        private MaterialPropertyBlock propertyBlock;

        private void Awake()
        {
            // previewRenderer が未設定またはマテリアルが null の場合、
            // 子オブジェクト内でマテリアルを持つ Renderer を探してフォールバックする。
            // （Inspector で Root の Renderer を誤って参照した場合にも対応）
            if (previewRenderer == null || previewRenderer.sharedMaterial == null)
            {
                foreach (var candidate in GetComponentsInChildren<Renderer>())
                {
                    if (candidate.sharedMaterial != null)
                    {
                        previewRenderer = candidate;
                        break;
                    }
                }
            }

            if (previewRenderer == null)
                UnityEngine.Debug.LogError("[DungeonPreviewRoomView] マテリアルを持つ Renderer が見つかりません。Floor 子オブジェクトに MeshRenderer とマテリアルが設定されているか確認してください。");

            propertyBlock = new MaterialPropertyBlock();
        }

        /// <summary>
        /// エンティティの中心セル座標にプレビューを移動する。
        /// Scale 13x1x13 の Cube と組み合わせると部屋境界にぴったり合致する。
        /// </summary>
        public void ApplyDungeonRoomEntity(DungeonRoomEntity roomEntity)
        {
            if (roomEntity.CellEntities == null || roomEntity.CellEntities.Length == 0) return;

            // 13x13 の中央インデックス = 6*13+6 = 84（中心セルのワールド座標 = 部屋中心）
            var centerIndex = roomEntity.CellEntities.Length / 2;
            transform.position = roomEntity.CellEntities[centerIndex].WorldPosition;
        }

        /// <summary>
        /// プレビュー全体の色を変更する（配置可否フィードバック用）。
        /// </summary>
        public void SetCellColor(DungeonCell.ViewModel viewModel)
        {
            if (previewRenderer == null) return;

            previewRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(BaseColorId, viewModel.cellColor);
            propertyBlock.SetColor(ColorId,     viewModel.cellColor);
            previewRenderer.SetPropertyBlock(propertyBlock);
        }
    }
}

