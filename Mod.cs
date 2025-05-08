using HarmonyLib;
using ModBagman;
using SoG;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using Microsoft.Xna.Framework;
using Microsoft.Extensions.Logging;


namespace GreentheNinja.MorePlayersMod
{
    [HarmonyPatch]
    public class MorePlayersMod : Mod
    {
        public override string Name => "MorePlayersMod";
        public override System.Version Version => new (0, 0, 1);
        
        public static MorePlayersMod Instance { get; private set; }
        
        public bool Enabled { get; private set; } = true;
        
        public override void Load()
        {
            Instance = this;
        }
        
        public override void Unload()
        {
            Instance = null;
        }
        
        [HarmonyPatch(typeof(Game1), nameof(Game1._Network_ParseClientMessage))]
        [HarmonyTranspiler()]
	    public static IEnumerable<CodeInstruction> LobbySizeLimitTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var instructionList = new List<CodeInstruction>();
            bool foundReplacement = false;
            foreach (CodeInstruction instruction in instructions)
            {
                instructionList.Add(instruction);
                // idk if it's really necessary but might as well speed up the rest if I can y'know?
                if (foundReplacement)
                {
                    continue;
                }
                if (instructionList.Count >= 6)
                {
                    // check if -6 is ldloc.s 20
                    // check if -5 is ldc.i4.4
                    // check if -4 is blt.s IL_0aac
                    // check if -3 is ldc.i4.2
                    // check if -2 is stloc.s 19
                    // check if -1 is br IL_0b95
                    if (instructionList[instructionList.Count - 6].opcode != OpCodes.Ldloc_S)
                    {
                        continue;
                    }
                    if (instructionList[instructionList.Count - 5].opcode != OpCodes.Ldc_I4_4)
                    {
                        continue;
                    }
                    if (instructionList[instructionList.Count - 4].opcode != OpCodes.Blt_S)
                    {
                        continue;
                    }
                    if (instructionList[instructionList.Count - 3].opcode != OpCodes.Ldc_I4_2)
                    {
                        continue;
                    }
                    if (instructionList[instructionList.Count - 2].opcode != OpCodes.Stloc_S)
                    {
                        continue;
                    }
                    if (instructionList[instructionList.Count - 1].opcode != OpCodes.Br)
                    {
                        continue;
                    }
                    Instance?.Logger.LogInformation("Found lobby player limit check from indexes {start} to {end}", instructionList.Count - 6, instructionList.Count - 1);
                    var replacementInstruction = new CodeInstruction(OpCodes.Ldc_I4_S, 100);
                    instructionList[instructionList.Count - 5] = replacementInstruction;
                    foundReplacement = true;
                }
            }
            
            if (!foundReplacement)
            {
                Instance?.Logger.LogWarning("Did not find lobby player limit check");
            }
            
            return instructionList;
        }
        
        [HarmonyPatch(typeof(EnemySpawner), nameof(EnemySpawner.Update))]
        [HarmonyTranspiler()]
        public static IEnumerable<CodeInstruction> EnemySpawnerUpdateTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            return ClampPlayerCountChecks(instructions);
        }
        
        // This will most likely have to be done for several individual functions, so here's a generic application.
        public static IEnumerable<CodeInstruction> ClampPlayerCountChecks(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);
            matcher.MatchStartForward(
                    new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(Game1), nameof(Game1.dixPlayers))),
                    new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(Dictionary<long, PlayerView>), nameof(Dictionary<long, PlayerView>.Count)))
                )
                .Repeat(
                    cm => {
                        cm.Advance(2);
                        cm.Insert(
                            new CodeInstruction(OpCodes.Ldc_I4_4),
                            CodeInstruction.Call(typeof(Math), nameof(Math.Min), new Type[2] {typeof(int), typeof(int)})
                        );
                    }
                );
            return matcher.Instructions();
        }
        
        // We need a bit of special logic here as opposed to ClampPlayerCountChecks, since we only want to touch the comparisons to the total player count.
        [HarmonyPatch(typeof(Game1), nameof(Game1._Trigger_HandleTriggerEvent), new Type[2] {typeof(FlagCodex.FlagID), typeof(byte)})]
        [HarmonyTranspiler()]
        public static IEnumerable<CodeInstruction> Game1_Trigger_HandleTriggerEventTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);
            matcher.MatchStartForward(
                    new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(Game1), nameof(Game1.dixPlayers))),
                    new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(Dictionary<long, PlayerView>), nameof(Dictionary<long, PlayerView>.Count))),
                    new CodeMatch(OpCodes.Blt)
                )
                .Repeat(
                    cm => {
                        cm.Advance(2);
                        cm.Insert(
                            new CodeInstruction(OpCodes.Ldc_I4_4),
                            CodeInstruction.Call(typeof(Math), nameof(Math.Min), new Type[2] {typeof(int), typeof(int)})
                        );
                    }
                );
            return matcher.Instructions();
        }
        
        // Mostly, all we need to do is prevent this line from firing for players 5+:
        //      xView.xEntity.xTransform.SetBoth(dxxPlayerPositions[byPlayNum]);
        // After that, I guess we'll see what happens?
        [HarmonyPatch(typeof(Bagmen.ShieldPracticeInArena), nameof(Bagmen.ShieldPracticeInArena.OnAdd))]
        [HarmonyTranspiler()]
        public static IEnumerable<CodeInstruction> ShieldPracticeInArenaOnAddTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);
            // do this whenever we find and remove the above line
            /*
            matcher.Insert(
                CodeInstruction.Call(
                    (TransformComponent transform, Dictionary<byte, Vector2> playerPositions, byte playerNumber) =>
                    ShieldPracticeTransformSetter(transform, playerPositions, playerNumber)
                )
            );
            */
            return matcher.Instructions();
        }
        
        public static void ShieldPracticeTransformSetter(TransformComponent transform, Dictionary<byte, Vector2> playerPositions, byte playerNumber)
        {
            if (playerPositions.ContainsKey(playerNumber))
            {
                transform.SetBoth(playerPositions[playerNumber]);
            }
        }
    }
}
