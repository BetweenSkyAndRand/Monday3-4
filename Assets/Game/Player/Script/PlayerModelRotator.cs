using Game.Player;
using Game.Player.Parameter;
using Game.Utility;
using UnityEngine;
namespace Game.Player.Script
{
    /// <summary>
    /// プレイヤーのビジュアルモデルを移動方向に向けて回転させるコンポーネント。
    /// IPlayerMover の向きを監視し、VisualModel を滑らかに回転させる。
    /// </summary>
    public class PlayerModelRotator : MonoBehaviour
    {
        [Header("参照")]
        [Tooltip("回転対象のモデル（通常は子オブジェクトの VisualModel）")]
        [SerializeField] private Transform visualModel;
        [Tooltip("移動方向を取得する IPlayerMover 実装。未設定時は PlayerFreeMover → RogueMover の順で自動検索する。")]
        [SerializeField] private MonoBehaviour playerMoverObject;
        [Tooltip("回転設定を保持する ScriptableObject。")]
        [SerializeField] private PlayerFreeMoveParameter playerFreeMoveParameter;
        [Header("回転設定")]
        [Tooltip("回転を滑らかに補間するか")]
        [SerializeField] private bool smoothRotation = true;
        private IPlayerMover playerMover;
        private void Awake()
        {
            if (playerMoverObject == null)
            {
                var playerFreeMover = GetComponent<PlayerFreeMover>();
                if (playerFreeMover != null)
                {
                    playerMoverObject = playerFreeMover;
                }
                else
                {
                    var rogueMover = GetComponentInParent<RogueMover>();
                    if (rogueMover != null)
                    {
                        playerMoverObject = rogueMover;
                    }
                }
            }
            playerMover = MonoBehaviourCaster.CastSingle<IPlayerMover>(playerMoverObject, nameof(PlayerModelRotator));
            if (visualModel == null)
            {
                var visualModelTransform = transform.Find("VisualModel");
                if (visualModelTransform != null)
                {
                    visualModel = visualModelTransform;
                }
                else
                {
                    visualModel = transform;
                    UnityEngine.Debug.LogWarning("[PlayerModelRotator] VisualModelが見つかりません。Player本体を回転させます。", this);
                }
            }
        }
        private void LateUpdate()
        {
            if (playerMover == null || visualModel == null || playerFreeMoveParameter == null)
            {
                UnityEngine.Debug.LogWarning(
                    $"[PlayerModelRotator] Skip: moverNull={playerMover == null}, visualNull={visualModel == null}, paramNull={playerFreeMoveParameter == null}",
                    this);
                return;
            }
            var facingDirection = playerMover.FacingDirection;
            facingDirection.y = 0f;
            if (facingDirection.sqrMagnitude < playerFreeMoveParameter.MoveThreshold * playerFreeMoveParameter.MoveThreshold)
            {
                return;
            }
            var targetRotation = Quaternion.LookRotation(facingDirection.normalized, Vector3.up);
            /*UnityEngine.Debug.Log(
                $"[PlayerModelRotator] Rotate. facing={facingDirection.normalized}, currentY={visualModel.rotation.eulerAngles.y:F1}, targetY={targetRotation.eulerAngles.y:F1}",
                this);*/
            if (smoothRotation)
            {
                visualModel.rotation = Quaternion.Slerp(
                    visualModel.rotation,
                    targetRotation,
                    playerFreeMoveParameter.RotationSpeed * Time.deltaTime);
            }
            else
            {
                visualModel.rotation = targetRotation;
            }
        }
        /// <summary>
        /// 目標回転角度を強制設定する（外部から呼び出し可能）。
        /// </summary>
        public void SetRotation(float angle)
        {
            if (visualModel == null)
            {
                return;
            }
            var euler = visualModel.rotation.eulerAngles;
            visualModel.rotation = Quaternion.Euler(euler.x, angle, euler.z);
        }
        /// <summary>
        /// 現在の回転角度を取得する。
        /// </summary>
        public float CurrentRotationY => visualModel != null ? visualModel.rotation.eulerAngles.y : 0f;
        /// <summary>
        /// 回転対象の VisualModel Transform を返す。
        /// HitBoxController などから HitBox の親として利用する。
        /// </summary>
        public Transform VisualModel => visualModel;
    }
}
