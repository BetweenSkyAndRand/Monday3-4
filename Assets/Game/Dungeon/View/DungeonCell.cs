using UnityEngine;

namespace Game.Dungeon
{
    /// <summary>
    /// 1 つのセルを表す MonoBehaviour。
    /// MaterialPropertyBlock を使い、マテリアルを共有したまま色を変更する。
    /// </summary>
    public class DungeonCell : MonoBehaviour
    {
        /// <summary>セルの表示状態を保持する ViewModel。</summary>
        public struct ViewModel
        {
            public Color cellColor;
        }

        [SerializeField] private Renderer cellRenderer;

        // URP は "_BaseColor"、Built-in は "_Color" を使うため両方セットして両シェーダーに対応する。
        // 存在しないプロパティへの SetColor は Unity が無視するため副作用はない。
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId     = Shader.PropertyToID("_Color");

        private MaterialPropertyBlock propertyBlock;

        private void Awake()
        {
            if (cellRenderer == null)
                cellRenderer = GetComponent<Renderer>();
            propertyBlock = new MaterialPropertyBlock();
        }

        /// <summary>エンティティの情報をこのセルに反映する。</summary>
        public void ApplyDungeonCellEntity(DungeonCellEntity entity)
        {
            transform.position = entity.WorldPosition;
            ApplyViewModel(new ViewModel { cellColor = entity.CellColor });
        }

        /// <summary>ViewModel の色をレンダラーに反映する。</summary>
        public void ApplyViewModel(ViewModel viewModel)
        {
            if (cellRenderer == null) return;

            cellRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(BaseColorId, viewModel.cellColor);
            propertyBlock.SetColor(ColorId,     viewModel.cellColor);
            cellRenderer.SetPropertyBlock(propertyBlock);
        }
    }
}


