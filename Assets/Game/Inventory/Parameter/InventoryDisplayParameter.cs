using System;
using System.Collections.Generic;
using Game.Debug;
using UnityEngine;

namespace Game.Inventory.Parameter
{
    [Serializable]
    public struct InventoryIconEntry
    {
        public string itemId;
        public Sprite icon;
    }

    [CreateAssetMenu(fileName = "InventoryDisplayParameter", menuName = "Game/Inventory/InventoryDisplayParameter")]
    public class InventoryDisplayParameter : ScriptableObject
    {
        [Header("基本設定")]
        [Tooltip("インベントリのスロット数。ScriptableObjectアセットをInspectorで編集するとGUIから変更できます。")]
        [Min(1)]
        [SerializeField] private uint slotCount = 4;
        [SerializeField] private float openCloseCooldown = 0.2f;
        [SerializeField] private Sprite emptySlotSprite;
        [SerializeField] private InventoryIconEntry[] iconEntries;

        private Dictionary<string, Sprite> iconLookup;
        private Dictionary<string, Sprite> normalizedIconLookup;

        public uint SlotCount => slotCount;
        public float OpenCloseCooldown => openCloseCooldown;
        public Sprite EmptySlotSprite => emptySlotSprite;

        public Sprite ResolveIcon(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                return emptySlotSprite;
            }

            if (iconLookup == null)
            {
                BuildLookup();
            }

            if (!iconLookup.TryGetValue(itemId, out var sprite))
            {
                var normalizedKey = NormalizeItemId(itemId);
                if (!string.IsNullOrEmpty(normalizedKey) && normalizedIconLookup.TryGetValue(normalizedKey, out var normalizedSprite))
                {
                    sprite = normalizedSprite;
                }
                else
                {
                    // アイコンが未設定の場合は警告を出す（アイテム自体は正しく存在する）
                    GameLogger.LogWarning(LogCategory.Inventory,
                        $"itemId '{itemId}' のアイコンが InventoryDisplayParameter に未設定です。" +
                        "Inspector の iconEntries にアイコンを登録してください。");
                    return emptySlotSprite;
                }
            }

            if (sprite == null)
            {
                GameLogger.LogWarning(LogCategory.Inventory,
                    $"itemId '{itemId}' のアイコンは登録されていますが Sprite が null です。");
                return emptySlotSprite;
            }

            return sprite;
        }

        private void BuildLookup()
        {
            iconLookup = new Dictionary<string, Sprite>();
            normalizedIconLookup = new Dictionary<string, Sprite>();
            foreach (var entry in iconEntries)
            {
                if (string.IsNullOrEmpty(entry.itemId))
                {
                    continue;
                }

                iconLookup[entry.itemId] = entry.icon;

                var normalizedKey = NormalizeItemId(entry.itemId);
                if (!string.IsNullOrEmpty(normalizedKey))
                {
                    normalizedIconLookup[normalizedKey] = entry.icon;
                }
            }
        }

        private static string NormalizeItemId(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                return string.Empty;
            }

            var normalized = itemId.ToLowerInvariant();
            normalized = normalized.Replace(" ", string.Empty);
            normalized = normalized.Replace("_", string.Empty);
            normalized = normalized.Replace("-", string.Empty);
            return normalized;
        }

        private void OnValidate()
        {
            iconLookup = null;
            normalizedIconLookup = null;
        }
    }
}

