using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace swatchr {
[ExecuteInEditMode]
public class SwatchrAmbientTriLightingColor : MonoBehaviour {
		[Header("Warning: This component changes scene settings in Lighting->Scene")]
		public SwatchrColor sky;
		public SwatchrColor equator;
		public SwatchrColor ground;

		void OnDestroy() {
			sky.OnColorChanged -= Apply;
			equator.OnColorChanged -= Apply;
			ground.OnColorChanged -= Apply;
		}

		void OnDisable() {
			sky.OnColorChanged -= Apply;
			equator.OnColorChanged -= Apply;
			ground.OnColorChanged -= Apply;
		}

		void OnEnable() {
			if (sky == null) {
				sky = new SwatchrColor();
			}
			if (equator == null) {
				equator = new SwatchrColor();
			}
			if (ground == null) {
				ground = new SwatchrColor();
			}
			sky.OnColorChanged += Apply;
			sky.OnEnable();

			equator.OnColorChanged += Apply;
			equator.OnEnable();

			ground.OnColorChanged += Apply;
			ground.OnEnable();
		}

		public void Apply() {
			if (RenderSettings.ambientMode != UnityEngine.Rendering.AmbientMode.Trilight) {
				Debug.LogWarning("[SwatchrAmbientTryLightingColor] RenderSettings.ambientMode != Trilight. Changing the setting to Tri Lighting. Change it manually in Lighting->Scene.");
				RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
			}
			RenderSettings.ambientSkyColor 		= sky.color;
			RenderSettings.ambientEquatorColor 	= equator.color;
			RenderSettings.ambientGroundColor 	= ground.color;
		}
}
}
