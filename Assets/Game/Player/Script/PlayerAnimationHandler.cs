using Game.Player;
using Game.Utility;
using UnityEngine;
namespace Game.Player.Script
{
    /// <summary>
    /// プレイヤーモデルのアニメーション（移動・被弾・死亡）を管理するコンポーネント。
    /// 攻撃アニメーションは PlayerAttack が WeaponData.AttackAnimationTrigger を通じて直接制御するため、
    /// このクラスは移動・被弾・死亡を担当する。
    /// </summary>
    public class PlayerAnimationHandler : MonoBehaviour
    {
        public const string TriggerDamage = "Damage";
        public const string TriggerDeath = "Death";
        public const string ParamIsMoving = "IsMoving";
        private static readonly int hashDamage = Animator.StringToHash(TriggerDamage);
        private static readonly int hashDeath = Animator.StringToHash(TriggerDeath);
        private static readonly int hashIsMoving = Animator.StringToHash(ParamIsMoving);
        [Header("参照")]
        [Tooltip("プレイヤーモデルの Animator。未設定の場合は子オブジェクトから自動検索する。")]
        [SerializeField] private Animator playerAnimator;
        [Tooltip("移動状態を取得する IPlayerMover 実装。未設定の場合は PlayerFreeMover → RogueMover の順で自動検索する。")]
        [SerializeField] private MonoBehaviour playerMoverObject;
        private IPlayerMover playerMover;
        private void Awake()
        {
            if (playerAnimator == null)
            {
                playerAnimator = GetComponentInChildren<Animator>();
            }
            if (playerMoverObject == null)
            {
                var playerFreeMover = GetComponentInParent<PlayerFreeMover>();
                if (playerFreeMover != null)
                {
                    playerMoverObject = playerFreeMover;
                }
                else
                {
                    playerMoverObject = GetComponentInParent<RogueMover>();
                }
            }
            playerMover = MonoBehaviourCaster.CastSingle<IPlayerMover>(playerMoverObject, nameof(PlayerAnimationHandler));
        }
        private void Update()
        {
            if (playerAnimator == null || playerMover == null)
            {
                return;
            }
            playerAnimator.SetBool(hashIsMoving, playerMover.IsMoving);
        }
        public void PlayDamageAnimation()
        {
            if (playerAnimator == null)
            {
                return;
            }
            playerAnimator.ResetTrigger(hashDamage);
            playerAnimator.SetTrigger(hashDamage);
        }
        public void PlayDeathAnimation()
        {
            if (playerAnimator == null)
            {
                return;
            }
            playerAnimator.ResetTrigger(hashDeath);
            playerAnimator.SetTrigger(hashDeath);
        }
    }
}
