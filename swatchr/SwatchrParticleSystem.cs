using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace swatchr {
	[RequireComponent(typeof(ParticleSystem))]
	public class SwatchrParticleSystem : SwatchrColorApplier {
		ParticleSystem swatchingParticleSystem;
		public override void Apply() {
			if (swatchingParticleSystem == null) {
				swatchingParticleSystem = GetComponent<ParticleSystem>();
			}
			var main = swatchingParticleSystem.main;
			main.startColor = swatchrColor.color;
		}
	}
}
