using UnityEngine;

namespace Game.Player.Domain
{
    /// <summary>
    /// モデルのバウンディングボックス情報を保持する値型。
    /// 全てのRendererを統合した境界を表す。
    /// </summary>
    public readonly struct ModelBounds
    {
        public readonly Vector3 Size;
        public readonly Vector3 Center;
        public readonly Vector3 Min;
        public readonly Vector3 Max;

        public ModelBounds(Vector3 size, Vector3 center, Vector3 min, Vector3 max)
        {
            Size = size;
            Center = center;
            Min = min;
            Max = max;
        }

        public bool IsValid => Size.x > 0 && Size.y > 0 && Size.z > 0;
    }

    /// <summary>
    /// スケール計算の結果を保持する値型。
    /// </summary>
    public readonly struct ScaleResult
    {
        public readonly Vector3 LocalScale;
        public readonly Vector3 PositionOffset;
        public readonly bool IsValid;
        public readonly string Message;

        private ScaleResult(Vector3 localScale, Vector3 positionOffset, bool isValid, string message)
        {
            LocalScale = localScale;
            PositionOffset = positionOffset;
            IsValid = isValid;
            Message = message;
        }

        public static ScaleResult Success(Vector3 scale, Vector3 offset)
        {
            return new ScaleResult(scale, offset, true, "スケール計算成功");
        }

        public static ScaleResult Failure(string message)
        {
            return new ScaleResult(Vector3.one, Vector3.zero, false, message);
        }
    }

    /// <summary>
    /// モデル置き換え処理の結果を保持する値型。
    /// </summary>
    public readonly struct ReplaceResult
    {
        public readonly bool Success;
        public readonly string Message;
        public readonly GameObject InstalledModel;

        private ReplaceResult(bool success, string message, GameObject installedModel)
        {
            Success = success;
            Message = message;
            InstalledModel = installedModel;
        }

        public static ReplaceResult SuccessResult(GameObject model)
        {
            return new ReplaceResult(true, "モデル置き換え成功", model);
        }

        public static ReplaceResult Failure(string message)
        {
            return new ReplaceResult(false, message, null);
        }
    }
}

