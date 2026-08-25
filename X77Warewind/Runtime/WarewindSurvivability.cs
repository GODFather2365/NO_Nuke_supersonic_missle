using System.Reflection;
using UnityEngine;

namespace Warewind
{
    /// <summary>
    /// Durability from vanilla Piledriver TBM (BallisticMissile1).
    /// Impact still ignored — shared AAM2 shell detonates on any impactDamage.
    /// </summary>
    internal static class WarewindSurvivability
    {
        private static readonly FieldInfo? Hitpoints =
            typeof(Missile).GetField("hitpoints", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo? Armor =
            typeof(Missile).GetField("armorProperties", BindingFlags.Instance | BindingFlags.NonPublic);

        private static float _hp = 100f;
        private static float _armorTier;
        private static float _pierce;
        private static float _blast;
        private static float _fire;
        private static float _pierceTol = 1f;
        private static float _blastTol = 1f;
        private static float _fireTol = 1f;
        private static bool _cached;

        internal static void Cache(Encyclopedia enc)
        {
            _cached = false;
            MissileDefinition? tbm = FindTbm(enc);
            if (tbm == null)
            {
                WarewindPlugin.ModLog?.LogWarning("Warewind: no BallisticMissile1 — using vanilla 100 HP.");
                return;
            }

            _armorTier = tbm.armorTier;
            Missile? mis = tbm.unitPrefab != null ? tbm.unitPrefab.GetComponent<Missile>() : null;
            if (mis == null && tbm.unitPrefab != null)
                mis = tbm.unitPrefab.GetComponentInChildren<Missile>(true);

            if (mis != null)
            {
                if (Hitpoints?.GetValue(mis) is float hp && hp > 0.01f)
                    _hp = hp;
                if (Armor?.GetValue(mis) is ArmorProperties ap)
                    CopyArmor(ap);
            }

            _cached = true;
            WarewindPlugin.ModLog?.LogInfo(
                $"Warewind TBM armor from '{tbm.jsonKey}' hp={_hp:F0} tier={_armorTier:F1} pierce={_pierce:F0}/{_pierceTol:F1}");
        }

        internal static void ApplyDefinition(MissileDefinition? def)
        {
            if (def == null)
                return;
            def.armorTier = _armorTier;
        }

        internal static void Apply(Missile missile)
        {
            if (missile == null)
                return;
            if (missile.definition is MissileDefinition md)
                md.armorTier = _armorTier;
            StampArmor(missile);
            SetHp(missile, _hp);
        }

        internal static bool ProcessDamage(
            Missile missile,
            float pierceDamage,
            float blastDamage,
            float amountAffected,
            float fireDamage,
            PersistentID dealerId)
        {
            if (missile == null || missile.disabled)
                return true;

            StampArmor(missile);

            if (dealerId == missile.persistentID || dealerId == missile.ownerID || dealerId.NotValid)
                return true;

            ArmorProperties ap = missile.GetArmorProperties();
            if (pierceDamage <= ap.pierceArmor && blastDamage <= ap.blastArmor && fireDamage <= ap.fireArmor)
                return true;

            float p = Mathf.Max(pierceDamage - ap.pierceArmor, 0f) / Mathf.Max(ap.pierceTolerance, 0.1f);
            float b = Mathf.Max(blastDamage - ap.blastArmor, 0f) * amountAffected / Mathf.Max(ap.blastTolerance, 0.1f);
            float f = Mathf.Max(fireDamage - ap.fireArmor, 0f) / Mathf.Max(ap.fireTolerance, 0.1f);
            float loss = p + b + f;
            if (loss <= 0.001f)
                return true;

            float hp = GetHp(missile) - loss;
            SetHp(missile, hp);
            if (hp > 0f)
                return true;

            PersistentUnit dealer;
            if (UnitRegistry.TryGetPersistentUnit(dealerId, out dealer) && dealer.GetHQ() != missile.NetworkHQ)
            {
                missile.RecordDamage(dealerId, 1000f);
                missile.ReportKilled();
            }

            WarewindFuse.AllowDetonate = true;
            try
            {
                Vector3 n = missile.rb != null ? missile.rb.velocity : missile.transform.forward;
                missile.Detonate(n, false, false);
            }
            finally
            {
                WarewindFuse.AllowDetonate = false;
            }
            return true;
        }

        private static void StampArmor(Missile missile)
        {
            if (!_cached || Armor?.GetValue(missile) is not ArmorProperties ap)
                return;
            ap.pierceArmor = _pierce;
            ap.blastArmor = _blast;
            ap.fireArmor = _fire;
            ap.pierceTolerance = _pierceTol;
            ap.blastTolerance = _blastTol;
            ap.fireTolerance = _fireTol;
        }

        private static void CopyArmor(ArmorProperties ap)
        {
            _pierce = ap.pierceArmor;
            _blast = ap.blastArmor;
            _fire = ap.fireArmor;
            _pierceTol = ap.pierceTolerance;
            _blastTol = ap.blastTolerance;
            _fireTol = ap.fireTolerance;
        }

        private static MissileDefinition? FindTbm(Encyclopedia enc)
        {
            if (enc?.missiles == null)
                return null;
            MissileDefinition? best = null;
            int score = -1;
            for (int i = 0; i < enc.missiles.Count; i++)
            {
                MissileDefinition? m = enc.missiles[i];
                if (m == null || string.IsNullOrEmpty(m.jsonKey) || m.unitPrefab == null)
                    continue;
                string k = m.jsonKey;
                int s = 0;
                if (k.Equals("BallisticMissile1", System.StringComparison.OrdinalIgnoreCase))
                    s = 100;
                else if (k.StartsWith("BallisticMissile1", System.StringComparison.OrdinalIgnoreCase))
                    s = 80;
                else if (k.IndexOf("BallisticMissile", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    s = 40;
                if (s > score)
                {
                    score = s;
                    best = m;
                }
            }
            return best;
        }

        private static float GetHp(Missile missile)
        {
            if (Hitpoints?.GetValue(missile) is float hp)
                return hp;
            return _hp;
        }

        private static void SetHp(Missile missile, float hp)
        {
            Hitpoints?.SetValue(missile, Mathf.Max(0f, hp));
        }
    }
}
