using UnityEngine;
using UnityEngine.UI;

namespace Game.Enemy.View
{
    /// <summary>
    /// 敵のHPバー表示を管理するView（見た目更新のみ）。
    /// ゲームロジックは持たない。
    /// </summary>
    public class EnemyHpBarView : MonoBehaviour
    {
        [SerializeField] private Slider hpSlider;
        [SerializeField] private Image fillImage;
        [SerializeField] private Gradient hpGradient;

        private void Awake()
        {
            // 表示専用のため、EventSystem の入力(移動キー・クリック)で値が変わらないようにする
            if (hpSlider != null)
            {
                hpSlider.interactable = false;
                hpSlider.navigation = new Navigation { mode = Navigation.Mode.None };
            }
        }

        public void SetHp(float currentHp, float maxHp)
        {
            var safeMaxHp = Mathf.Max(0.01f, maxHp);
            var ratio = Mathf.Clamp01(currentHp / safeMaxHp);

            if (hpSlider != null)
            {
                hpSlider.SetValueWithoutNotify(ratio);
            }

            if (fillImage != null)
            {
                fillImage.color = hpGradient.Evaluate(ratio);
            }
        }

        public void SetVisible(bool isVisible)
        {
            gameObject.SetActive(isVisible);
        }
    }
}

