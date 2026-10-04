using System.Reflection;
using UnityEngine;

namespace Warewind
{
    /// <summary>
    /// 500 kt FX: vanilla Shockwave baked for the TBM tacNuke is sized by the game's own yield →
    /// at blastYield=500e6 kg it dwarfs the map. Scale radius/lifetime/height on cloned FX GOs
    /// (templates stay untouched), clamp so the ring stays readable, log actual maxRadius.
    /// Missing private fields are skipped silently — only reflection, no new dependencies.
    /// </summary>
    internal static class WarewindShockwaveFx
    {
        private const BindingFlags Inst = BindingFlags.Instance | BindingFlags.NonPublic;

        // Shockwave field names from decompile; fallbacks cover renamed variants across game updates.
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

            WarewindPlugin.ModLog?.LogInfo(
                $"Warewind nuke FX: shockwaves={waves.Length} mult={WarewindConstants.ShockwaveScaleMult:F2} " +
                $"cap={WarewindConstants.ShockwaveMaxRadiusCapM:F0}m");
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

            // Ring must cross the cap in a sane time — stretch lifetime with the same ratio, clamped.
            if (LifetimeField != null && LifetimeField.GetValue(w) is float life && life > 0.01f)
            {
                float newLife = Mathf.Clamp(life * ratio, life, WarewindConstants.ShockwaveMaxLifetimeS);
                LifetimeField.SetValue(w, newLife);
            }

            if (HeightField != null && HeightField.GetValue(w) is float h && h > 0.01f)
                HeightField.SetValue(w, h * ratio);

            WarewindPlugin.ModLog?.LogInfo(
                $"Warewind shockwave '{w.gameObject.name}': r {baseR:F0}->{want:F0}m");
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
