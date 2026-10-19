using UnityEngine;
using UnityEngine.UI;

namespace Game.Item
{
    /// <summary>
    /// アイコン Image の子として、レア度の枠 Image を自動生成して重ねる。
    /// UI ごとに枠用の部品を手で置かなくて済むようにするための共通処理。
    /// </summary>
    public static class RarityFrameOverlay
    {
        private const string FrameObjectName = "RarityFrame";

        /// <summary>
        /// 枠を表示する。frameSprite が null のときは枠を隠す（空スロット・アイコンなし用）。
        /// </summary>
        public static void Apply(Image iconImage, Sprite frameSprite)
        {
            if (iconImage == null)
            {
                return;
            }

            var frameTransform = iconImage.transform.Find(FrameObjectName);
            if (frameSprite == null)
            {
                if (frameTransform != null)
                {
                    frameTransform.gameObject.SetActive(false);
                }

                return;
            }

            if (frameTransform == null)
            {
                frameTransform = CreateFrameObject(iconImage.transform);
            }

            var frameImage = frameTransform.GetComponent<Image>();
            frameImage.sprite = frameSprite;
            frameTransform.gameObject.SetActive(true);
        }

        private static Transform CreateFrameObject(Transform iconTransform)
        {
            var frameObject = new GameObject(FrameObjectName, typeof(RectTransform), typeof(Image));
            var frameRectTransform = (RectTransform)frameObject.transform;
            frameRectTransform.SetParent(iconTransform, false);
            frameRectTransform.anchorMin = Vector2.zero;
            frameRectTransform.anchorMax = Vector2.one;
            frameRectTransform.offsetMin = Vector2.zero;
            frameRectTransform.offsetMax = Vector2.zero;

            // 枠がクリックやホバーを奪わないようにする
            var frameImage = frameObject.GetComponent<Image>();
            frameImage.raycastTarget = false;
            return frameRectTransform;
        }
    }
}
