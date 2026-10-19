using System;
using System.Collections.Generic;

namespace DOD.HitBox
{
    /// <summary>
    /// HitBox データのルートモデル。
    /// ランタイム・エディター両方で使用する。
    /// </summary>
    [Serializable]
    public class HitBoxData
    {
        public float colliserSize = 1f;
        public List<ColliderData> colliders = new List<ColliderData>();
    }

    /// <summary>
    /// 1つのコライダーオブジェクトの座標を表す。
    /// 1マス=1ユニット基準。原点(0,0)はプレイヤー位置。
    /// x/y はコライダーサイズの半分を加算したオフセット済み値。
    /// </summary>
    [Serializable]
    public class ColliderData
    {
        public string name;
        public float x;
        public float y;

        // JsonUtility のデシリアライズ用
        public ColliderData() { }

        public ColliderData(string name, float x, float y)
        {
            this.name = name;
            this.x = x;
            this.y = y;
        }
    }
}
