using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace PassiveBossPowers
{
    public class PluginInfo
    {
        public const string Name = "Passive Boss Powers";
        public const string Guid = "beardedkwan.PassiveBossPowers";
        public const string Version = "1.0.0";
    }

    [BepInPlugin(PluginInfo.Guid, PluginInfo.Name, PluginInfo.Version)]
    [BepInProcess("valheim.exe")]
    public class PassiveBossPowers : BaseUnityPlugin
    {
        void Awake()
        {
            Harmony harmony = new Harmony(PluginInfo.Guid);
            harmony.PatchAll();
        }

        // Make the forsaken status effects never expire
        [HarmonyPatch(typeof(ObjectDB), "CopyOtherDB")]
        internal class Patch_ObjectDB_CopyOtherDB
        {
            static void Postfix(ref ObjectDB __instance)
            {
                foreach (StatusEffect se in __instance.m_StatusEffects)
                {
                    if (se.name.StartsWith("GP_"))
                    {
                        se.m_ttl = 0f;
                        se.m_cooldown = 0f;
                        Debug.Log($"[{PluginInfo.Name}] Made {se.name} status effect passive.");
                    }
                }
            }
        }

        // Determine how many bosses have been defeated on start
        private static int bossesDefeatedCount = 0;
        [HarmonyPatch(typeof(Player), "Start")]
        private class Get_Bosses
        {
            private static void Postfix(ZoneSystem __instance)
            {
                if (!(Player.m_localPlayer != null))
                {
                    return;
                }

                bossesDefeatedCount = 0;

                foreach (string globalKey in ZoneSystem.instance.GetGlobalKeys())
                {
                    Debug.Log($"GlobalKey: {globalKey}");
                    if (globalKey.StartsWith("defeated"))
                    {
                        bossesDefeatedCount++;
                    }
                }

                Debug.Log($"Bosses defeated: {bossesDefeatedCount}");
            }
        }

        // Increment bossesDefeatedCount after defeating a boss and hanging it's trophy
        [HarmonyPatch(typeof(ItemStand), "DelayedPowerActivation")]
        internal class Patch_ItemStand_DelayedPowerActivation
        {
            static void Postfix(ref ItemStand __instance)
            {
                if (__instance.m_guardianPower == null)
                {
                    return;
                }

                bossesDefeatedCount += 1;
            }
        }

        // Cycle through unlocked powers
        [HarmonyPatch(typeof(Player), "Update")]
        private class Patch_Cycle_Powers
        {
            private static void Prefix(Player __instance)
            {
                if (UnityEngine.Input.GetKeyDown(KeyCode.F3))
                {
                    List<string> bossPowers = new List<string> { "GP_Eikthyr", "GP_TheElder", "GP_Bonemass", "GP_Moder", "GP_Yagluth", "GP_Queen" };

                    int selectedBossPowerIndex = 0;
                    string currentPower = __instance.GetGuardianPowerName();
                    if (currentPower != null)
                    {
                        selectedBossPowerIndex = bossPowers.IndexOf(currentPower);
                    }

                    if (bossesDefeatedCount > 0)
                    {
                        selectedBossPowerIndex = (selectedBossPowerIndex + 1) % bossesDefeatedCount;
                        string selectedPower = bossPowers[selectedBossPowerIndex];

                        __instance.SetGuardianPower(selectedPower);

                        string power = selectedPower.Substring(3);
                        __instance.Message(MessageHud.MessageType.Center, $"{power} Power Selected");

                        Debug.Log($"Selected boss power based on defeated count: {selectedPower}");
                    }
                    else
                    {
                        Debug.Log("No bosses defeated yet.");
                    }
                }
            }
        }
    }
}
