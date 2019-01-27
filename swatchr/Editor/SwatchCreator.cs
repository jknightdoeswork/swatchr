using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace swatchr {
	public static class SwatchCreator {

		[MenuItem("Assets/Swatchr/Create New Swatch")]
		public static void CreateSwatch() {
			Swatch asset = ScriptableObject.CreateInstance<Swatch>();
			ProjectWindowUtil.CreateAsset(asset, "New Swatch.asset");
			AssetDatabase.SaveAssets();
		}

		public static Swatch CreateSwatchFromASEFile(SwatchASEFile aseFile, string projectSaveDestination) {
			Swatch swatch = Swatch.FromSwatchASEFile(aseFile);

			projectSaveDestination = AssetDatabase.GenerateUniqueAssetPath(projectSaveDestination);
			AssetDatabase.CreateAsset(swatch, projectSaveDestination);
			AssetDatabase.SaveAssets();

			EditorUtility.FocusProjectWindow();
			Selection.activeObject = swatch;
			return swatch;
		}

		[MenuItem("Assets/Swatchr/Duplicate Swatch")]
		public static void DuplicateSwatch() {
			var activeObject = (Swatch)Selection.activeObject;
			Swatch asset = ScriptableObject.CreateInstance<Swatch>();
			asset.AddColorsFromOtherSwatch(activeObject);
			ProjectWindowUtil.CreateAsset(asset, activeObject.name+".asset");
		}
		[MenuItem("Assets/Swatchr/Duplicate Swatch", true)]
		public static bool ValidateDuplicateSwatch() {
			var activeObject = Selection.activeObject;
			return activeObject != null && activeObject is Swatch;
		}

		[MenuItem("Assets/Swatchr/Export Swatch To Color Presets")]
		public static void ExportToPresets() {
			var activeObject = (Swatch)Selection.activeObject;
			SwatchPresetExporter.ExportToColorPresetLibrary(activeObject);
		}

		[MenuItem("Assets/Swatchr/Export Swatch To Color Presets", true)]
		public static bool ValidateExportToPresets() {
			var activeObject = Selection.activeObject;
			return activeObject != null && activeObject is Swatch;
		}

		[MenuItem("Assets/Swatchr/Import ASE File")]
		static void ImportSelectedASEFile() {
			var activeObject = Selection.activeObject;
			var path = AssetDatabase.GetAssetPath(activeObject.GetInstanceID());
			var fullPath = path.Replace("Assets", Application.dataPath);
			var saveDestination = path.Replace(".ase", ".asset");

			var aseFile = new SwatchASEFile(fullPath);
			CreateSwatchFromASEFile(aseFile, saveDestination);
		}

		[MenuItem("Assets/Swatchr/Import ASE File", true)]
		static bool ValidateImportSelectedASEFile() {
			var activeObject = Selection.activeObject;
			if (activeObject == null) {
				return false;
			}
			var path = AssetDatabase.GetAssetPath(activeObject.GetInstanceID());
			return path.EndsWith(".ase");
		}

		[MenuItem("Assets/Swatchr/Import ASE File (Browse...)")]
		static void ImportASEFileBrowse() {
			var path = EditorUtility.OpenFilePanel("Swatchr Import", "", "ase");
			if (path != null && path != string.Empty) {
				SwatchASEFile aseFile = new SwatchASEFile(path);
				CreateSwatchFromASEFile(aseFile, GetSelectedSavePath(aseFile.title));
			}
		}

		[MenuItem("Assets/Swatchr/Import ASE Folder Into One Swatch (Browse...)")]
		static void ImportASEFolderIntoOne() {
			var path = EditorUtility.OpenFolderPanel("Swatchr Import Folder", "", "");
			if (path != null && path != string.Empty) {
				var files = Directory.GetFiles(path);
				Swatch parentSwatch = null;
				for (int i = 0; i < files.Length; i++) {
					string file = files[i];
					if (file.EndsWith(".ase")) {
						SwatchASEFile aseFile = new SwatchASEFile(file);
						if (parentSwatch == null) {
							parentSwatch = CreateSwatchFromASEFile(aseFile, GetSelectedSavePath(aseFile.title));
						}
						else {
							parentSwatch.AddColorsFromASEFile(aseFile);
						}
					}
				}
			}
		}

		[MenuItem("Assets/Swatchr/Import ASE Folder Into Seperate Swatches (Browse...)")]
		static void ImportASEFolderIntoMany() {
			var path = EditorUtility.OpenFolderPanel("Swatchr Import Folder", "", "");
			if (path != null && path != string.Empty) {
				var files = Directory.GetFiles(path);
				for (int i = 0; i < files.Length; i++) {
					string file = files[i];
					if (file.EndsWith(".ase")) {
						SwatchASEFile aseFile = new SwatchASEFile(file);
						CreateSwatchFromASEFile(aseFile, GetSelectedSavePath(aseFile.title));
					}
				}
			}
		}
		
		[MenuItem("Assets/Swatchr/Import Swatch From Texture (Browse...)")]
		public static void ImportSwatchFromTexture() {
			var path = EditorUtility.OpenFilePanel("Swatchr Import Texture", "", "png");
			if (path != null && path != string.Empty) {
				Debug.Log("[SwatchEditorGUI] importing texture at path " + path);
				var bytes = File.ReadAllBytes(path);
				var tex = new Texture2D(1,1);
				tex.LoadImage(bytes);
				var pixels = tex.GetPixels();
				if (pixels != null && pixels.Length > 0) {
					var swatch = ScriptableObject.CreateInstance<Swatch>();
					System.Array.Resize<Color>(ref swatch.colors, pixels.Length);
					for (int j = 0; j < pixels.Length; j++) { 
						swatch.colors[j] = pixels[j];
					}
					ProjectWindowUtil.CreateAsset(swatch, "New Swatch.asset");
					AssetDatabase.SaveAssets();
				}
			}
		}

		[MenuItem("Assets/Swatchr/Export Swatch To Texture")]
		public static void ExportSwatchToTexture() {
			var selectedSwatch = (Swatch)Selection.activeObject;
			var assetLocation  = GetSelectedPath() + "/" + GetSelectedFileName() + ".png";
			string saveLocation = ConvertAssetPathToFullPath(assetLocation);
			SwatchrExportToTexture.ExportSwatchToTexture(selectedSwatch, saveLocation);
			UnityEditor.AssetDatabase.ImportAsset(assetLocation);
		}

		[MenuItem("Assets/Swatchr/Export Swatch To Texture", true)]
		public static bool ExportSwatchToTexture_Validate() {
			var activeObject = Selection.activeObject;
			return activeObject != null && activeObject is Swatch;
		}

		static string GetSelectedSavePath(string title) {
			return AssetDatabase.GenerateUniqueAssetPath(GetSelectedPath() + "/" + title + ".asset");
		}
		static string GetSelectedFileName() {
			return Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(Selection.activeObject));
		}
		static string GetSelectedPath() {
			string path = AssetDatabase.GetAssetPath(Selection.activeObject);
			if (path == "") {
				path = "Assets";
			}
			else if (Path.GetExtension(path) != "") {
				path = path.Replace(Path.GetFileName(path), "");
			}
			return path;
		}

		public static string ConvertAssetPathToFullPath(string assetPath) {
			var fullPath = Application.dataPath + assetPath.Substring("Assets".Length);
			return fullPath;
		}
	}
}
