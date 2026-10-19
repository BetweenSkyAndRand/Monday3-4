using System.Collections.Generic;
using Game.Combat;
using Game.Weapon;
using Game.Debug;
using UnityEngine;

namespace Game.Player.Script
{
    /// <summary>
    /// プレイヤーの武器HitBoxに当たった対象にダメージを与えるコンポーネント。
    /// SampleHitBox が JSON から生成した BoxCollider GameObject に AddComponent して使う。
    /// OnTriggerEnter / OnTriggerStay で敵を検出してダメージを与える。
    /// このクラス自体は BoxCollider を要求しない（HitBox エディタで配置した攻撃範囲用
    /// BoxCollider を「使う側」であって、勝手に新規生成すべきではない設計）。
    /// </summary>
    public class PlayerWeaponHitBox : MonoBehaviour
    {
        [Header("デバッグ")]
        [SerializeField] private bool enableHitDebugLog = true;

        private readonly HashSet<int> damagedTargetInstanceIds = new();
        private PlayerEquipment equipment;
        private int currentDamage;
        private bool isActive;

        /// <summary>
        /// PlayerEquipment を設定して初期化する。
        /// HitBoxController.Rebuild() から呼ばれることを想定。
        /// </summary>
        public void Initialize(PlayerEquipment playerEquipment, int damage)
        {
            equipment = playerEquipment;
            currentDamage = damage;
        }

        /// <summary>
        /// HitBox の有効/無効を切り替える。
        /// 有効化したときだけダメージを与える。
        /// </summary>
        public void SetActive(bool active)
        {
            isActive = active;

            if (!active)
            {
                damagedTargetInstanceIds.Clear();
            }
        }

        public void BeginAttackWindow()
        {
            damagedTargetInstanceIds.Clear();
            isActive = true;
        }

        public void EndAttackWindow()
        {
            isActive = false;
            damagedTargetInstanceIds.Clear();
        }

        private void OnTriggerEnter(Collider other)
        {
            TryApplyDamage(other);
        }

        private void OnTriggerStay(Collider other)
        {
            TryApplyDamage(other);
        }

        private void TryApplyDamage(Collider other)
        {
            if (!isActive || other == null)
            {
                return;
            }

            if (other.CompareTag("Player"))
            {
                return;
            }

            var damageable = other.GetComponentInParent<IDamageable>();
            if (damageable == null)
            {
                return;
            }

            var targetRootObject = other.transform.root.gameObject;
            var targetInstanceId = targetRootObject.GetInstanceID();
            if (damagedTargetInstanceIds.Contains(targetInstanceId))
            {
                return;
            }

            damagedTargetInstanceIds.Add(targetInstanceId);

            damageable.TakeDamage(currentDamage);
            PlayHitPresentation(targetRootObject.transform);

            if (enableHitDebugLog)
            {
                UnityEngine.Debug.Log($"<color=#FF4D4D><b>[PlayerWeaponHitBox] HIT</b></color> target=<color=#FFD966><b>{targetRootObject.name}</b></color>, damage=<color=#FF9E00><b>{currentDamage}</b></color>");
            }

            GameLogger.Log(LogCategory.Combat, $"攻撃成功！ {targetRootObject.name} に {currentDamage} ダメージを与えました");
        }

        private void PlayHitPresentation(Transform targetTransform)
        {
            var currentWeapon = equipment != null ? equipment.CurrentWeapon : null;
            if (currentWeapon == null)
            {
                return;
            }

            if (currentWeapon.HitVfxPrefab != null)
            {
                var vfxManager = Effect.VFXManager.Instance;
                if (vfxManager != null)
                {
                    vfxManager.PlayEffect(currentWeapon.HitVfxPrefab, targetTransform.position, Quaternion.identity);
                }
            }

            // ID ベースで登録されていれば AudioManager.PlayConfiguredSe3D を優先する。
            // 未設定の場合のみ従来の AudioClip 直渡しにフォールバックする。
            var hitSeId = currentWeapon.HitSeId;
            var hitSeClip = currentWeapon.HitSe;
            if (!string.IsNullOrEmpty(hitSeId) || hitSeClip != null)
            {
                var audioManager = Audio.AudioManager.Instance;
                if (audioManager != null)
                {
                    if (!string.IsNullOrEmpty(hitSeId))
                    {
                        audioManager.PlayConfiguredSe3D(hitSeId, targetTransform.position);
                    }
                    else
                    {
                        audioManager.PlaySe3D(hitSeClip, targetTransform.position);
                    }
                }
            }
        }
    }
}

