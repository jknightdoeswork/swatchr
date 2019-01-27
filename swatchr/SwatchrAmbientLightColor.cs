using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using swatchr;
namespace swatchr {
public class SwatchrAmbientLightColor : SwatchrColorApplier {


	public override void Apply () {
		if (RenderSettings.ambientMode != UnityEngine.Rendering.AmbientMode.Flat) {
			Debug.LogWarning("[SwatchrAmbientLightColor] Ambient light mode is not set to flat. Changing it now. Change it manually in Lighting->Scene.");
			RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
		}
		RenderSettings.ambientLight = swatchrColor.color;
	}
}
}