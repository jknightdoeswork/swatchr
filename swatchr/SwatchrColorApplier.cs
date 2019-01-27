using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace swatchr {
	[ExecuteInEditMode]
	public abstract class SwatchrColorApplier : MonoBehaviour {
		public SwatchrColor swatchrColor;

		void OnDestroy() {
			swatchrColor.OnColorChanged -= Apply;
		}

		void OnDisable() {
			swatchrColor.OnColorChanged -= Apply;
		}

		void OnEnable() {
			if (swatchrColor == null) {
				swatchrColor = new SwatchrColor();
			}
			swatchrColor.OnColorChanged += Apply;
			swatchrColor.OnEnable();
		}

		public abstract void Apply();
	}
}
