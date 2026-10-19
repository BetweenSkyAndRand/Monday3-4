namespace Game.Search
{
    /// <summary>
    /// 探索開始時に「スポーンしてよい」という合図を受け取るインターフェース。
    /// 部屋数の取得などの実装詳細は、このインターフェースを実装するクラスが自ら担う。
    /// 責務：「スポーン開始命令を伝達する」の 1 点のみ。
    /// </summary>
    public interface ISpawnInstructionHandler
    {
        void OnSpawnInstruction();
    }
}