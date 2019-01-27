using System;
using UnityEngine;

namespace swatchr {
	// SwatchrColor
	//  Holds a Swatch and an index into that Swatch.
	//  Returns a color using the color index and the swatch.
	//  Implements the "Observer Pattern" allowing classes to listen
	//  to changes in the swatch or colorIndex.
	//  Calls OnColorChanged when the swatch or the colorIndex has changed.
	//  In order to react to changes within the swatch, we need to 
	//  subscribe to OnSwatchChanged events.
	//  We use a "weak delegate" pattern so that we never have to
	//  un register for the event. This is useful because this class
	//  cannot simply deregister in the destructor because the destructor
	//  won't get called if we use a regular event subscription pattern.
	//  More info on weak delegates can be found here:
	//  https://www.codeproject.com/Articles/29922/Weak-Events-in-C#heading0002
	[Serializable]
	public class SwatchrColor {
		public SwatchrColor() {
		}
		public void OnEnable() {
			swatch = _swatch; // this will subscribe to changes in the swatch, and call OnColorChanged
		}
		public void OnDisable() {
			if (_swatch != null)
				_swatch.OnSwatchChanged -= OnSwatchChanged;
		}
		void OnSwatchChanged(object sender, EventArgs e) {
			if (OnColorChanged != null)
				OnColorChanged();
		}

		public Swatch swatch {
			get {return _swatch;}
			set {
				if (_swatch != null)
					_swatch.OnSwatchChanged -= OnSwatchChanged;
				_swatch = value;
				if (_swatch != null)
					_swatch.OnSwatchChanged += OnSwatchChanged;
				if (OnColorChanged != null)
					OnColorChanged();
			}
		}

		public int colorIndex {
			get {return _colorIndex;}
			set {_colorIndex = value;
				if (OnColorChanged != null) OnColorChanged();
			}
		}
		
		public Color color {
			get {
				if (swatch == null || swatch.colors == null || swatch.colors.Length <= colorIndex || colorIndex < 0) {
					return _overrideColor;
				}
				return swatch.colors[colorIndex];
			}
		}

		[SerializeField]
		public Swatch _swatch;
		[SerializeField]
		public int _colorIndex;
		[SerializeField]
		public Color _overrideColor;

		public event Action OnColorChanged;
	}
}