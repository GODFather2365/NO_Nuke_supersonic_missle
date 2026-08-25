using HarmonyLib;
using Warewind;

namespace Warewind.Patches
{
    /// <summary>
    /// PersistentUnit snapshots unitName at register — keep kill feed / HQ messages on Warewind identity.
    /// </summary>
    [HarmonyPatch(typeof(UnitRegistry), nameof(UnitRegistry.RegisterUnit))]
    internal static class WarewindPersistentIdentityPatch
    {
        private static void Postfix(Unit unit)
        {
            if (unit is not Missile missile || !WarewindBootstrap.IsOurs(missile))
                return;
            WarewindSpawnGate.ApplyDisplayIdentity(missile);
        }
    }
}
