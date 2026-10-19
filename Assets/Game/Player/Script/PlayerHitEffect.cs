using UnityEngine;
using Game.Presentation;

namespace Game.Player.Script
{
    /// <summary>
    /// プレイヤーが被弾した際の演出を管理するクラス。
    /// 画面フラッシュ、SE、プレイヤーモデルの発光（オプション）を再生する。
    /// モデル発光は PlayerModelFlash に委譲している。
    /// 責務: 被弾演出の再生のみ。ダメージ処理は IDamageable に委譲。
    /// </summary>
    public class PlayerHitEffect : MonoBehaviour
    {
        [Header("演出設定")]
        [SerializeField] private AudioClip hitSe;
        [SerializeField] private Color flashColor = new Color(1f, 0f, 0f, 0.5f);
        [SerializeField] private float flashDuration = 0.2f;

        [Header("モデル発光（オプション）")]
        [SerializeField] private bool enableModelFlash = true;
        [SerializeField] private PlayerModelFlash playerModelFlash;

        /// <summary>被弾演出を再生する（外部から呼び出される）。</summary>
        public void PlayHitEffect()
        {
            PlayScreenFlash();
            PlayHitSound();

            if (enableModelFlash && playerModelFlash != null)
            {
                playerModelFlash.Flash();
            }
        }

        private void PlayScreenFlash()
        {
            var screenEffectManager = Effect.ScreenEffectManager.Instance;
            if (screenEffectManager != null)
            {
                screenEffectManager.Flash(ResolveFlashColor(), ResolveFlashDuration());
            }
        }

        private void PlayHitSound()
        {
            var resolvedHitSe = ResolveHitSe();
            if (resolvedHitSe == null)
            {
                return;
            }

            var audioManager = Audio.AudioManager.Instance;
            if (audioManager != null)
            {
                audioManager.PlaySe(resolvedHitSe);
            }
        }

        private AudioClip ResolveHitSe()
        {
            var settings = GamePresentationSettings.Instance;
            if (settings != null && settings.PlayerHitSe != null)
            {
                return settings.PlayerHitSe;
            }

            return hitSe;
        }

        private Color ResolveFlashColor()
        {
            var settings = GamePresentationSettings.Instance;
            if (settings != null)
            {
                return settings.PlayerHitFlashColor;
            }

            return flashColor;
        }

        private float ResolveFlashDuration()
        {
            var settings = GamePresentationSettings.Instance;
            if (settings != null)
            {
                return settings.PlayerHitFlashDuration;
            }

            return flashDuration;
        }
    }
}

