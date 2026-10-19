namespace Game.Revival.Domain
{
    /// <summary>
    /// 復活フロー中の状態遷移を管理する小さなステートマシン。
    /// </summary>
    public class PlayerLifeStateMachine
    {
        public PlayerLifeState CurrentState { get; private set; } = PlayerLifeState.Alive;

        public bool TryEnterDeathProcessing()
        {
            if (CurrentState != PlayerLifeState.Alive)
            {
                return false;
            }

            CurrentState = PlayerLifeState.DeathProcessing;
            return true;
        }

        public bool TryEnterRespawning()
        {
            if (CurrentState != PlayerLifeState.DeathProcessing)
            {
                return false;
            }

            CurrentState = PlayerLifeState.Respawning;
            return true;
        }

        public void EnterAlive()
        {
            CurrentState = PlayerLifeState.Alive;
        }

        public void EnterGameOver()
        {
            CurrentState = PlayerLifeState.GameOver;
        }
    }
}

