using UnityEngine;

namespace Game.Enemy.View
{
    /// <summary>
    /// HPバーのCanvasを常にメインカメラへ向け続けるBillboardコンポーネント。
    /// EnemyModelRotator が VisualModel を回転させても HPバーが連動しないようにする。
    /// </summary>
    public class EnemyHpBarBillboard : MonoBehaviour
    {
        private Camera mainCamera;

        private void Awake()
        {
            mainCamera = Camera.main;
        }

        private void LateUpdate()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                return;
            }

            transform.rotation = mainCamera.transform.rotation;
        }
    }
}
