using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Game.Interaction
{
    /// <summary>
    /// インタラクティブUIに表示するテキストを一括管理する ScriptableObject。
    /// 各インタラクタブルオブジェクトはこの設定を参照してテキストを取得する。
    /// 日本語・英語の切り替えや表現の変更が容易になる。
    ///
    /// 使用方法:
    ///   1. Project ウィンドウで右クリック → Create → Game → Interact Text Settings
    ///   2. 作成した .asset を適切なフォルダに配置
    ///   3. 各 Controller の Inspector で参照する
    /// </summary>
    [CreateAssetMenu(fileName = "InteractTextSettings", menuName = "Game/Interact Text Settings", order = 0)]
    public class InteractTextSettings : ScriptableObject
    {
        [Header("Storage Box")]
        [Tooltip("Text displayed when opening storage")]
        public string storageOpenText = "Open Storage";
        [Tooltip("Text displayed when closing storage")]
        public string storageCloseText = "Close Storage";

        [Header("Treasure Chest")]
        [Tooltip("Chest with weapon format. {0}:weapon name")]
        public string treasureBoxWeaponFormat = "Open Chest ({0})";
        [Tooltip("Chest with unknown weapon text")]
        public string treasureBoxWeaponGeneric = "Open Chest (Weapon)";
        [Tooltip("Chest with unknown contents text")]
        public string treasureBoxGeneric = "Open Chest";
        [Tooltip("Text when chest UI is already open")]
        public string treasureBoxCloseText = "Close Chest";

        [Header("Shop")]
        [Tooltip("Text displayed when opening the shop")]
        public string shopOpenText = "Open Shop";

        [Header("Weapon Pickup")]
        [Tooltip("Weapon pickup format. {0}:weapon name")]
        public string weaponPickupFormat = "Pick Up {0}";

        [Header("Build Mode")]
        [Tooltip("Build mode toggle button text")]
        public string buildModeToggleText = "部屋を拡張する";

        [Header("Build Mode HUD")]
        [Tooltip("Room count format. {0}:placed room count, {1}:max room count")]
        public string buildRoomCountFormat = "部屋数 {0} / {1}";
        [Tooltip("Message when the room limit is reached")]
        public string buildRoomLimitText = "部屋数が上限です\n（Escでキャンセル）";
        [Tooltip("Message when the preview position is placeable")]
        public string buildPlaceableText = "配置できます（クリックで確定）";
        [Tooltip("Message when the preview overlaps an existing room")]
        public string buildOverlappedText = "ほかの部屋と重なっています";
        [Tooltip("Message when no adjacent room exists")]
        public string buildNoNeighborText = "隣接する部屋が必要です";

        [Header("Exploration")]
        [Tooltip("End exploration text")]
        public string searchEndText = "End Exploration";

        [Header("Exploration End Confirm Dialog")]
        [Tooltip("Confirm dialog message")]
        [TextArea(2, 4)]
        public string searchEndConfirmMessage = "探索を終了しますか？\n拡張した部屋は崩壊します。";
        [Tooltip("Confirm button label")]
        public string searchEndConfirmYesText = "終了する";
        [Tooltip("Cancel button label")]
        public string searchEndConfirmNoText = "戻る";
    }
}
