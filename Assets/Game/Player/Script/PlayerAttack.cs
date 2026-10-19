using System.Collections;
using Game.Player;
using Game.Utility;
using Game.Weapon;
using UnityEngine;
namespace Game.Player.Script
{
    /// <summary>
    /// プレイヤーの攻撃処理を担当する司令塔クラス。
    /// 攻撃入力を受け取り、クールタイムと武器の状態を確認して攻撃を実行する。
    /// HitBox の管理は HitBoxController、クールタイムは AttackCooldown に委譲する。
    /// 自由移動仕様では攻撃中も移動可能なため、IMovementBlocker は互換維持のための無効実装とする。
    /// </summary>
    public class PlayerAttack : MonoBehaviour, IMovementBlocker
    {
        [SerializeField] private GameInputReader inputReader;
        [SerializeField] private PlayerEquipment equipment;
        [SerializeField] private HitBoxController hitBoxController;
        [SerializeField] private PlayerModelRotator playerModelRotator;
        [SerializeField] private MonoBehaviour playerMoverObject;
        [Header("アニメーション")]
        [Tooltip("プレイヤーモデルの Animator。WeaponData.AttackAnimationTrigger で指定されたトリガーを SetTrigger する。\n" +
                 "未設定の場合は子オブジェクトから自動検索する。null の場合はアニメ再生をスキップする。")]
        [SerializeField] private Animator playerAnimator;
        [Header("デバッグ")]
        [SerializeField] private bool enableAttackDebugLog = true;
        private AttackCooldown cooldown = new AttackCooldown(1f);
        private bool isAttacking;
        private IPlayerMover playerMover;
        public bool IsMovementBlocked => false;
        public bool IsAttacking => isAttacking;
        private void Awake()
        {
            if (inputReader == null)
            {
                inputReader = GetComponent<GameInputReader>();
            }
            if (equipment == null)
            {
                equipment = GetComponent<PlayerEquipment>();
            }
            if (hitBoxController == null)
            {
                hitBoxController = GetComponent<HitBoxController>();
            }
            if (playerModelRotator == null)
            {
                playerModelRotator = GetComponent<PlayerModelRotator>();
            }
            if (playerMoverObject == null)
            {
                var playerFreeMover = GetComponent<PlayerFreeMover>();
                if (playerFreeMover != null)
                {
                    playerMoverObject = playerFreeMover;
                }
                else
                {
                    playerMoverObject = GetComponent<RogueMover>();
                }
            }
            playerMover = MonoBehaviourCaster.CastSingle<IPlayerMover>(playerMoverObject, nameof(PlayerAttack));
            if (playerAnimator == null)
            {
                playerAnimator = GetComponentInChildren<Animator>();
            }
        }
        private void OnEnable()
        {
            if (inputReader != null)
            {
                inputReader.OnAttack += HandleAttack;
            }
            if (equipment != null)
            {
                equipment.OnWeaponChanged += OnWeaponChanged;
            }
        }
        private void OnDisable()
        {
            if (inputReader != null)
            {
                inputReader.OnAttack -= HandleAttack;
            }
            if (equipment != null)
            {
                equipment.OnWeaponChanged -= OnWeaponChanged;
            }
        }
        private void OnWeaponChanged(WeaponData weapon)
        {
            if (hitBoxController != null)
            {
                hitBoxController.Rebuild(weapon);
            }
            cooldown = new AttackCooldown(weapon != null ? weapon.CooldownTime : 1f);
            cooldown.Reset();
        }
        private void HandleAttack()
        {
            if (equipment == null)
            {
                return;
            }
            if (equipment.CurrentWeapon == null)
            {
                return;
            }
            if (isAttacking)
            {
                return;
            }
            if (!cooldown.IsReady)
            {
                return;
            }
            if (enableAttackDebugLog)
            {
                var weaponName = equipment.CurrentWeapon.WeaponName;
                UnityEngine.Debug.Log($"[PlayerAttack] 攻撃開始: weapon={weaponName}, damage={equipment.CurrentAttackPower}, duration={equipment.CurrentWeapon.AttackDuration:F2}");
            }
            StartCoroutine(AttackCoroutine(equipment.CurrentWeapon));
        }
        private IEnumerator AttackCoroutine(WeaponData weapon)
        {
            cooldown.Use();
            isAttacking = true;
            var attackRotation = ResolveAttackRotation();
            if (enableAttackDebugLog)
            {
                UnityEngine.Debug.Log($"[PlayerAttack] 攻撃ウィンドウ開始: weapon={weapon.WeaponName}, duration={weapon.AttackDuration:F2}");
            }
            PlayAttackEffect(weapon, attackRotation);
            PlayAttackAnimation(weapon);
            if (hitBoxController != null)
            {
                yield return StartCoroutine(hitBoxController.ActivateFor(weapon.AttackDuration, attackRotation));
            }
            else
            {
                yield return new WaitForSeconds(weapon.AttackDuration);
            }
            isAttacking = false;
            if (enableAttackDebugLog)
            {
                UnityEngine.Debug.Log($"[PlayerAttack] 攻撃ウィンドウ終了: weapon={weapon.WeaponName}");
            }
        }
        private void PlayAttackEffect(WeaponData weapon, Quaternion attackRotation)
        {
            if (weapon == null)
            {
                return;
            }
            if (weapon.AttackVfxPrefab != null)
            {
                var effectPosition = transform.position + attackRotation * weapon.VfxOffset;
                var vfxManager = Effect.VFXManager.Instance;
                if (vfxManager != null)
                {
                    vfxManager.PlayEffect(weapon.AttackVfxPrefab, effectPosition, attackRotation);
                }
            }
            if (weapon.AttackSe != null)
            {
                var audioManager = Audio.AudioManager.Instance;
                if (audioManager != null)
                {
                    audioManager.PlaySe(weapon.AttackSe);
                }
            }
        }
        private void PlayAttackAnimation(WeaponData weapon)
        {
            if (weapon == null || playerAnimator == null)
            {
                return;
            }
            var triggerName = weapon.AttackAnimationTrigger;
            if (string.IsNullOrEmpty(triggerName))
            {
                return;
            }
            var weaponAttackTime = weapon.AttackDuration + weapon.CooldownTime;
            const float attackAnimationDuration = 1.4f;
            var animationSpeed = attackAnimationDuration / Mathf.Max(0.1f, weaponAttackTime);
            playerAnimator.SetFloat("AttackSpeed", animationSpeed);
            playerAnimator.ResetTrigger(triggerName);
            playerAnimator.SetTrigger(triggerName);
            if (enableAttackDebugLog)
            {
                UnityEngine.Debug.Log($"[PlayerAttack] アニメトリガー発火: weapon={weapon.WeaponName}, trigger={triggerName}, speed={animationSpeed:F2}x (time={weaponAttackTime:F2}s)");
            }
        }
        private Quaternion ResolveAttackRotation()
        {
            var attackDirection = Vector3.zero;
            if (playerModelRotator != null && playerModelRotator.VisualModel != null)
            {
                attackDirection = playerModelRotator.VisualModel.forward;
            }
            if (attackDirection.sqrMagnitude < 0.0001f && playerMover != null)
            {
                attackDirection = playerMover.FacingDirection;
            }
            attackDirection = new Vector3(attackDirection.x, 0f, attackDirection.z);
            if (attackDirection.sqrMagnitude < 0.0001f)
            {
                attackDirection = transform.forward;
            }
            return Quaternion.LookRotation(attackDirection.normalized, Vector3.up);
        }
    }
}
