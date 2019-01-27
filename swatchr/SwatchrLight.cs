using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace swatchr {
[RequireComponent(typeof(Light))]
public class SwatchrLight : SwatchrColorApplier {
	private Light swatchingLight;
	public override void Apply () {
		if (swatchingLight == null) {
			swatchingLight = GetComponent<Light>();
		}
		swatchingLight.color = swatchrColor.color;
	}
}
}
