using System;
using UnityEngine.InputSystem;

namespace Game.Player.Script
{
 /// <summary>
    /// InputActionReference に対する拡張メソッド群。
    /// GameInputReader から InputActionReference を簡潔に操作するための補助処理。
    /// </summary>
    public static class InputActionReferenceExtensions
    {
        /// <summary>
        /// InputActionReference が参照する Action を有効化する。
        /// </summary>
        public static void Enable(this InputActionReference self)
        {
            self.action?.Enable();
        }

        /// <summary>
        /// InputActionReference が参照する Action を無効化する。
        /// </summary>
        public static void Disable(this InputActionReference self)
        {
            self.action?.Disable();
        }

        /// <summary>
        /// InputActionReference の performed イベントにコールバックを登録する。
        /// </summary>
        public static void SetPerformed(
            this InputActionReference self,
            Action<InputAction.CallbackContext> callback
        )
        {
            if (self.action == null) return;
            self.action.performed += callback;
        }

        /// <summary>
        /// InputActionReference の canceled イベントにコールバックを登録する。
        /// </summary>
        public static void SetCanceled(
            this InputActionReference self,
            Action<InputAction.CallbackContext> callback
        )
        {
            if (self.action == null) return;
            self.action.canceled += callback;
        }

        /// <summary>
        /// InputActionReference の performed イベントからコールバックを解除する。
        /// </summary>
        public static void RemovePerformed(
            this InputActionReference self,
            Action<InputAction.CallbackContext> callback
        )
        {
            if (self.action == null) return;
            self.action.performed -= callback;
        }

        /// <summary>
        /// InputActionReference の canceled イベントからコールバックを解除する。
        /// </summary>
        public static void RemoveCanceled(
            this InputActionReference self,
            Action<InputAction.CallbackContext> callback
        )
        {
            if (self.action == null) return;
            self.action.canceled -= callback;
        }
    }
}