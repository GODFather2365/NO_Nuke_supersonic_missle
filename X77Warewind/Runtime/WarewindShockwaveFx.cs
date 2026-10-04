using System.Reflection;
using UnityEngine;

namespace Warewind
{
    /// <summary>
    /// Scales Shockwave FX components on cloned air-effect GameObjects for the 500 kt blast.
    /// Uses reflection for private game fields; original TBM templates stay untouched.
    /// </summary>
    internal static class WarewindShockwaveFx
    {
        private const BindingFlags Inst = BindingFlags.Instance | BindingFlags.NonPublic;

        private static readonly FieldInfo? MaxRadiusField =
            typeof(Shockwave).GetField("maxRadius", Inst);
        private static readonly FieldInfo? RadiusField =
            typeof(Shockwave).GetField("radius", Inst);
        private static readonly FieldInfo? LifetimeField =
            typeof(Shockwave).GetField("lifetime", Inst) ??
            typeof(Shockwave).GetField("duration", Inst);
        private static readonly FieldInfo? HeightField =
            typeof(Shockwave).GetField("height", Inst) ??
            typeof(Shockwave).GetField("maxHeight", Inst);

        internal static void Apply(GameObject? airEffect, float yieldKg)
        {
            if (airEffect == null || yieldKg <= WarewindConstants.NukeYieldThresholdKg)
                return;

            Shockwave[] waves = airEffect.GetComponentsInChildren<Shockwave>(true);
            for (int i = 0; i < waves.Length; i++)
                ScaleOne(waves[i]);
        }

        private static void ScaleOne(Shockwave? w)
        {
            if (w == null)
                return;

            float baseR = ReadFloat(MaxRadiusField, w, def: 1000f);
            if (baseR <= 0.01f)
                baseR = ReadFloat(RadiusField, w, def: 1000f);

            float want = baseR * WarewindConstants.ShockwaveScaleMult;
            if (want > WarewindConstants.ShockwaveMaxRadiusCapM)
                want = WarewindConstants.ShockwaveMaxRadiusCapM;
            if (want <= 0.01f)
                return;

            float ratio = want / baseR;
            WriteFloat(MaxRadiusField, w, want);
            WriteFloat(RadiusField, w, want);

            if (LifetimeField != null && LifetimeField.GetValue(w) is float life && life > 0.01f)
                LifetimeField.SetValue(w, Mathf.Clamp(life * ratio, life, WarewindConstants.ShockwaveMaxLifetimeS));

            if (HeightField != null && HeightField.GetValue(w) is float h && h > 0.01f)
                HeightField.SetValue(w, h * ratio);
        }

        private static float ReadFloat(FieldInfo? f, object host, float def)
        {
            if (f?.GetValue(host) is float v && v > 0.01f)
                return v;
            return def;
        }

        private static void WriteFloat(FieldInfo? f, object host, float v)
        {
            if (f != null && f.FieldType == typeof(float))
                f.SetValue(host, v);
        }
    }
}