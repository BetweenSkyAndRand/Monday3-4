using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player.Script
{
    /// <summary>
    /// プレイヤー入力を集約し、移動入力はプロパティ、操作入力はイベントで外部へ通知するクラス。
    /// GridMover や PlayerInteract はこのクラスを参照して入力を受け取る。
    /// </summary>
    public class GameInputReader : MonoBehaviour, Game.Player.IGameplayInputController
    {
        [Header("入力設定")]
        [SerializeField] private InputActionReference moveActionReference;
        [SerializeField] private InputActionReference interactActionReference;

        /// <summary>攻撃ボタンの InputActionReference。PlayerAction.inputactions に攻撃アクションを追加してアサインする。</summary>
        [SerializeField] private InputActionReference attackActionReference;

        /// <summary>ポーズボタン（ESCキー等）の InputActionReference。</summary>
        [SerializeField] private InputActionReference pauseActionReference;

        /// <summary>インベントリ開閉ボタン（Tab等）の InputActionReference。</summary>
        [SerializeField] private InputActionReference inventoryToggleActionReference;

        /// <summary>アイテム投棄ボタンの InputActionReference。PlayerAction.inputactions の Drop をアサインする。</summary>
        [SerializeField] private InputActionReference dropActionReference;

        /// <summary>装備スロット1〜4（1,2,3,4キー）の InputActionReference。</summary>
        [SerializeField] private InputActionReference weaponEquip1ActionReference;
        [SerializeField] private InputActionReference weaponEquip2ActionReference;
        [SerializeField] private InputActionReference weaponEquip3ActionReference;
        [SerializeField] private InputActionReference weaponEquip4ActionReference;

        /// <summary>
        /// 現在の移動入力値。
        /// GridMover が毎フレーム参照して移動方向に変換する。
        /// </summary>
        public Vector2 MoveInput { get; private set; }

        /// <summary>
        /// インタラクト入力が発生したときに通知するイベント。
        /// PlayerInteract が購読して、対象の Interact() を呼ぶ。
        /// </summary>
        public event Action OnInteract;

        /// <summary>
        /// 攻撃入力が発生したときに通知するイベント。
        /// PlayerAttack が購読して、攻撃処理を開始する。
        /// </summary>
        public event Action OnAttack;

        /// <summary>
        /// ポーズ入力が発生したときに通知するイベント。
        /// PauseInputHandler が購読してポーズメニューを開く。
        /// </summary>
        public event Action OnPause;

        /// <summary>
        /// インベントリ開閉入力が発生したときに通知するイベント。
        /// InventoryController が購読して表示/非表示を切り替える。
        /// </summary>
        public event Action OnInventoryToggle;

        /// <summary>
        /// アイテム投棄入力が発生したときに通知するイベント。
        /// </summary>
        public event Action OnDrop;

        /// <summary>マウスホイールでインタラクト対象を切り替える入力。引数は +1（次）/ -1（前）。</summary>
        public event Action<int> OnInteractTargetScroll;

        /// <summary>
        /// 装備スロット選択入力が発生したときに通知するイベント。
        /// 引数は装備スロット番号（1〜4）。InventoryController が購読してスロットを直接装備する。
        /// </summary>
        public event Action<int> OnEquipSlot;

        /// <summary>
        /// ビルドモード中の配置確定入力（左クリック）が発生したときに通知するイベント。
        /// DungeonController が購読して、部屋の配置処理を開始する。
        /// </summary>
        public event Action OnBuildPlace;

        /// <summary>
        /// 現在のマウスのスクリーン座標。
        /// DungeonController がビルドモード中にプレビュー位置を更新するために毎フレーム参照する。
        /// </summary>
        public Vector2 MouseScreenPosition { get; private set; }

        private bool isGameplayInputEnabled = true;


        /// <summary>
        /// 各 InputActionReference にコールバックを登録する。
        /// performed で入力を受け、canceled で入力解除時にゼロへ戻す。
        /// </summary>
        private void Awake()
        {
            moveActionReference.SetPerformed(Handle);
            moveActionReference.SetCanceled(OnMoveCanceled);

            interactActionReference.SetPerformed(OnInteractPerformed);

            // attackActionReference は未アサインの場合があるため null チェックしてから登録する
            if (attackActionReference != null)
            {
                attackActionReference.SetPerformed(OnAttackPerformed);
            }

            // pauseActionReference は未アサインの場合があるため null チェックしてから登録する
            if (pauseActionReference != null)
            {
                pauseActionReference.SetPerformed(OnPausePerformed);
            }

            if (inventoryToggleActionReference != null)
            {
                inventoryToggleActionReference.SetPerformed(OnInventoryTogglePerformed);
            }

            if (dropActionReference != null)
            {
                dropActionReference.SetPerformed(OnDropPerformed);
            }

            // WeaponEquip1〜4 は未アサインの場合があるため null チェックしてから登録する
            if (weaponEquip1ActionReference != null)
            {
                weaponEquip1ActionReference.SetPerformed(_ => OnEquipSlot?.Invoke(1));
            }

            if (weaponEquip2ActionReference != null)
            {
                weaponEquip2ActionReference.SetPerformed(_ => OnEquipSlot?.Invoke(2));
            }

            if (weaponEquip3ActionReference != null)
            {
                weaponEquip3ActionReference.SetPerformed(_ => OnEquipSlot?.Invoke(3));
            }

            if (weaponEquip4ActionReference != null)
            {
                weaponEquip4ActionReference.SetPerformed(_ => OnEquipSlot?.Invoke(4));
            }
        }

        /// <summary>
        /// マウス座標の更新とインベントリトグルのフォールバック入力を処理する。
        /// inventoryToggleActionReference が未アサインの場合のみ Tab キーで代替する。
        /// </summary>
        private void Update()
        {
            // マウス座標を更新（ビルドモードで使用）
            if (Mouse.current != null)
            {
                MouseScreenPosition = Mouse.current.position.ReadValue();

                // 左クリックが押された瞬間に OnBuildPlace を通知する
                if (isGameplayInputEnabled && Mouse.current.leftButton.wasPressedThisFrame)
                {
                    OnBuildPlace?.Invoke();
                }
            }

            if (isGameplayInputEnabled && Mouse.current != null)
            {
                var scrollY = Mouse.current.scroll.ReadValue().y;
                if (scrollY > 0.01f)
                {
                    OnInteractTargetScroll?.Invoke(-1);
                }
                else if (scrollY < -0.01f)
                {
                    OnInteractTargetScroll?.Invoke(1);
                }
            }

            if (isGameplayInputEnabled && inventoryToggleActionReference == null && Keyboard.current != null)
            {
                if (Keyboard.current.tabKey.wasPressedThisFrame)
                {
                    OnInventoryToggle?.Invoke();
                }
            }
        }

        /// <summary>
        /// オブジェクト破棄時にイベント購読を解除する。
        /// </summary>
        private void OnDestroy()
        {
            moveActionReference.RemovePerformed(Handle);
            moveActionReference.RemoveCanceled(OnMoveCanceled);

            interactActionReference.RemovePerformed(OnInteractPerformed);

            if (attackActionReference != null)
            {
                attackActionReference.RemovePerformed(OnAttackPerformed);
            }

            if (pauseActionReference != null)
            {
                pauseActionReference.RemovePerformed(OnPausePerformed);
            }

            if (inventoryToggleActionReference != null)
            {
                inventoryToggleActionReference.RemovePerformed(OnInventoryTogglePerformed);
            }

            if (dropActionReference != null)
            {
                dropActionReference.RemovePerformed(OnDropPerformed);
            }

            // WeaponEquip1〜4 のコールバックはラムダのため RemovePerformed は不要（アセット破棄時に自動解除される）
        }

        /// <summary>
        /// GameObject が有効になったとき、入力受付を開始する。
        /// </summary>
        private void OnEnable()
        {
            ApplyGameplayInputActionState();
            if (pauseActionReference != null)
            {
                pauseActionReference.Enable();
            }
        }

        /// <summary>
        /// GameObject が無効になったとき、入力受付を停止する。
        /// </summary>
        private void OnDisable()
        {
            SetActionEnabled(moveActionReference, false);
            SetActionEnabled(interactActionReference, false);
            SetActionEnabled(attackActionReference, false);

            if (pauseActionReference != null)
            {
                pauseActionReference.Disable();
            }

            SetActionEnabled(inventoryToggleActionReference, false);
            SetActionEnabled(dropActionReference, false);
            SetActionEnabled(weaponEquip1ActionReference, false);
            SetActionEnabled(weaponEquip2ActionReference, false);
            SetActionEnabled(weaponEquip3ActionReference, false);
            SetActionEnabled(weaponEquip4ActionReference, false);
        }

        /// <summary>
        /// 移動入力が発生したときに呼ばれる。
        /// 入力値をそのまま MoveInput に格納する。
        /// </summary>
        private void Handle(InputAction.CallbackContext context)
        {
            MoveInput = context.ReadValue<Vector2>();
        }

        /// <summary>
        /// 移動入力が離されたときに呼ばれる。
        /// MoveInput をゼロに戻し、GridMover が停止できるようにする。
        /// </summary>
        private void OnMoveCanceled(InputAction.CallbackContext _)
        {
            MoveInput = Vector2.zero;
        }

        /// <summary>
        /// インタラクト入力が発生したときに呼ばれる。
        /// Input System の詳細は外に出さず、OnInteract イベントだけを通知する。
        /// </summary>
        private void OnInteractPerformed(InputAction.CallbackContext _)
        {
            if (!isGameplayInputEnabled)
            {
                return;
            }

            OnInteract?.Invoke();
        }

        /// <summary>
        /// 攻撃入力が発生したときに呼ばれる。
        /// Input System の詳細は外に出さず、OnAttack イベントだけを通知する。
        /// </summary>
        private void OnAttackPerformed(InputAction.CallbackContext _)
        {
            if (!isGameplayInputEnabled)
            {
                return;
            }

            OnAttack?.Invoke();
        }

        /// <summary>
        /// ポーズ入力が発生したときに呼ばれる。
        /// Input System の詳細は外に出さず、OnPause イベントだけを通知する。
        /// </summary>
        private void OnPausePerformed(InputAction.CallbackContext _)
        {
            // UnityEngine.Debug.Log("[GameInputReader] ポーズキーが押されました - OnPause イベントを発火");
            OnPause?.Invoke();
        }

        private void OnInventoryTogglePerformed(InputAction.CallbackContext _)
        {
            if (!isGameplayInputEnabled)
            {
                return;
            }

            OnInventoryToggle?.Invoke();
        }

        private void OnDropPerformed(InputAction.CallbackContext _)
        {
            if (!isGameplayInputEnabled)
            {
                return;
            }

            OnDrop?.Invoke();
        }

        public void DisableGameplayInputExceptPause()
        {
            isGameplayInputEnabled = false;
            MoveInput = Vector2.zero;
            ApplyGameplayInputActionState();
        }

        public void EnableGameplayInputExceptPause()
        {
            isGameplayInputEnabled = true;
            ApplyGameplayInputActionState();
        }

        private void ApplyGameplayInputActionState()
        {
            SetActionEnabled(moveActionReference, isGameplayInputEnabled);
            SetActionEnabled(interactActionReference, isGameplayInputEnabled);
            SetActionEnabled(attackActionReference, isGameplayInputEnabled);
            SetActionEnabled(inventoryToggleActionReference, isGameplayInputEnabled);
            SetActionEnabled(dropActionReference, isGameplayInputEnabled);
            SetActionEnabled(weaponEquip1ActionReference, isGameplayInputEnabled);
            SetActionEnabled(weaponEquip2ActionReference, isGameplayInputEnabled);
            SetActionEnabled(weaponEquip3ActionReference, isGameplayInputEnabled);
            SetActionEnabled(weaponEquip4ActionReference, isGameplayInputEnabled);
        }

        private void SetActionEnabled(InputActionReference actionReference, bool shouldEnable)
        {
            if (actionReference == null)
            {
                return;
            }

            if (shouldEnable)
            {
                actionReference.Enable();
                return;
            }

            actionReference.Disable();
        }
    }
}
