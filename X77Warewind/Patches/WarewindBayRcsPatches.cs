using HarmonyLib;
using Warewind.Bootstrap;

namespace Warewind.Patches
{
    /// <summary>
    /// Shared mount keeps RadarSize for external pylons; internal bay hardpoints must not change aircraft RCS.
    /// Temporarily zero GetRCSPerRound during Attach/Remove/Rearm (do not mutate shared SO permanently).
    /// </summary>
    internal static class WarewindBayRcs
    {
        private static readonly AccessTools.FieldRef<MountedMissile, Hardpoint> HardpointRef =
            AccessTools.FieldRefAccess<MountedMissile, Hardpoint>("hardpoint");

        private static readonly AccessTools.FieldRef<MountedMissile, WeaponMount> MountRef =
            AccessTools.FieldRefAccess<MountedMissile, WeaponMount>("mount");

        internal static bool IsBayHardpoint(Hardpoint? hardpoint) =>
            hardpoint != null && hardpoint.bayDoors != null && hardpoint.bayDoors.Length > 0;

        internal static bool TrySuppress(WeaponMount? mount, Hardpoint? hardpoint, out float savedRcs)
        {
            savedRcs = 0f;
            if (mount == null || !WarewindBootstrap.IsOurMount(mount) || !IsBayHardpoint(hardpoint))
                return false;
            savedRcs = mount.RCS;
            mount.RCS = mount.emptyRCS;
            return true;
        }

        internal static void Restore(WeaponMount? mount, float savedRcs, bool active)
        {
            if (!active || mount == null)
                return;
            mount.RCS = savedRcs;
        }

        internal static WeaponMount? GetMount(MountedMissile mm) => mm == null ? null : MountRef(mm);

        internal static Hardpoint? GetHardpoint(MountedMissile mm) => mm == null ? null : HardpointRef(mm);
    }

    [HarmonyPatch(typeof(MountedMissile), nameof(MountedMissile.AttachToHardpoint))]
    internal static class WarewindBayRcsAttachPatch
    {
        private static void Prefix(Hardpoint hardpoint, WeaponMount weaponMount, out float __state)
        {
            __state = float.NaN;
            if (WarewindBayRcs.TrySuppress(weaponMount, hardpoint, out float saved))
                __state = saved;
        }

        private static void Finalizer(WeaponMount weaponMount, float __state)
        {
            WarewindBayRcs.Restore(weaponMount, __state, !float.IsNaN(__state));
        }
    }

    [HarmonyPatch(typeof(MountedMissile), "RemoveFromHardpoint")]
    internal static class WarewindBayRcsRemovePatch
    {
        private static void Prefix(MountedMissile __instance, out float __state)
        {
            __state = float.NaN;
            WeaponMount? mount = WarewindBayRcs.GetMount(__instance);
            Hardpoint? hp = WarewindBayRcs.GetHardpoint(__instance);
            if (WarewindBayRcs.TrySuppress(mount, hp, out float saved))
                __state = saved;
        }

        private static void Finalizer(MountedMissile __instance, float __state)
        {
            WarewindBayRcs.Restore(WarewindBayRcs.GetMount(__instance), __state, !float.IsNaN(__state));
        }
    }

    [HarmonyPatch(typeof(MountedMissile), nameof(MountedMissile.Rearm))]
    internal static class WarewindBayRcsRearmPatch
    {
        private static void Prefix(MountedMissile __instance, out float __state)
        {
            __state = float.NaN;
            WeaponMount? mount = WarewindBayRcs.GetMount(__instance);
            Hardpoint? hp = WarewindBayRcs.GetHardpoint(__instance);
            if (WarewindBayRcs.TrySuppress(mount, hp, out float saved))
                __state = saved;
        }

        private static void Finalizer(MountedMissile __instance, float __state)
        {
            WarewindBayRcs.Restore(WarewindBayRcs.GetMount(__instance), __state, !float.IsNaN(__state));
        }
    }
}
