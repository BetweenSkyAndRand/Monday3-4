using UnityEngine;
using UnityEngine.InputSystem;

// ReSharper disable once CheckNamespace
namespace Game.BuildMode
{
    /// <summary>
    /// ビルドモード中のカメラ移動・ズームを担当するコンポーネント。
    /// 新 Input System（PlayerAction）でWASD移動、Mouse.currentでスクロールズームを行う。
    /// ファイル名: BuildCameraMove.cs と一致させるためクラス名を BuildCameraMove に統一する。
    /// </summary>
    public class BuildCameraMove : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 15f;
        [SerializeField] private float zoomSpeed = 200f;
        [SerializeField] private float minZoom = 10f;
        [SerializeField] private float maxZoom = 40f;

        private PlayerAction playerAction;
        private bool isWaitingForInputRelease;

        private void Awake()
        {
            playerAction = new PlayerAction();
        }

        private void OnEnable()
        {
            playerAction.Player.Enable();
            // ビルドモード突入時に押しっぱなしだった移動入力でカメラが滑らないよう、一度離すまで無視する
            isWaitingForInputRelease = true;
        }

        private void OnDisable()
        {
            playerAction.Player.Disable();
        }

        private void OnDestroy()
        {
            playerAction?.Dispose();
            playerAction = null;
        }

        private void Update()
        {
            MoveCamera();
            ZoomCamera();
        }

        private void MoveCamera()
        {
            Vector2 raw = playerAction.Player.Move.ReadValue<Vector2>();
            if (isWaitingForInputRelease)
            {
                if (raw.sqrMagnitude > 0.04f)
                {
                    return;
                }

                isWaitingForInputRelease = false;
            }

            Vector3 move = new Vector3(raw.x, 0f, raw.y);
            transform.position += move * (moveSpeed * Time.deltaTime);
        }

        private void ZoomCamera()
        {
            if (Mouse.current == null) return;

            float scroll = Mouse.current.scroll.ReadValue().y;
            Vector3 pos = transform.position;
            pos.y -= scroll * zoomSpeed * Time.deltaTime;
            pos.y = Mathf.Clamp(pos.y, minZoom, maxZoom);
            transform.position = pos;
        }
    }
}