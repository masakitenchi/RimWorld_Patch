﻿using RimWorld;
using Verse;
using Androids;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Reflection;
using System.Linq;

namespace AndroidsIdeologyPatch
{
#if DEBUG
    [HarmonyDebug]
#endif
    [StaticConstructorOnStartup]
	public static class HarmonyPatches
    {
		private static readonly Type patchType;

        private static readonly MethodInfo list_get_Item = AccessTools.Method(typeof(List<Pawn>), "get_Item");
        public static bool IsDroid(this Pawn p)
        {
            return p.RaceProps.FleshType.ToString() == "ChJDroid";
            //return p.def.defName == "ChjDroid" || p.def.defName == "ChjBattleDroid";
        }
        public static bool IsSkynet(this Pawn p)
        {
            return p.health.hediffSet.hediffs.Exists(x => x.def.defName == "AndroidPassive");
        }
		static HarmonyPatches()
        {
            Log.Message("AndroidsIdeologyPatch Initializing...");
			patchType = typeof(HarmonyPatches);
			Harmony harmony = new Harmony("com.reggex.AndroidIdeologyPatch");
            //Patch all "ShouldHaveThought" method in ThoughtWorker_Precept_*_Social
            /*Type targettype = typeof(ThoughtWorker_Precept_Social);
            MethodInfo targetmethod = targettype.GetMethod("ShouldHaveThought");
            Assembly assembly_csharp = Assembly.GetAssembly(targettype);
            Type[] derivedtypes = AccessTools.GetTypesFromAssembly(assembly_csharp).Where(t => t.IsSubclassOf(targettype)).ToArray();
            Log.Message(String.Format("Found {0} SubClass of {1}. Patching ShouldHaveThought_Social.", derivedtypes.Length, targettype.ToString()));
            foreach (Type derivedtype in derivedtypes)
            {
                MethodInfo[] overridingMethods = derivedtype.GetMethods(BindingFlags.DeclaredOnly|BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).Where(m =>m.GetBaseDefinition() == targetmethod).ToArray();
                foreach (MethodInfo method in overridingMethods)
                {
                    harmony.Patch(method, postfix: new HarmonyMethod(patchType, "SocialPostfix"));
                }
            }*/
			harmony.Patch(AccessTools.Method(typeof(ThoughtWorker_Precept_HasNoProsthetic), "ShouldHaveThought"), postfix: new HarmonyMethod(patchType,nameof(ProstheticShouldHaveThoughtPostfix)));
			harmony.Patch(AccessTools.Method(typeof(ThoughtWorker_Precept_HasNoProsthetic_Social), "ShouldHaveThought"), postfix: new HarmonyMethod(patchType, nameof(ProstheticShouldHaveThoughtSocialPostfix)));
            harmony.Patch(AccessTools.Method(typeof(ThoughtWorker_Precept_IdeoDiversity_Social), "ShouldHaveThought"), postfix: new HarmonyMethod(patchType, nameof(SocialPostfix)));
            //Transpilers that add new conditions
            harmony.Patch(AccessTools.Method(typeof(ThoughtWorker_Precept_IdeoDiversity), "ShouldHaveThought"), transpiler: new HarmonyMethod(patchType, nameof(DiversityTranspiler)));
            harmony.Patch(AccessTools.Method(typeof(ThoughtWorker_Precept_IdeoDiversity_Uniform), "ShouldHaveThought"), transpiler: new HarmonyMethod(patchType, nameof(UniformTranspiler)));
            Log.Message("AndroidsIdeologyPatch Initialized.");
		}
		public static void ProstheticShouldHaveThoughtPostfix(ref ThoughtState __result, Pawn p)
        {
            __result = p.IsAndroid() || p.IsSkynet() ? false : __result;
			return;
        }
		public static void ProstheticShouldHaveThoughtSocialPostfix(ref ThoughtState __result, Pawn otherPawn)
        {
            __result = otherPawn.IsAndroid() || otherPawn.IsSkynet() ? false : __result;
			return;
        }

