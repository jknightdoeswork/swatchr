using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace swatchr {
	[CanEditMultipleObjects]
	[CustomEditor(typeof(Swatch))]
	public class SwatchEditorGUI : Editor {
		private bool merge;
		private bool replace;
		private Swatch mergeObject;
		private Swatch replaceObject;
		private int colorRef;

		public override void OnInspectorGUI() {
			Swatch swatch = (Swatch)target;

			// Swatch
			{
				EditorGUILayout.LabelField("Swatch", EditorStyles.boldLabel);
				// if (swatch.colors != null && swatch.colors.Length > 0) {
					var startingRect = GUILayoutUtility.GetLastRect();
					if (SwatchrPaletteDrawer.DrawColorPallete(swatch, ref colorRef, true)) {
						Repaint();
					}
					
					if (swatch.numColors > 0) {
						
						var selectedColor = swatch.GetColor(colorRef);
						int selectedColorRow = colorRef / SwatchrPaletteDrawer.itemsPerRow;
						float selectedColorY = selectedColorRow * EditorGUIUtility.singleLineHeight + EditorGUIUtility.singleLineHeight;
						// EditorGUI.LabelField(colorKeyRect, ""+colorRef);
						var changeColorRect = new Rect(startingRect.x + SwatchrPaletteDrawer.itemsPerRow * EditorGUIUtility.singleLineHeight + 30, startingRect.y + selectedColorY, 64, EditorGUIUtility.singleLineHeight);
						
						EditorGUI.BeginChangeCheck();
						var newColor = EditorGUI.ColorField(changeColorRect, selectedColor);
						if (EditorGUI.EndChangeCheck()) {
							swatch.colors[colorRef] = newColor;
							swatch.SignalChange();
							GameViewRepaint();
						}
						int x = (int)(changeColorRect.x + changeColorRect.width + 2);
						int y = (int)(changeColorRect.y + changeColorRect.height - EditorGUIUtility.singleLineHeight);
						if (SwatchrPaletteDrawer.DrawDeleteButton(x, y)) {
							if (colorRef + 1 < swatch.colors.Length) {
								Array.Copy(swatch.colors, colorRef+1, swatch.colors, colorRef, swatch.colors.Length-colorRef-1);
							}
							Array.Resize<Color>(ref swatch.colors, swatch.colors.Length-1);
							if (colorRef >= swatch.colors.Length) {
								colorRef = swatch.colors.Length - 1;
								if (colorRef < 0) {
									colorRef = 0;
								}
							}
							swatch.SignalChange();
							GameViewRepaint();
						}
					}
				//}
			}

			// Add
			{
				EditorGUILayout.LabelField("Add", EditorStyles.boldLabel);
				if (GUILayout.Button("Add .ASE")) {
					var path = EditorUtility.OpenFilePanel("Swatchr Import", "", "ase");
					if (path != null && path != string.Empty) {
						Debug.Log("[SwatchEditorGUI] path " + path);
						SwatchASEFile aseFile = new SwatchASEFile(path);
						swatch.AddColorsFromASEFile(aseFile);
						GameViewRepaint();
					}
				}

				if (GUILayout.Button("Add .ASE Folder")) {
					var path = EditorUtility.OpenFolderPanel("Swatchr Folder Import", "", "");
					//var path = EditorUtility.OpenFilePanel("Import", "", "ase");
					if (path != null && path != string.Empty) {
						var files = Directory.GetFiles(path);
						for (int i = 0; i < files.Length; i++) {
							string file = files[i];
							if (file.EndsWith(".ase")) {
								SwatchASEFile aseFile = new SwatchASEFile(file);
								swatch.AddColorsFromASEFile(aseFile);
								GameViewRepaint();
							}
						}
					}
				}

				if (GUILayout.Button("Add Texture")) {
					var path = EditorUtility.OpenFilePanel("Swatchr Import Texture", "", "png");
					if (path != null && path != string.Empty) {
						Debug.Log("[SwatchEditorGUI] importing texture at path " + path);
						
						var bytes = File.ReadAllBytes(path);
						var tex = new Texture2D(1,1);
						tex.LoadImage(bytes);
						var pixels = tex.GetPixels();
						if (pixels != null && pixels.Length > 0) {
							//int i = swatch.colors.Length;
							int i = 0;
							Array.Resize<Color>(ref swatch.colors, pixels.Length);
							for (int j = 0; j < pixels.Length; j++) { 
								swatch.colors[i++] = pixels[j];
							}
							swatch.SignalChange();
							GameViewRepaint();
						}
					}
				}
			}

			// Replace
			{
				EditorGUILayout.LabelField("Replace", EditorStyles.boldLabel);
				if (replace) {
					// Object Field
					replaceObject = (Swatch)EditorGUILayout.ObjectField(replaceObject, typeof(Swatch), false);
					// Confirm
					EditorGUI.BeginDisabledGroup(replaceObject == null);
					if (GUILayout.Button("Replace")) {
						swatch.ReplaceSelfWithOtherSwatch(replaceObject);
						GameViewRepaint();
						replaceObject = null;
						//replace = false;
					}
					EditorGUI.EndDisabledGroup();
				}
				// Start & Cancel
				if (GUILayout.Button(replace ? "Cancel" : "Replace")) {
					replace = !replace;
					replaceObject = null;
				}
			}

			// Merge
			{
				EditorGUILayout.LabelField("Merge", EditorStyles.boldLabel);
				if (merge) {
					// Object Field
					mergeObject = (Swatch)EditorGUILayout.ObjectField(mergeObject, typeof(Swatch), false);
					// Confirm
					EditorGUI.BeginDisabledGroup(mergeObject == null);
					if (GUILayout.Button("Merge")) {
						swatch.AddColorsFromOtherSwatch(mergeObject);
						GameViewRepaint();
						mergeObject = null;
						merge = false;
					}
					EditorGUI.EndDisabledGroup();
				}
				// Start & Cancel
				if (GUILayout.Button(merge ? "Cancel" : "Merge")) {
					mergeObject = null;
					merge = !merge;
				}
			}
			
			// Export
			{
				EditorGUILayout.LabelField("Export", EditorStyles.boldLabel);
				if (GUILayout.Button("Export To Color Picker Presets")) {
					SwatchPresetExporter.ExportToColorPresetLibrary(swatch);
				}
				if (GUILayout.Button("Export To Texture")) {
					SwatchCreator.ExportSwatchToTexture();
				}
				EditorGUILayout.Space();
			}

			// Save
			if (GUILayout.Button("Save")) {
				EditorUtility.SetDirty(swatch);
				AssetDatabase.SaveAssets();
			}
		}

		string[] otherSwatchGUIDs;
		string[] otherSwatchFilenames;

		void LoadOtherSwatches() {
			var swatchGUIDs = AssetDatabase.FindAssets("t:Swatch");
			var selfGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(target.GetInstanceID()));
			int iterator = 0;
			int numOtherSwatches = swatchGUIDs.Length - 1;
			otherSwatchGUIDs = new string[numOtherSwatches];
			otherSwatchFilenames = new string[numOtherSwatches];
			for (int i = 0; i < swatchGUIDs.Length; i++) {
				if (swatchGUIDs[i].Equals(selfGUID)) {
					continue;
				}
				var swatchPath = AssetDatabase.GUIDToAssetPath(swatchGUIDs[i]);
				var swatchName = System.IO.Path.GetFileNameWithoutExtension(swatchPath);

				otherSwatchGUIDs[iterator] = swatchGUIDs[i];
				otherSwatchFilenames[iterator++] = swatchName;
			}
		}

		static EditorWindow gameview;
		public static void GameViewRepaint() {
			if (gameview == null) {
				System.Reflection.Assembly assembly = typeof(UnityEditor.EditorWindow).Assembly;
				System.Type type = assembly.GetType("UnityEditor.GameView");
				gameview = EditorWindow.GetWindow(type);
			}
			if (gameview != null) {
				gameview.Repaint();
			}
		}
	}
}