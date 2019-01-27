using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace swatchr {
	[RequireComponent(typeof(Camera))]
	public class SwatchrCameraColor : SwatchrColorApplier {
		[HideInInspector]
		public Camera swatchingCamera;
		public override void Apply() {
			if (swatchingCamera == null) {
				swatchingCamera = GetComponent<Camera>();
			}
			swatchingCamera.backgroundColor = swatchrColor.color;
		}
	}
}
