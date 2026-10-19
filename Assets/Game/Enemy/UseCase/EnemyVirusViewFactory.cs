using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Game.Enemy
{
    public static class EnemyVirusViewFactory
    {
        private static readonly Regex UidRegex = new("^[A-Za-z0-9_]+$", RegexOptions.Compiled);

        public static Dictionary<string, EnemyVirusViewData> CreateDictionary(EnemyVirusViewDataList viewDataList)
        {
            if (viewDataList == null)
            {
                throw new ArgumentNullException(nameof(viewDataList));
            }

            var list = viewDataList.ViewDataList;
            if (list == null)
            {
                throw new ArgumentNullException(nameof(viewDataList), "viewDataList is null.");
            }

            if (list.Count == 0)
            {
                throw new InvalidOperationException("viewDataList is empty.");
            }

            var result = new Dictionary<string, EnemyVirusViewData>();
            foreach (var viewData in list)
            {
                if (viewData == null)
                {
                    throw new ArgumentNullException(nameof(viewDataList), "viewData includes null.");
                }

                var uid = viewData.Uid?.Trim();
                if (string.IsNullOrEmpty(uid))
                {
                    throw new ArgumentException("uid is null or empty.", nameof(viewDataList));
                }

                if (!UidRegex.IsMatch(uid))
                {
                    throw new ArgumentException($"uid contains invalid characters: {uid}", nameof(viewDataList));
                }

                if (result.ContainsKey(uid))
                {
                    throw new InvalidOperationException($"Duplicated uid: {uid}");
                }

                result.Add(uid, viewData);
            }

            return result;
        }
    }
}

