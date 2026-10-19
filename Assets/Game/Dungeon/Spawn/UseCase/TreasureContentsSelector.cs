using Game.Treasure;
namespace Game.Dungeon.Spawn.UseCase
{
    /// <summary>
    /// 宝箱の中身を万分率（ratePer10000）テーブルから抽選するロジック。
    /// 責務: 抽選ロジックのみ（Instantiate や設定取得は行わない）。
    /// </summary>
    public class TreasureContentsSelector
    {
        /// <summary>
        /// treasureItemWeights の万分率テーブル（ratePer10000）を参照して宝箱の中身を決定する。
        /// ratePer10000 が 0 以下のエントリは除外して計算する。
        /// テーブルが空・全エントリが無効・合計が 10000 未満の場合は、中身が空の宝箱を返す。
        /// （宝箱の中身になるアイテムは今後追加予定のため、現状は武器のみを抽選対象とする）
        /// </summary>
        /// <param name="treasureItemWeights">万分率テーブル</param>
        /// <param name="random">乱数生成器（テスト容易性のため外部注入）</param>
        /// <returns>抽選結果の TreasureContents</returns>
        public TreasureContents Select(TreasureItemWeight[] treasureItemWeights, System.Random random)
        {
            if (treasureItemWeights == null || treasureItemWeights.Length == 0)
            {
                return CreateEmptyContents();
            }
            // 有効なエントリの万分率合計を集計する
            var totalRatePer10000 = 0;
            foreach (var item in treasureItemWeights)
            {
                if (item.ratePer10000 > 0)
                {
                    totalRatePer10000 += item.ratePer10000;
                }
            }
            if (totalRatePer10000 <= 0)
            {
                // 有効なエントリが 1 つもない場合のフォールバック
                return CreateEmptyContents();
            }
            if (totalRatePer10000 > 10000)
            {
                UnityEngine.Debug.LogWarning($"[TreasureContentsSelector] treasureItemWeights の ratePer10000 合計が 10000 を超えています（{totalRatePer10000}）。10000 までを有効範囲として抽選します。");
            }
            // 万分率抽選（0 ～ 9999）で選択する
            var roll = random.Next(0, 10000);
            var accumulated = 0;
            foreach (var item in treasureItemWeights)
            {
                if (item.ratePer10000 <= 0)
                {
                    continue;
                }
                accumulated += item.ratePer10000;
                if (roll < accumulated)
                {
                    return new TreasureContents(item.weaponData);
                }
                // 合計が 10000 を超える設定時は、超過分を抽選対象に含めない。
                if (accumulated >= 10000)
                {
                    break;
                }
            }
            // 合計が 10000 未満だった場合や無効データ時のフォールバック
            return CreateEmptyContents();
        }
        /// <summary>中身が空の宝箱用コンテンツ。WeaponData が null のため宝箱は空になる。</summary>
        private static TreasureContents CreateEmptyContents()
        {
            return new TreasureContents(null);
        }
    }
}