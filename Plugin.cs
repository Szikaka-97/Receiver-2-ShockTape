using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using Receiver2;
using System.Reflection;
using System.Collections.Generic;

namespace ShockTape {
	[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
	public class Plugin : BaseUnityPlugin {

		public static Plugin Instance {
			get;
			private set;
		}

		private static ConfigEntry<bool> spawnEnabled;
		private static ConfigEntry<float> flashbangRarity;

		private void Awake() {
			Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
			
			Instance = this;

			flashbangRarity = Config.Bind(
				new ConfigDefinition("Tape Settings", "Flashbang tape chance"),
				0.1f,
				new ConfigDescription("How often will flashbang tapes spawn", new AcceptableValueRange<float>(0f, 1f))
			);

			spawnEnabled = Config.Bind(
				"Tape Settings",
				"Enabled",
				true
			);

			FlashbangTape.GetReflectionMethods();

			Harmony.CreateAndPatchAll(this.GetType());
		}

		[HarmonyPatch(typeof(RuntimeTileLevelGenerator), "InstantiateTapeGroup")]
		[HarmonyPostfix]
		public static void patchInstantiateTape(ref GameObject __result) {
			if (Random.value > flashbangRarity.Value || !spawnEnabled.Value) return;

			TapeGroupItem tape = __result.GetComponent<TapeGroupItem>();

			var tape_roll_left = tape.tape_roll_left;
			var tape_roll_right = tape.tape_roll_right;
			var body_renderer = tape.body_renderer;
			var tape_manager = tape.tape_manager;
			var rigid_body = tape.rigid_body;
			var colliders = tape.colliders;
			var instance_id = tape.ItemInstanceID;

			Destroy(tape);


			tape = __result.AddComponent<FlashbangTape>();

			tape.tape_roll_left = tape_roll_left;
			tape.tape_roll_right = tape_roll_right;
			tape.body_renderer = body_renderer;
			tape.tape_manager = tape_manager;
			tape.tape_group_id = TapeGroupID.MindControl;
			tape.rigid_body = rigid_body;
			tape.colliders = colliders;
			tape.type = InventoryItem.Type.TapeGroup;
			((FlashbangTape) tape).instance_id = instance_id;

			tape.Move(null, true, true);
			tape.SetRested();
		}
	}
}
