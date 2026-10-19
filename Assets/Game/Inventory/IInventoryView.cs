using System;
using System.Collections.Generic;
using Game.Inventory.Parameter;

namespace Game.Inventory
{
    public interface IInventoryView
    {
        void Initialize(InventoryDisplayParameter parameter, int totalSlotCount);
        void Show();
        void Hide();
        void Toggle();
        void ApplyViewModel(IReadOnlyList<InventoryViewModel> viewModels);
        void SetSlotClickListener(Action<int> onClick);

        /// <summary>
        /// 装備ボタンのクリックリスナーを設定する。
        /// InventoryController.Awake() から呼び出す。
        /// </summary>
        void SetEquipButtonListener(Action onClick);

        /// <summary>武器スロットが選択されているときに装備ボタンを表示する。</summary>
        void ShowEquipButton();

        /// <summary>武器以外が選択されているとき、または選択解除されたときに装備ボタンを隠す。</summary>
        void HideEquipButton();

        /// <summary>
        /// アイテム捨てるボタンのクリックリスナーを設定する。
        /// InventoryController.Awake() から呼び出す。
        /// </summary>
        void SetDiscardButtonListener(Action onDiscard);

        /// <summary>アイテムが選択されているときに捨てるボタンを表示する。</summary>
        void ShowDiscardButton();

        /// <summary>アイテムが選択されていないとき、または空スロット選択時に捨てるボタンを隠す。</summary>
        void HideDiscardButton();

    }
}