        public static void SocialThoughtWorkerPatchForDroids(Pawn p, Pawn otherPawn, ref ThoughtState __result)
        {
            __result = otherPawn.IsDroid() ? false : __result;
        }
        public static IEnumerable<CodeInstruction> DiversityTranspiler(IEnumerable<CodeInstruction> instructions,ILGenerator generator)
        {
            int index=0;
            bool foundif = false, foundnextloop = false;
            List<CodeInstruction> codes = instructions.ToList();
            Label nextloop = generator.DefineLabel();
            for(var i=0;i<codes.Count;i++)
            {
                if(!foundif && codes[i].opcode==OpCodes.Ldloc_2 && codes[i+1].opcode==OpCodes.Ldloc_3)
                {
                    foundif = true;
                    index = i+5;
                }
                if(!foundnextloop && codes[i].opcode==OpCodes.Ldloc_3 && codes[i+1].opcode==OpCodes.Ldc_I4_1)
                {
                    codes[i].labels.Add(nextloop);
                    foundnextloop = true;
                }
            }
            if(!foundif || !foundnextloop)
            {
                Log.Error("Error: No Such Code Found. Old transpiler?");
                return instructions;
            }
            List<CodeInstruction> newinstructions = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldloc_2),
                new CodeInstruction(OpCodes.Ldloc_3),
                new CodeInstruction(OpCodes.Callvirt,list_get_Item),
                new CodeInstruction(OpCodes.Call,AccessTools.Method(typeof(HarmonyPatches),"IsDroid")),
                new CodeInstruction(OpCodes.Brtrue_S,nextloop),
                new CodeInstruction(OpCodes.Ldloc_2),
                new CodeInstruction(OpCodes.Ldloc_3),
                new CodeInstruction(OpCodes.Callvirt,list_get_Item),
                new CodeInstruction(OpCodes.Call,AccessTools.Method(typeof(HarmonyPatches),"IsSkynet")),
                new CodeInstruction(OpCodes.Brtrue_S,nextloop),
            };
            codes.InsertRange(index, newinstructions);
            return codes;
        }
        public static void SocialPostfix(ref ThoughtState __result, Pawn otherPawn)
        {
            __result = otherPawn.IsDroid() || otherPawn.IsSkynet() ? false : __result;
        }
        public static IEnumerable<CodeInstruction> UniformTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            int index=0;
            bool foundif = false, foundnextloop = false;
            List<CodeInstruction> codes = instructions.ToList();
            Label nextloop = generator.DefineLabel();
            Label starthere = generator.DefineLabel();
            for (var i = 0; i < codes.Count; i++)
            {
                if (!foundif && codes[i].opcode == OpCodes.Ldloc_0 && codes[i + 1].opcode == OpCodes.Ldloc_2)
                {
                    foundif = true;
                    index = i+5;
                }
                if (!foundnextloop && codes[i].opcode == OpCodes.Ldloc_2 && codes[i + 1].opcode == OpCodes.Ldc_I4_1)
                {
                    codes[i].labels.Add(nextloop);
                    foundnextloop = true;
                }
            }
            if (!foundif || !foundnextloop)
            {
                Log.Error("Error: Cannot patch IdeoUniformity method. Old transpiler?");
                return instructions;
            }
            List<CodeInstruction> newinstructions = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldloc_0),
                new CodeInstruction(OpCodes.Ldloc_2),
                new CodeInstruction(OpCodes.Callvirt,list_get_Item),
                new CodeInstruction(OpCodes.Call,AccessTools.Method(typeof(HarmonyPatches),"IsDroid")),
                new CodeInstruction(OpCodes.Brtrue_S,nextloop),
                new CodeInstruction(OpCodes.Ldloc_0),
                new CodeInstruction(OpCodes.Ldloc_2),
                new CodeInstruction(OpCodes.Callvirt,list_get_Item),
                new CodeInstruction(OpCodes.Call,AccessTools.Method(typeof(HarmonyPatches),"IsSkynet")),
                new CodeInstruction(OpCodes.Brtrue_S,nextloop),
            };
            newinstructions[0].labels.Add(starthere);
            codes.InsertRange(index, newinstructions);
            return codes;
        }
    }
}