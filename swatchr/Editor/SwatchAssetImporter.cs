using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace swatchr {
	public class SwatchAssetImporter : AssetPostprocessor {

		// Uncomment this to automatically create swatches from .ase files in the project.
		// This is commented out because I *think* it would affect performance,
		// but I haven't confirmed that.
		/*
		  private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromPath) {
			for (int i = 0; i < importedAssets.Length; i++) {
				var fileName = importedAssets[i];
				if (fileName.EndsWith(".ase")) {
					swatchr.SwatchCreator.CreateSwatchFromASEFile(fileName);
				}
			}
		}
		*/
	}
}
