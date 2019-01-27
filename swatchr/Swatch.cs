using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SmartWeakEvent;
//https://www.codeproject.com/Articles/29922/Weak-Events-in-C#heading0002
//https://gist.github.com/dgrunwald/6445360
namespace swatchr {
	public class Swatch : ScriptableObject {
		public Color[] colors;
		public int numColors { get { return colors != null ? colors.Length : 0; } }
		
		[NonSerialized]
		private Texture2D texture = null;
		public Texture2D cachedTexture {
			get {
				if (texture == null) {
					texture = CreateTexture();
				}
				return texture;
			}
		}

		public void RegenerateTexture() {
			texture = CreateTexture();
		}

		Texture2D CreateTexture() {
			#if SWATCHR_VERBOSE
			Debug.LogWarning("[Swatch] Creating Texture");
			#endif
			
			var swatch = this;
			if (swatch.colors != null && swatch.colors.Length > 0) {
				Texture2D colorTexture = new Texture2D(swatch.colors.Length, 1);
				colorTexture.filterMode = FilterMode.Point;
				colorTexture.SetPixels(swatch.colors);
				colorTexture.Apply();
				return colorTexture;
			}
			return null;
	}
		public event EventHandler OnSwatchChanged {
			add { _event.Add(value); }
			remove { _event.Remove(value); }
		}

		[NonSerialized]
		private FastSmartWeakEvent<EventHandler> _event = new FastSmartWeakEvent<EventHandler>();

		public Color GetColor(int colorIndex) {
			if (colors == null || colors.Length <= colorIndex || colorIndex < 0) {
				return Color.white;
			}
			return colors[colorIndex];
		}

		public static Swatch FromSwatchASEFile(SwatchASEFile file) {
			var swatchScriptableObject = ScriptableObject.CreateInstance<Swatch>();
			swatchScriptableObject.colors = new Color[file.colors.Count];
			for (int i = 0; i < swatchScriptableObject.colors.Length; i++) {
				swatchScriptableObject.colors[i] = new Color(file.colors[i].r, file.colors[i].g, file.colors[i].b);
			}
			return swatchScriptableObject;
		}

		public void AddColorsFromASEFile(SwatchASEFile file) {
			int initialLength = this.colors != null ? this.colors.Length : 0;
			int fileLength = file.colors != null ? file.colors.Count : 0;
			Array.Resize<Color>(ref this.colors, initialLength + fileLength);
			int i = initialLength;
			var iterator = file.colors.GetEnumerator();
			while (iterator.MoveNext()) {
				var fileColor = iterator.Current;
				this.colors[i++] = new Color(fileColor.r, fileColor.g, fileColor.b);
			}
			SignalChange();
		}

		public void AddColorsFromOtherSwatch(Swatch otherSwatch) {
			int initialLength = this.colors != null ? this.colors.Length : 0;
			int otherSwatchLength = otherSwatch.colors != null ? otherSwatch.colors.Length : 0;
			Array.Resize<Color>(ref this.colors, initialLength + otherSwatchLength);
			int i = initialLength;
			for (int j = 0; j < otherSwatchLength; j++) { 
				this.colors[i++] = otherSwatch.colors[j];
			}
			SignalChange();
		}

		public void ReplaceSelfWithOtherSwatch(Swatch otherSwatch) {
			if (otherSwatch.colors != null) {
				Array.Resize<Color>(ref colors, otherSwatch.colors.Length);
				Array.Copy(otherSwatch.colors, colors, otherSwatch.colors.Length);
			}
			else {
				Array.Resize<Color>(ref colors, 0);
			}
			
			SignalChange();
		}

		public void SignalChange() {
			RegenerateTexture();
			_event.Raise(this, EventArgs.Empty);
			#if UNITY_EDITOR
			UnityEditor.EditorUtility.SetDirty(this);
			#endif
		}

	}
}
