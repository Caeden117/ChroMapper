using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal.Internal;

/*
 * Woah, woah, woah. Harmony in ChroMapper? What is this shit!?
 * 
 * In the Universal Render Pipeline, Unity decided to, in the name of performance, butcher the quality of the Bloom Effect.
 * In Post Processing V2, it looked fine. In Post Processing V3, it looks like shit.
 * 
 * This harmony patch is designed to modify the 2(!) lines of code needed to restore graphical quality to the levels seen in
 * Post Processing V2.
 */
public class BetterBloomController : MonoBehaviour
{
#if !UNITY_STANDALONE_OSX
    private const string betterBloomID = "com.caeden117.chromapper.betterbloom";

    private Harmony betterBloomHarmony;

    // Start is called before the first frame update
    private void Start()
    {
        betterBloomHarmony = new Harmony(betterBloomID);

        if (Settings.Instance.HighQualityBloom)
        {
            var ppPass = typeof(PostProcessPass);
            MethodBase setupBloom = ppPass.GetMethod("SetupBloom",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public,
                Type.DefaultBinder,
                new[] { typeof(CommandBuffer), typeof(int), typeof(Material) }, new ParameterModifier[] { });
            var transpiler = new HarmonyMethod(typeof(BetterBloomController), nameof(PatchSetupBloom));
            betterBloomHarmony.Patch(setupBloom, transpiler: transpiler);
        }
    }

    private void OnDestroy() => betterBloomHarmony.UnpatchAll(betterBloomID);

    /*
     * Replace the native IL for the "SetupBloom" function to remove bit shifting to the right.
     * This gives us a full resolution Bloom effect, much more realistic to Beat Saber's.
     * 
     * This is called once, not every time the method is called, so no big performance drops will happen.
     * 
     * Thanks to DaNike from the Beat Saber Modding Group for helping me with this transpiler patch.
     */
    private static IEnumerable<CodeInstruction> PatchSetupBloom(IEnumerable<CodeInstruction> insns)
    {
        var resList = new List<CodeInstruction>();
        var seqCount = 0;
        var foundLdc1 = false;
        foreach (var ci in insns)
        {
            if (seqCount < 2)
            {
                if (!foundLdc1 && ci.opcode == OpCodes.Ldc_I4_1)
                {
                    foundLdc1 = true;
                }
                else if (foundLdc1 && ci.opcode == OpCodes.Shr)
                {
                    foundLdc1 = false;
                    seqCount++;
                    continue;
                }
            }

            if (!foundLdc1)
                resList.Add(ci);
        }

        return resList;
    }
#endif
}
