using System.IO;
using UnityEngine;

namespace Game.Dungeon.Spawn
{
    /// <summary>
    /// room_spawn.json を読み込み、SpawnConfigData を返す Repository。
    /// 責務: JSON ファイル読み込みのみ（ロジックなし）。
    /// </summary>
    public class SpawnConfigRepository
    {
        private const string JsonRelativePath = "MasterData/room_spawn.json";

        /// <summary>
        /// room_spawn.json を読み込んで SpawnConfigData を返す。
        /// ファイルが存在しない、またはパースに失敗した場合は null を返す。
        /// </summary>
        public SpawnConfigData Load()
        {
            var filePath = Path.Combine(Application.streamingAssetsPath, JsonRelativePath);

            if (!File.Exists(filePath))
            {
                UnityEngine.Debug.LogError($"[SpawnConfigRepository] room_spawn.json が見つかりません: {filePath}");
                return null;
            }

            var json = File.ReadAllText(filePath);
            var config = JsonUtility.FromJson<SpawnConfigData>(json);

            if (config == null)
            {
                UnityEngine.Debug.LogError("[SpawnConfigRepository] room_spawn.json のパースに失敗しました。");
            }

            return config;
        }
    }
}

