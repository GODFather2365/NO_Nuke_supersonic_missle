using System.Reflection;
using UnityEngine;

namespace Warewind
{
    internal enum WarewindThreatKind
    {
        None,
        Radar,
        Ir
    }

    /// <summary>Hostile inbound seeker — lock optional, closing geometry enough.</summary>
    internal static class WarewindThreatScan
    {
        private static readonly FieldInfo? SeekerTarget =
            typeof(MissileSeeker).GetField("targetUnit", BindingFlags.Instance | BindingFlags.NonPublic);

        /// <summary>
        /// One pass: radar lock stays on <paramref name="preferRadar"/> while valid;
        /// IR is picked independently so flares are not blocked by a closer SAM.
        /// </summary>
        internal static void Scan(Missile self, Missile? preferRadar, out Missile? radar, out Missile? ir)
        {
            radar = null;
            ir = null;
            if (self == null)
                return;

            // Sticky lock: keep jamming even if closing geometry flickers for a frame.
            if (preferRadar != null && IsStickyRadar(self, preferRadar))
                radar = preferRadar;

            System.Collections.Generic.List<Unit> units = UnitRegistry.allUnits;
            if (units == null)
                return;

            PersistentID id = self.persistentID;
            Missile? bestRadar = null;
            Missile? bestIr = null;
            float bestRadarD = float.MaxValue;
            float bestIrD = float.MaxValue;

            for (int i = 0; i < units.Count; i++)
            {
                if (!(units[i] is Missile m) || m == null || m.disabled || m == self)
                    continue;
                if (!IsHostile(self, m))
                    continue;
                if (!TryClassify(self, m, id, out WarewindThreatKind k))
                    continue;

                float d = (m.transform.position - self.transform.position).sqrMagnitude;
                if (k == WarewindThreatKind.Radar)
                {
                    if (d < bestRadarD)
                    {
                        bestRadarD = d;
                        bestRadar = m;
                    }
                }
                else if (k == WarewindThreatKind.Ir && d < bestIrD)
                {
                    bestIrD = d;
                    bestIr = m;
                }
            }

            if (radar == null)
                radar = bestRadar;
            ir = bestIr;
        }

        /// <summary>Keep existing jam lock while seeker lives — no IsClosing / nose gate.</summary>
        internal static bool IsStickyRadar(Missile self, Missile inbound)
        {
            if (self == null || inbound == null || inbound.disabled)
                return false;
            if (!IsHostile(self, inbound))
                return false;
            if (inbound.GetComponent<ARHSeeker>() == null && inbound.GetComponent<SARHSeeker>() == null)
                return false;
            float dist = (inbound.transform.position - self.transform.position).magnitude;
            return dist <= WarewindConstants.ThreatDetectRangeM;
        }

        private static bool TryClassify(Missile self, Missile inbound, PersistentID selfId, out WarewindThreatKind kind)
        {
            kind = WarewindThreatKind.None;

            bool locked = (selfId.IsValid && inbound.targetID.IsValid && inbound.targetID == selfId)
                            || GetSeekerTarget(inbound) == self;

            Vector3 toSelf = self.transform.position - inbound.transform.position;
            float dist = toSelf.magnitude;
            if (dist > WarewindConstants.ThreatDetectRangeM)
                return false;
            // Close-in: still track locked seekers (was dropping jam under 30 m).
            if (dist < 30f && !locked)
                return false;

            if (inbound.GetComponent<ARHSeeker>() != null || inbound.GetComponent<SARHSeeker>() != null)
            {
                kind = WarewindThreatKind.Radar;
                if (locked)
                    return true;
                if (!IsClosing(inbound, toSelf, dist))
                    return false;
                return NoseToward(inbound, toSelf);
            }

            if (inbound.GetComponent<IRSeeker>() != null)
            {
                kind = WarewindThreatKind.Ir;
                if (locked)
                    return true;
                if (!IsClosing(inbound, toSelf, dist))
                    return false;
                return NoseToward(inbound, toSelf);
            }

            return false;
        }

        private static bool IsHostile(Missile self, Missile inbound)
        {
            if (self.NetworkHQ != null && inbound.NetworkHQ != null)
                return inbound.NetworkHQ != self.NetworkHQ;
            return true;
        }

        private static bool IsClosing(Missile inbound, Vector3 toSelf, float dist)
        {
            if (dist < 0.01f)
                return true;

            Vector3 vel = inbound.rb != null && inbound.rb.velocity.sqrMagnitude > 100f
                ? inbound.rb.velocity
                : inbound.transform.forward * Mathf.Max(inbound.speed, 80f);
            if (vel.sqrMagnitude < 100f)
                return false;

            Vector3 toN = toSelf / dist;
            return Vector3.Dot(vel.normalized, toN) >= WarewindConstants.ThreatClosingDotMin;
        }

        private static bool NoseToward(Missile inbound, Vector3 toSelf)
        {
            if (toSelf.sqrMagnitude < 1f)
                return true;
            return Vector3.Angle(inbound.transform.forward, toSelf) <= WarewindConstants.ThreatAimConeDeg;
        }

        private static Unit? GetSeekerTarget(Missile inbound)
        {
            MissileSeeker? s = inbound.GetComponent<MissileSeeker>();
            if (s == null || SeekerTarget == null)
                return null;
            return SeekerTarget.GetValue(s) as Unit;
        }
    }
}
