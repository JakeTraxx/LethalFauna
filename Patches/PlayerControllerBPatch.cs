using GameNetcodeStuff;
using HarmonyLib;
using LethalFauna.Enemies;
using UnityEngine;

namespace LethalFauna.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    internal class PlayerControllerBPatch
    {
        static bool checkSprint;
        static bool checkClimbing;

        // Register crouching action for the Scribe
        [HarmonyPatch("Crouch")]
        [HarmonyPostfix]
        static void CrouchPatch(PlayerControllerB __instance)
        {
            if (!__instance.IsOwner)
                return;

            ScribeAI[] scribes = Object.FindObjectsOfType<ScribeAI>();
            foreach (ScribeAI ai in scribes)
                ai.NewActivity(__instance, 3);
        }

        // Register jumping action for the Scribe
        [HarmonyPatch("PlayerJump")]
        [HarmonyPostfix]
        static void PlayerJumpPatch(PlayerControllerB __instance)
        {
            if (!__instance.IsOwner)
                return;

            ScribeAI[] scribes = Object.FindObjectsOfType<ScribeAI>();
            foreach (ScribeAI ai in scribes)
                ai.NewActivity(__instance, 1);
        }

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void UpdatePatch(PlayerControllerB __instance)
        {
            if (!__instance.IsOwner)
                return;

            // Register sprinting action for the Scribe (not working currently)
            /*if (__instance.isSprinting && checkSprint)
            {
                checkSprint = false;
                ScribeAI[] scribes = Object.FindObjectsOfType<ScribeAI>();
                foreach (ScribeAI ai in scribes)
                    ai.NewActivity(__instance, 0);
            }
            else if (!__instance.isSprinting && !checkSprint)
                checkSprint = true;

            // Register climbing action for the Scribe (not working currently)
            if (__instance.isClimbingLadder && checkClimbing)
            {
                checkClimbing = false;
                ScribeAI[] scribes = Object.FindObjectsOfType<ScribeAI>();
                foreach (ScribeAI ai in scribes)
                    ai.NewActivity(__instance, 2);
            }
            else if (!__instance.isClimbingLadder && !checkClimbing)
                checkClimbing = true;*/
        }
    }
}
