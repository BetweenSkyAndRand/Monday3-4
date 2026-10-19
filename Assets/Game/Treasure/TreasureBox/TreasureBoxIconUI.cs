using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ReSharper disable once CheckNamespace
namespace Game.Treasure
{
    /// <summary>
    /// 宝箱インタラクト時にアイコン付きで中身を表示する専用UI。
    /// シーンに 1 つだけ配置し、TreasureBoxController から ITreasureBoxIconProvider 経由で表示要求を受け取る。
    /// 既存の InteractUI（"[E] ～" のテキスト表示）とは別レイヤーとして動作する。
    /// </summary>
    public class TreasureBoxIconUI : MonoBehaviour
    {
        public static TreasureBoxIconUI Instance { get; private set; }

        [Header("UI参照")]
        [Tooltip("UI ルート。Show/Hide で SetActive する。")]
        [SerializeField] private GameObject root;

        [Tooltip("中身アイコンを表示する Image。")]
        [SerializeField] private Image iconImage;

        [Tooltip("数量表示（パッチ数など）。null 可。")]
        [SerializeField] private TextMeshProUGUI quantityText;

        [Tooltip("補足テキスト（武器名など）。null 可。")]
        [SerializeField] private TextMeshProUGUI labelText;

        private void Awake()
        {
            Instance = this;

            if (root == null)
            {
                root = gameObject;
            }

            Hide();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// アイコン情報を指定して UI を表示する。
        /// </summary>
        /// <param name="icon">表示するアイコン Sprite。null の場合は Image を非表示にする。</param>
        /// <param name="quantity">数量。0 以下なら数量表示は非表示。</param>
        /// <param name="label">補足テキスト。null/空なら非表示。</param>
        public void Show(Sprite icon, int quantity, string label)
        {
            if (root != null)
            {
                root.SetActive(true);
            }

            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
            }

            if (quantityText != null)
            {
                if (quantity > 1)
                {
                    quantityText.text = $"x{quantity}";
                    quantityText.gameObject.SetActive(true);
                }
                else
                {
                    quantityText.gameObject.SetActive(false);
                }
            }

            if (labelText != null)
            {
                if (!string.IsNullOrEmpty(label))
                {
                    labelText.text = label;
                    labelText.gameObject.SetActive(true);
                }
                else
                {
                    labelText.gameObject.SetActive(false);
                }
            }
        }

        public void Hide()
        {
            if (root != null)
            {
                root.SetActive(false);
            }
        }
    }

    /// <summary>
    /// IInteractableObject の追加機能インターフェース。
    /// 実装オブジェクトはインタラクト UI に表示するアイコン情報を提供する。
    /// PlayerInteract が currentInteractable をキャストして利用する。
    /// </summary>
    public interface ITreasureBoxIconProvider
    {
        /// <summary>表示するアイコン。</summary>
        Sprite GetTreasureIcon();

        /// <summary>数量表示用の数。0 以下なら非表示。</summary>
        int GetTreasureQuantity();

        /// <summary>補足ラベル（武器名など）。null/空なら非表示。</summary>
        string GetTreasureLabel();
    }
}

