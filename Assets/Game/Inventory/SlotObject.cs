using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Game.Inventory
{
    public struct SlotObject
    {
        ///
        [SerializeField] public uint id;
        /// <summary>
        /// UI画像の表示を司るクラス
        /// 所持しているアイテムのアイコンを表示する
        ///</summary>
        [SerializeField] public Image icon;
        /// <summary>
        /// スロット内のアイテム数を表示するテキスト
        /// </summary>
        [SerializeField] public TextMeshProUGUI numberText;

    }
}