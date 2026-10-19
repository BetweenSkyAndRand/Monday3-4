using System.Collections.Generic;
using UnityEngine;

namespace Game.Utility
{
    /// <summary>
    /// MonoBehaviour を指定インターフェース型にキャストする共通ユーティリティ。
    /// Unity が interface を SerializeField でサポートしないため、
    /// 実行時にキャスト検証を行う処理を一箇所に集約する。
    ///
    /// SearchController など複数クラスで同じキャストパターンが必要になるため
    /// 共通化し、呼び出し元クラスの責務を純粋なロジックに絞る。
    /// </summary>
    public static class MonoBehaviourCaster
    {
        /// <summary>
        /// 単一の MonoBehaviour を指定インターフェース型にキャストする。
        /// キャスト失敗時はエラーログを出して null を返す。
        /// </summary>
        /// <param name="source">キャスト元の MonoBehaviour。</param>
        /// <param name="callerName">ログ出力用の呼び出し元クラス名。</param>
        public static T CastSingle<T>(MonoBehaviour source, string callerName) where T : class
        {
            if (source == null)
            {
                return null;
            }

            var casted = source as T;
            if (casted == null)
            {
                UnityEngine.Debug.LogError($"[{callerName}] {source.name} は {typeof(T).Name} を実装していません。");
            }

            return casted;
        }

        /// <summary>
        /// MonoBehaviour 配列を指定インターフェース型にキャストする。
        /// キャスト失敗した要素はスキップしてエラーログを出す。
        /// </summary>
        /// <param name="sourceObjects">キャスト元の MonoBehaviour 配列。</param>
        /// <param name="callerName">ログ出力用の呼び出し元クラス名。</param>
        public static T[] CastArray<T>(MonoBehaviour[] sourceObjects, string callerName) where T : class
        {
            if (sourceObjects == null)
            {
                return System.Array.Empty<T>();
            }

            var result = new List<T>();
            foreach (var sourceObject in sourceObjects)
            {
                if (sourceObject == null)
                {
                    continue;
                }

                var casted = sourceObject as T;
                if (casted == null)
                {
                    UnityEngine.Debug.LogError($"[{callerName}] {sourceObject.name} は {typeof(T).Name} を実装していません。");
                    continue;
                }

                result.Add(casted);
            }

            return result.ToArray();
        }
    }
}

