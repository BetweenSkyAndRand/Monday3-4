using System;
using System.Collections.Generic;

namespace Game.Enemy
{
    public static class EnemyVirusEntityFactory
    {
        public static Dictionary<string, EnemyVirusEntity> CreateDictionary(EnemyVirusMasterDataList masterDataList)
        {
            if (masterDataList == null)
            {
                throw new ArgumentNullException(nameof(masterDataList));
            }

            var list = masterDataList.EnemyVirusMasterDataListValue;
            if (list == null)
            {
                throw new ArgumentNullException(nameof(masterDataList), "enemyVirusMasterDataList is null.");
            }

            if (list.Count == 0)
            {
                throw new InvalidOperationException("enemyVirusMasterDataList is empty.");
            }

            var result = new Dictionary<string, EnemyVirusEntity>();
            foreach (var masterData in list)
            {
                if (masterData == null)
                {
                    throw new ArgumentNullException(nameof(masterDataList), "masterData includes null.");
                }

                var uid = masterData.Uid?.Trim();
                if (string.IsNullOrEmpty(uid))
                {
                    throw new ArgumentException("uid is null or empty.", nameof(masterDataList));
                }

                if (result.ContainsKey(uid))
                {
                    throw new InvalidOperationException($"Duplicated uid: {uid}");
                }

                var entity = Create(masterData);
                result.Add(uid, entity);
            }

            return result;
        }

        public static EnemyVirusEntity Create(EnemyVirusMasterData masterData)
        {
            if (masterData == null)
            {
                throw new ArgumentNullException(nameof(masterData));
            }

            var uid = masterData.Uid?.Trim();
            if (string.IsNullOrEmpty(uid))
            {
                throw new ArgumentException("uid is null or empty.", nameof(masterData));
            }

            var paramMasterData = masterData.VirusParamMasterData
                ?? throw new ArgumentNullException(nameof(masterData), "VirusParamMasterData is null.");
            var actionPatternMasterData = masterData.VirusActionPatternMasterData
                ?? throw new ArgumentNullException(nameof(masterData), "VirusActionPatternMasterData is null.");
            var weaponMasterData = paramMasterData.WeaponMasterData
                ?? throw new ArgumentNullException(nameof(masterData), "WeaponMasterData is null.");

            var sourceActionPatterns = actionPatternMasterData.ActionPatterns
                ?? throw new ArgumentNullException(nameof(masterData), "ActionPatterns is null.");
            var copiedActionPatterns = new List<EnemyVirusStateTransitionType>(sourceActionPatterns);

            var weaponEntity = new WeaponEntity(weaponMasterData.AttackPower, weaponMasterData.AttackCoolTime, weaponMasterData.AttackRangeData);
            var actionPatternEntity = new EnemyVirusActionPatternEntity(
                actionPatternMasterData.InitialState,
                actionPatternMasterData.StateTransitionCoolTime,
                copiedActionPatterns);

            return new EnemyVirusEntity(
                uid,
                paramMasterData.EnemyVirusType,
                paramMasterData.Hp,
                paramMasterData.BaseAttackPower,

                paramMasterData.ViewRangeData,
                weaponEntity,
                actionPatternEntity);
        }
    }
}

