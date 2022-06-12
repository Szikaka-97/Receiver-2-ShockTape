using System;
using System.Collections.Generic;
using UnityEngine;
using Receiver2;

namespace ShockTape {
	class FlashbangVision : StochasticVision {

		private List<IAlertable> alertedEnemies = new();

		new public void Update(Vector3 start, LocalAimHandler lah, StochasticVision.VisionCone vision_cone = null, float max_distance = -1f, bool use_ignore_vision = false) {
			this.last_spotted_point = lah.RandomPointInCollider(0.1f);
			this.last_spotted_velocity = lah.GetVelocity();
			this.consecutive_blocked = 0;
			this.can_see_player = true;
			this.visible_percentage = Mathf.MoveTowards(this.visible_percentage, 1f, 1.2f * Time.deltaTime);
			this.can_see_player_this_frame = true;
		}

		public void addAlertedEnemy(IAlertable enemy) {
			alertedEnemies.Add(enemy);
		}

		public void unAlertEnemies() {
			foreach(var enemy in alertedEnemies) {
				enemy.UnAlert();
			}
		}
	}
}
