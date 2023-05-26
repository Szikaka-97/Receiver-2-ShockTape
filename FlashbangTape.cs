using System;
using System.Collections.Generic;
using System.Reflection;
using Receiver2;
using UnityEngine;
using SimpleJSON;

namespace ShockTape {
	class FlashbangTape : TapeGroupItem, IConsumable {
		private static FieldInfo instanceID;
		private static MethodInfo getActiveItem;

		public int instance_id;

		private static Color[] easyColors = new Color[] {
			new Color(0.7f, 0.7f, 0.7f),
			new Color(0.1f, 0.1f, 0.8f),
			new Color(0.8f, 0.1f, 0.8f),
			new Color(0.1f, 0.1f, 0.1f)
		};

		public static Color getLabelColor() {
			if (Plugin.difficulty.Value == "easy") {
				return easyColors[UnityEngine.Random.Range(0, easyColors.Length)];
			}
			else {
				return new Color(0.4f, 0.2f, 0);
			}
		}

		public static Color getBodyColor() {
			if (Plugin.difficulty.Value == "easy") {
				return new Color(0.4f, 0.4f, 0.4f);
			}
			else {
				return new Color(0.65f, 0.65f, 0.65f);
			}
		}

		public static void GetReflectionMethods() {
			instanceID = typeof(InventoryItem).GetField("item_instance_id", BindingFlags.Instance | BindingFlags.NonPublic);
			getActiveItem = typeof(RuntimeTileLevelGenerator).GetMethod("GetItemWithInventoryInstanceID", BindingFlags.NonPublic | BindingFlags.Instance);
		}

		new private void Start() {
			base.Start();

			instanceID.SetValue(this, instance_id);

			BulletScriptEvent shot_event = new();

			shot_event.AddListener(WasShot);

			transform.Find("bulletcatch").GetComponent<Shootable>().shot_function = shot_event;

			transform.Find("model/cassette_body").GetComponent<MeshRenderer>().material.SetColor("_BodyColor", getBodyColor());
			transform.Find("model/cassette_labels").GetComponent<MeshRenderer>().material.color = getLabelColor();

			transform.Find("model/tape_roll_left").localScale = Vector3.zero;
			transform.Find("model/tape_roll_right").localScale = Vector3.zero;
		}

		new public void DrawDebugSphere() {
			if (Camera.main == null) {
				return;
			}
			Color color = new Color(0f, 0f, 0.2f);

			float dist = Vector3.Distance(Camera.main.transform.position, transform.position);
			if (dist > 1f) {
				DebugDraw.Sphere(transform.position, color, Vector3.one * 0.01f * dist, Quaternion.identity, DebugDraw.Lifetime.OneFrame, DebugDraw.Type.Xray);
			}
		}

		new public void Consume() {
			FlashBang();

			Destroy(gameObject);
		}

		new public void WasShot(ShootableQuery shootable_query) {
			if (shootable_query.bullet_trajectory.bullet_source_entity_type == ReceiverEntityType.Player && ReceiverCoreScript.Instance().game_mode.GetGameMode() != GameMode.Classic) {
				Destroy(this);
				GetComponent<Rigidbody>().isKinematic = false;

				AudioManager.PlayOneShotAttached("event:/robots/damage_battery", gameObject, 0.5f);

				ActiveItem item = (ActiveItem) getActiveItem.Invoke(RuntimeTileLevelGenerator.instance, new object[] {instance_id});

				item.item = null;
				item.item_type = Type.GenericHoldable;
				item.unload = true;
			}
		}

		public override void SetPersistentData(JSONObject data) {
			tape_group_id = (TapeGroupID) data["tape_group_id"].AsInt;
		}

		public override JSONObject GetPersistentData() {
			JSONObject jsonobject = new JSONObject();
			jsonobject.Add("tape_group_id", (int) tape_group_id);
			return jsonobject;
		}

		public override string TypeName() {
			return "flashbang_tape_item";
		}


		private System.Collections.IEnumerator clearAlert(FlashbangVision vision) {
			yield return new WaitForSeconds(1);

			vision.unAlertEnemies();
		}

		private System.Collections.IEnumerator tinnitus(FMOD.Studio.EventInstance instance) {
			float time = 0;
			
			while (time < 10) {
				time += Time.deltaTime;

				instance.setVolume(1 - Mathf.Log10(time));

				yield return null;
			}

			instance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
			instance.release();

			yield break;
		}

		private void FlashBang() {
			LocalAimHandler lah = LocalAimHandler.player_instance;
				
			RuntimeTileLevelGenerator levelGenerator = RuntimeTileLevelGenerator.instance;
			if (levelGenerator != null)
			{
				ActiveTile activeTile = levelGenerator.GetActiveTile(lah.transform.position);

				List<ActiveEnemy> enemies = new List<ActiveEnemy>();
				enemies.AddRange(levelGenerator.GetActiveEnemiesInTile(activeTile.tile_position - 1));
				enemies.AddRange(levelGenerator.GetActiveEnemiesInTile(activeTile.tile_position));
				enemies.AddRange(levelGenerator.GetActiveEnemiesInTile(activeTile.tile_position + 1));

				Wolfire.ScreenEffect.Flash(Wolfire.ScreenEffect.ScreenFlashEvent.Type.Solid, Color.white, 0, 3);
				
				AudioManager.PlayOneShotAttached("event:/robots/trip_mine", gameObject, 7);
				Plugin.Instance.StartCoroutine(tinnitus(AudioManager.Play("event:/Tinnitus")));
				
				FlashbangVision vision = new FlashbangVision();
				vision.ForceSeePlayer(lah.transform.position);

				foreach(var enemy in enemies) {
					if (enemy.enemy != null && Vector3.Distance(enemy.enemy.transform.position, lah.transform.position) <= 15) {
						TurretScript turret = enemy.enemy.GetComponent<TurretScript>();
						if (turret != null && turret.camera_alive) {
							if (turret.powered_off) {
								turret.TurnOn();
							}
							else {
								turret.Alert(vision, lah);
								vision.addAlertedEnemy(turret);
							}
						}
						else {
							ShockDrone drone = enemy.enemy.GetComponent<ShockDrone>();
							if (drone != null && drone.camera_part.Alive) {
								drone.camera_part.Alert(vision, lah);
								vision.addAlertedEnemy(drone.camera_part);
							}
						}
					}
				}

				Plugin.Instance.StartCoroutine(clearAlert(vision));
			}
		}
	}
}
