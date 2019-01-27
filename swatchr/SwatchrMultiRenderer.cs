using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace swatchr {
	public class SwatchrMultiRenderer : SwatchrColorApplier {
		public Renderer[] renderers;

		static MaterialPropertyBlock mpb;
		static int colorShaderId;
		// you need a fake public field for this attribute to work
		// string is method name, field name is button text
		[Zaikman.InspectorButtonAttribute("GatherChildren")]
		public bool GatherChildRenderers;
		public void GatherChildren() {
			renderers = GetComponentsInChildren<Renderer>();
		}
		public override void Apply() {
			if (mpb == null) {
				mpb				= new MaterialPropertyBlock();
				colorShaderId	= Shader.PropertyToID("_Color"); 
			}
			mpb.SetColor(colorShaderId, swatchrColor.color);
			if (renderers != null) {
				for (int i = 0; i < renderers.Length; i++) {
					renderers[i].SetPropertyBlock(mpb);
				}
			}
		}
	}
}