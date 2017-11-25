using RimWorld;
using Verse;

namespace Zipangu {
    /// <summary>
    /// Plant_ExtendのXML設定
    /// </summary>
    public class ThingDef_Plant_Extend : ThingDef {
        /// <summary>
        /// 最大葉落ち温度。この温度以下になると葉落ちする可能性があります。
        /// </summary>
        public float MaxLeaflessTemperature;

        /// <summary>
        /// 最小葉落ち温度。この温度以下になると確実に葉落ちします。
        /// </summary>
        public float MinLeaflessTemperature;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public ThingDef_Plant_Extend() {
            this.MaxLeaflessTemperature = Plant.MaxLeaflessTemperature;
            this.MinLeaflessTemperature = -10f;
         }
    }

    /// <summary>
    /// Plantを拡張します
    /// </summary>
    public class Plant_Extend : Plant {
        protected override float LeaflessTemperatureThresh {
            get {
                var d = this.def as ThingDef_Plant_Extend;
                if (d != null) {
                    float num = d.MaxLeaflessTemperature - d.MinLeaflessTemperature;
                    return (float)this.HashOffset() * 0.01f % num - num + d.MaxLeaflessTemperature;
                }
                else {
                    return base.LeaflessTemperatureThresh;
                }
            }
        }
    }
}
