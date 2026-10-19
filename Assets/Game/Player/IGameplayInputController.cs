// ReSharper disable once CheckNamespace
namespace Game.Player
{
    /// <summary>
    /// ゲームプレイ入力（移動・攻撃・インタラクトなど）を一時停止/再開するためのインターフェース。
    /// Pause 入力だけは受け付け続けたいケース（死亡演出中など）で使用する。
    /// </summary>
    public interface IGameplayInputController
    {
        /// <summary>
        /// Pause 以外のゲームプレイ入力を停止する。
        /// </summary>
        void DisableGameplayInputExceptPause();

        /// <summary>
        /// DisableGameplayInputExceptPause で停止した入力を再開する。
        /// </summary>
        void EnableGameplayInputExceptPause();
    }
}

