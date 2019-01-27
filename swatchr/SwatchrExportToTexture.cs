using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace swatchr {
public static class SwatchrExportToTexture {
	public static void ExportSwatchToTexture(Swatch selectedSwatch, string fullSaveLocation) {
		Texture2D swatchrTexture = selectedSwatch.cachedTexture;
		byte[] pngBytes = swatchrTexture.EncodeToPNG();
		Debug.Log("[SwatchrExportToTexture] exporting swatch to " + fullSaveLocation);
		File.WriteAllBytes(fullSaveLocation, pngBytes);
	}

}
}