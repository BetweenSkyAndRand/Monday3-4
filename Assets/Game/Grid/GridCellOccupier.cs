using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Game.Grid
{
    /// <summary>
    /// このオブジェクトが置かれているグリッドセルを GridOccupationManager に占有登録する。
    /// 進入不可（プレイヤーが乗り上げられない）扱いにしたいオブジェクトに付ける。
    ///
    /// 想定用途:
    ///   - ボタン（BuildModeToggleObject）と同じ「マスにオブジェクトがあるので進めない」挙動を
    ///     ボックス_red / blue などの障害物にも与えるため。
    ///
    /// 動作:
    ///   - Start(): transform の XZ から所属セルを計算し Occupy する
    ///   - OnDestroy(): 自身が占有しているセルを解放する
    /// </summary>
    [DisallowMultipleComponent]
    public class GridCellOccupier : MonoBehaviour
    {
        [Tooltip("1セルのワールドサイズ。プレイヤー(RogueMover)と同じ値にする。")]
        [SerializeField] private float cellSize = 1f;

        private void Start()
        {
            // このオブジェクトがいるグリッド座標を占有する
            var gridPos = new Vector2Int(
                Mathf.RoundToInt(transform.position.x / cellSize),
                Mathf.RoundToInt(transform.position.z / cellSize)
            );
            GridOccupationManager.Occupy(gridPos, gameObject);
        }

        private void OnDestroy()
        {
            // グリッド占有を解放
            GridOccupationManager.ReleaseByOccupant(gameObject);
        }
    }
}

