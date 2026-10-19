using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.BuildMode
{
    /// <summary>
    /// ビルドモード専用の入力読み取りクラス。
    /// ビルドモード中の配置確定入力（左クリック）とマウス座標を管理する。
    /// GameInputReader から分離され、ビルドモード関連の責務のみを担当する。
    /// </summary>
    public class BuildInputReader : MonoBehaviour
    {
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

        /// <summary>
        /// 毎フレーム、マウスのスクリーン座標を更新する。
        /// 左クリックが押された瞬間に OnBuildPlace を通知する。
        /// </summary>
        private void Update()
        {
            if (Mouse.current == null)
            {
                return;
            }

            MouseScreenPosition = Mouse.current.position.ReadValue();

            // 左クリックが押された瞬間に OnBuildPlace を通知する
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                OnBuildPlace?.Invoke();
            }
        }
    }
}

