using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// プレイヤーモデル置き換え時の設定を管理するScriptableObject。
    /// ターゲットサイズやスケール設定をアセットとして保存可能。
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerModelConfig", menuName = "Game/Player/Player Model Config")]
    public class PlayerModelConfig : ScriptableObject
    {
        [Header("ターゲットサイズ")]
        [Tooltip("目標幅（X軸）- 通常1.0でセル1個分")]
        [SerializeField] private float targetWidth = 1.0f;
        
        [Tooltip("目標奥行（Z軸）- 通常1.0でセル1個分")]
        [SerializeField] private float targetDepth = 1.0f;
        
        [Tooltip("目標高さ（Y軸）- 通常2.0でセル2個分")]
        [SerializeField] private float targetHeight = 2.0f;
        
        [Header("スケール設定")]
        [Tooltip("アスペクト比を維持（推奨: ON）- OFFにすると形が歪む可能性があります")]
        [SerializeField] private bool preserveAspectRatio = true;
        
        [Tooltip("モデルの底面を地面（Y=0）に合わせる")]
        [SerializeField] private bool alignToGround = true;
        
        [Header("マテリアル設定")]
        [Tooltip("マテリアルを自動適用")]
        [SerializeField] private bool autoApplyMaterials = true;
        
        [Tooltip("マテリアル検索フォルダ")]
        [SerializeField] private string[] materialFolders = new[]
        {
            "Assets/FreeAnimatedSpaceMan/Materials",
            "Assets/Material"
        };
        
        public float TargetWidth => targetWidth;
        public float TargetDepth => targetDepth;
        public float TargetHeight => targetHeight;
        public bool PreserveAspectRatio => preserveAspectRatio;
        public bool AlignToGround => alignToGround;
        public bool AutoApplyMaterials => autoApplyMaterials;
        public string[] MaterialFolders => materialFolders;
        
        public Vector3 TargetSize => new Vector3(targetWidth, targetHeight, targetDepth);
    }
}

