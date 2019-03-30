using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
namespace swatchr {
	// SwatchPresetExporter
	// This class exports a swatch to the unity color picker.
	// It does this by writing a YAML file.
	// Total hack! I'm a wizard!
	public class SwatchPresetExporter {

		public const string COLORS_TEMPLATE = "%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!114 &1\nMonoBehaviour:\n  m_ObjectHideFlags: 52\n  m_PrefabParentObject: {fileID: 0}\n  m_PrefabInternal: {fileID: 0}\n  m_GameObject: {fileID: 0}\n  m_Enabled: 1\n  m_EditorHideFlags: 1\n  m_Script: {fileID: 12323, guid: 0000000000000000e000000000000000, type: 0}\n  m_Name: \n  m_EditorClassIdentifier: \n  m_Presets:";
		/*
		- m_Name: 
			m_Color: {r: 0.18616219, g: 0.05374134, b: 0.52205884, a: 1}
		- m_Name: 
			m_Color: {r: 0.31163496, g: 0.45094258, b: 0.9632353, a: 1}
		*/

		public static void ExportToColorPresetLibrary(Swatch swatch) {

			var swatchProjectPath = AssetDatabase.GetAssetPath(swatch.GetInstanceID());
			var swatchDirectory = Path.GetDirectoryName(swatchProjectPath);
			var libraryDirectory = swatchDirectory + "/Editor";

			if (!AssetDatabase.IsValidFolder(libraryDirectory)) {
				AssetDatabase.CreateFolder(swatchDirectory, "Editor");
			}
			
			var libraryProjectpath = libraryDirectory + "/" + swatch.name + ".colors";
			var fullFileName = libraryProjectpath.Replace("Assets", Application.dataPath);
			string fileText = COLORS_TEMPLATE;
			for (int i = 0; i < swatch.colors.Length; i++) {
				fileText += GetYAMLForColor(swatch.colors[i]);
			}
			Debug.Log("[SwatchPresetExporter] writing to " + fullFileName);
			File.WriteAllText(fullFileName, fileText);
			AssetDatabase.ImportAsset(libraryProjectpath);
		}

		public static string GetYAMLForColor(Color c) {
			return string.Format(System.Globalization.CultureInfo.InvariantCulture, "\n  - m_Name: \n    m_Color: {{r: {0}, g: {1}, b: {2}, a: {3}}}", c.r, c.g, c.b, c.a);
		}
	}

}
