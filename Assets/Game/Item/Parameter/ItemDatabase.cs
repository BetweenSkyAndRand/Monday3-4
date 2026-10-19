using System.Collections.Generic;
using UnityEngine;

namespace Game.Item.Parameter
{
    /// <summary>
    /// アイテムマスターデータベース。ItemParameter の一覧を保持し、itemId をキーに検索できるようにする。
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Item/ItemDatabase", fileName = "ItemDatabase")]
    public class ItemDatabase : ScriptableObject
    {
        [SerializeField] private ItemParameter[] items;

        private Dictionary<string, ItemParameter> itemCache;

        /// <summary>
        /// itemId から ItemParameter を取得する。見つからない場合は null を返す。
        /// </summary>
        public ItemParameter GetItem(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                return null;
            }

            EnsureCache();

            if (itemCache.TryGetValue(itemId, out var itemParameter))
            {
                return itemParameter;
            }

            UnityEngine.Debug.LogWarning($"[ItemDatabase] アイテムが見つかりません: {itemId}", this);
            return null;
        }

        /// <summary>ItemParameter.WeaponData が一致するアイテムを返す。見つからない場合は null。</summary>
        public ItemParameter FindByWeaponData(Game.Weapon.WeaponData weaponData)
        {
            if (weaponData == null)
            {
                return null;
            }

            EnsureCache();

            foreach (var itemParameter in itemCache.Values)
            {
                if (itemParameter.WeaponData == weaponData)
                {
                    return itemParameter;
                }
            }

            return null;
        }

        /// <summary>指定レア度が有効なアイテムの一覧（ドロップ・宝箱の2段階目の抽選対象）。</summary>
        public List<ItemParameter> GetEnabledItems(ItemRarity rarity)
        {
            EnsureCache();

            var enabledItems = new List<ItemParameter>();
            foreach (var itemParameter in itemCache.Values)
            {
                if (itemParameter.IsRarityEnabled(rarity))
                {
                    enabledItems.Add(itemParameter);
                }
            }

            return enabledItems;
        }

        public IEnumerable<string> GetAllItemIds()
        {
            EnsureCache();
            return itemCache.Keys;
        }

        private void EnsureCache()
        {
            if (itemCache != null)
            {
                return;
            }

            itemCache = new Dictionary<string, ItemParameter>();
            if (items == null)
            {
                return;
            }

            foreach (var itemParameter in items)
            {
                if (itemParameter == null)
                {
                    UnityEngine.Debug.LogWarning("[ItemDatabase] items に null が含まれています。スキップします。", this);
                    continue;
                }

                if (string.IsNullOrEmpty(itemParameter.ItemId))
                {
                    UnityEngine.Debug.LogWarning($"[ItemDatabase] itemId が空のアイテムがあります。スキップします。asset={itemParameter.name}", this);
                    continue;
                }

                if (itemCache.ContainsKey(itemParameter.ItemId))
                {
                    UnityEngine.Debug.LogWarning($"[ItemDatabase] 重複した itemId があります: {itemParameter.ItemId}。後の定義で上書きします。", this);
                }

                itemCache[itemParameter.ItemId] = itemParameter;
            }
        }
        public List<ItemParameter> GetAllItemsInOrder()
        {
            var orderedItems = new List<ItemParameter>();
            if (items == null) return orderedItems;
            var addedItemIds = new HashSet<string>();
            foreach (var itemParameter in items)
            {
                if (itemParameter == null || string.IsNullOrEmpty(itemParameter.ItemId)) continue;
                if (addedItemIds.Add(itemParameter.ItemId)) orderedItems.Add(itemParameter);
            }
            return orderedItems;
        }
        private void OnValidate()
        {
            itemCache = null;
        }
    }
}
