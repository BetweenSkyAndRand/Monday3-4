namespace Game.GroundItem
{
    /// <summary>敵ドロップ・投棄・死亡時ドロップから GroundItemSpawnController を参照するための入口。</summary>
    public static class GroundItemRuntime
    {
        private static GroundItemSpawnController activeController;

        /// <summary>プレイヤー死亡時にアイテムを地面へ落とすか。初期値は GroundItemParameter で、ポーズ画面から切り替える。</summary>
        public static bool DropItemsOnDeath { get; set; } = true;

        public static void Register(GroundItemSpawnController controller)
        {
            activeController = controller;
        }

        public static void Unregister(GroundItemSpawnController controller)
        {
            if (activeController == controller)
            {
                activeController = null;
            }
        }

        public static bool TryPickup(string instanceId)
        {
            return TryGetController(out var controller) && controller.TryPickup(instanceId);
        }

        public static bool TryGetController(out GroundItemSpawnController controller)
        {
            controller = activeController;
            return controller != null;
        }
    }
}