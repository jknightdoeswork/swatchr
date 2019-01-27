using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace swatchr {
	public class SwatchrPaletteDrawer {
		public SwatchrPaletteDrawer() {
		}
		public const int itemsPerRow = 8;
		public static bool DrawColorPallete(Swatch swatch, ref int colorKey, bool drawNewColorButton) {
			if (swatch == null) {
				return false;
			}

			var lastRect = GUILayoutUtility.GetLastRect();

			if (swatch.colors != null && swatch.colors.Length > 0) {
				int swatchHash = swatch.cachedTexture.GetHashCode();
				if (palleteTexture == null || palleteTextureCachedHashCode != swatchHash) {
					if (palleteTexture == null) {
						#if SWATCHR_VERBOSE
						Debug.LogWarning("[SwatchrPalleteDrawer] creating pallete texture because there is none");
						#endif
					}
					else {
						#if SWATCHR_VERBOSE
						Debug.LogWarningFormat("[SwatchrPalleteDrawer] creating pallete texture because cache miss. {0} != {1}", palleteTextureCachedHashCode, swatchHash);
						#endif
					}
					palleteTexture = textureWithColors(swatch.colors);
					palleteTextureCachedHashCode = swatchHash;
				}
			}
			else {
				palleteTexture = null;
			}

			if (blackTexture == null) {
				#if SWATCHR_VERBOSE
				Debug.LogWarning("[SwatchrPalleteDrawer] creating black texture");
				#endif
				blackTexture = textureWithColor(Color.black);
			}
			if (whiteTexture == null) {
				#if SWATCHR_VERBOSE
				Debug.LogWarning("[SwatchrPalleteDrawer] creating white texture");
				#endif
				whiteTexture = textureWithColor(Color.white);
			}

			int numColors = swatch.colors != null ? swatch.colors.Length : 0;
			int numPerRow = itemsPerRow;
			int numInBottomRow = numColors % numPerRow;
			
			float heightOfPallete = 0;
			var textureRect = new Rect(lastRect.x, lastRect.y + lastRect.height, 0.0f,0.0f);
			if (palleteTexture != null) {
				textureRect = new Rect(lastRect.x, lastRect.y + lastRect.height, palleteTexture.width * EditorGUIUtility.singleLineHeight, palleteTexture.height * EditorGUIUtility.singleLineHeight);
				heightOfPallete = textureRect.height;
			}

			if (numInBottomRow == 0) {
				heightOfPallete += EditorGUIUtility.singleLineHeight;
			}
			
			Rect clickRect = textureRect;
			if (swatch.colors == null || swatch.colors.Length == 0) {
				clickRect.width = EditorGUIUtility.singleLineHeight;
			}
			clickRect.height = heightOfPallete;

			GUILayoutUtility.GetRect(clickRect.width, clickRect.height);
			if (palleteTexture != null) {
				DrawTexture(palleteTexture, textureRect);
				DrawBlackGrid(textureRect.x, textureRect.y, swatch.colors.Length, palleteTexture.width, palleteTexture.height, (int)EditorGUIUtility.singleLineHeight, blackTexture);
			}

			if (drawNewColorButton) {
				DrawNewColorButton(numColors, textureRect);
			}

			bool somethingHasChanged = false;
			if (IsClick()) {
				if (IsClickInRect(clickRect)) {
					var e  = Event.current;
					Vector2 rectClickPosition = e.mousePosition - textureRect.position;
					int cellXIndex = (int)(rectClickPosition.x / EditorGUIUtility.singleLineHeight);
					int cellYIndex = (int)(rectClickPosition.y / EditorGUIUtility.singleLineHeight);
					int textureWidth = palleteTexture != null ? palleteTexture.width : 0;
					int clickedOnKey = cellYIndex * textureWidth + cellXIndex;
					if (numColors > 0 && clickedOnKey < numColors) {
						colorKey = clickedOnKey;
						somethingHasChanged = true;
					}
					else if (clickedOnKey == numColors) {
						colorKey = clickedOnKey;
						System.Array.Resize(ref swatch.colors, numColors + 1);
						swatch.colors[colorKey] = Color.white;
						swatch.SignalChange();
						somethingHasChanged = true;
					}
					else {

					}
				}
			}

			if (swatch.colors != null && swatch.colors.Length > 0) {
				DrawOnSelectedCell(colorKey, textureRect);
				int selectedColorRow = colorKey / SwatchrPaletteDrawer.itemsPerRow;
				float selectedColorY = selectedColorRow * EditorGUIUtility.singleLineHeight + EditorGUIUtility.singleLineHeight;
				var colorKeyRect = new Rect(lastRect.x + SwatchrPaletteDrawer.itemsPerRow * EditorGUIUtility.singleLineHeight, lastRect.y + selectedColorY, 64, EditorGUIUtility.singleLineHeight);
				EditorGUI.LabelField(colorKeyRect, colorKey.ToString());
			}

			return somethingHasChanged;
		}

		public static bool DrawDeleteButton(int x, int y) {
			int slh = (int)(EditorGUIUtility.singleLineHeight);
			int slhHalf = (int)(slh * 0.5f);
			if (blackTexture == null) {
				blackTexture = textureWithColor(Color.black);
			}

			DrawBlackGrid(x, y, 1, 1, 1, slh, blackTexture);
			DrawBlackGrid(x + 1, y + 1, 1, 1, 1, slh - 2, whiteTexture);

			int minusLength = 7;
			int halfMinusLength = 3;

			var minusRect = new Rect(x + slhHalf - halfMinusLength, y + slhHalf, minusLength, 1);
			DrawTexture(blackTexture, minusRect);
			var clickRect = new Rect(x, y, slh, slh);
			if (IsClick()) {
				if (IsClickInRect(clickRect)) {
					return true;
				}
			}

			return false;
		}

		public static Rect GetNewColorButtonRect(Swatch swatch) {
			int numColors = swatch.colors.Length;
			int totalRows = Mathf.CeilToInt(numColors / (float)itemsPerRow);
			int numInBottomRow = numColors % itemsPerRow;
			Rect r = new Rect();
			r.x = (int)(numInBottomRow * EditorGUIUtility.singleLineHeight);
			r.y = (int)(totalRows * EditorGUIUtility.singleLineHeight);
			r.width = EditorGUIUtility.singleLineHeight;
			r.height = EditorGUIUtility.singleLineHeight;
			return r;
		}

		static void DrawBlackGrid(float startingPointX, float startingPointY, int numColors, int cellsX, int cellsY, int cellSize, Texture2D colorTexture) {
			if (cellsX == 0 && cellsY == 0) {
				return;
			}
			// draw vertical lines
			Rect currentRect = new Rect(startingPointX, startingPointY, 1, cellSize * cellsY);
			int fullHeight = cellSize*cellsY + 1; // +1 to get the corners
			int oneLessHeight = cellSize* (cellsY-1) + 1;
			// oneLessHeight will be 1 if theres only one row
			if (cellsY == 1) {
				oneLessHeight = 0;
			}
			int numInBottomRow = numColors % cellsX;
			for (int i = 0; i <= cellsX; i++) {
				// height will be 1 unit shorter if bottom cell does not exist
				currentRect.x = startingPointX + cellSize * i;
				bool bottomCellExists = numInBottomRow == 0 || i <= numInBottomRow;
				currentRect.height = bottomCellExists ? fullHeight : oneLessHeight;
				DrawTexture(colorTexture, currentRect);
			}

			// draw horizontal lines
			currentRect.x = startingPointX;
			currentRect.height = 1;
			currentRect.width = cellSize * cellsX;
			for (int i = 0; i <= cellsY; i++) {
				currentRect.y = startingPointY + cellSize * i;
				if ((i == cellsY || cellsY == 1) && numInBottomRow > 0) {
					currentRect.width = numInBottomRow*cellSize;
				}
				DrawTexture(colorTexture, currentRect);
			}
		}

		static void DrawOnSelectedCell(int selectedCell, Rect textureRect) {
			int selectedCellY = selectedCell / itemsPerRow;
			int selectedCellX = selectedCell - (itemsPerRow * selectedCellY);
			Rect smallBlackRect = new Rect(textureRect.x + selectedCellX * EditorGUIUtility.singleLineHeight, textureRect.y + selectedCellY * EditorGUIUtility.singleLineHeight, 10f, 10f);
			DrawBlackGrid(smallBlackRect.x-1, smallBlackRect.y-1, 1, 1, 1, (int)(EditorGUIUtility.singleLineHeight) + 2, blackTexture);
			DrawBlackGrid(smallBlackRect.x, smallBlackRect.y, 1, 1, 1, (int)(EditorGUIUtility.singleLineHeight), whiteTexture);
		}

		static void DrawNewColorButton(int selectedCell, Rect textureRect) {
			int selectedCellY 			= selectedCell / itemsPerRow;
			int selectedCellX 			= selectedCell - (itemsPerRow * selectedCellY);
			Rect smallBlackRect 		= new Rect(textureRect.x + selectedCellX * EditorGUIUtility.singleLineHeight, textureRect.y + selectedCellY * EditorGUIUtility.singleLineHeight, EditorGUIUtility.singleLineHeight, EditorGUIUtility.singleLineHeight);
			DrawBlackGrid(smallBlackRect.x, smallBlackRect.y, 1, 1, 1, (int)(EditorGUIUtility.singleLineHeight), blackTexture);
			DrawBlackGrid(smallBlackRect.x + 1, smallBlackRect.y + 1, 1, 1, 1, (int)(EditorGUIUtility.singleLineHeight-2), whiteTexture);

			int plusLength 				= 7;
			float halfPlusLength		= 3.0f;
			
			float centerX 				= smallBlackRect.x + smallBlackRect.width*0.5f;
			float centerY				= smallBlackRect.y + smallBlackRect.height*0.5f;
			Rect plusVerticalRect 		= new Rect(centerX, centerY - halfPlusLength, 1, plusLength);
			Rect plusHorizontalRect 	= new Rect(centerX - halfPlusLength, centerY , plusLength, 1);
			DrawTexture(blackTexture, plusVerticalRect);
			DrawTexture(blackTexture, plusHorizontalRect);
		}

		static GUIStyle tempDrawTextureStyle;
		static Texture2D blackTexture;
		static Texture2D whiteTexture;
		static Texture2D palleteTexture;
		static int palleteTextureCachedHashCode;
		static void DrawTexture(Texture2D texture, Rect rect) {
			if (tempDrawTextureStyle == null) {
				tempDrawTextureStyle = new GUIStyle();
			}
			tempDrawTextureStyle.normal.background = texture;
			EditorGUI.LabelField(rect, "", tempDrawTextureStyle);
		}

		static bool IsClick() {
			Event e = Event.current;
			return e != null && e.type == EventType.MouseDown && e.button == 0;
		}

		static bool IsClickInRect(Rect rect) {
			Event e = Event.current;
			return e != null && e.type == EventType.MouseDown && e.button == 0 && rect.Contains(e.mousePosition);
		}
		static Texture2D textureWithColor(Color color) {
			var tex = new Texture2D(1, 1, TextureFormat.RGB24, false, true);
			tex.filterMode = FilterMode.Point;
			tex.wrapMode = TextureWrapMode.Clamp;
			tex.hideFlags = HideFlags.HideAndDontSave;
			tex.SetPixel(0, 0, color);
			tex.Apply();
			return tex;
		}

		static Texture2D textureWithColors(Color[] colors) {
			if (colors == null || colors.Length == 0) {
				return new Texture2D(0, 0, TextureFormat.RGBA32, false, true);
			}
			// figure out our texture size based on the itemsPerRow and color count
			int totalRows = Mathf.CeilToInt((float)colors.Length / (float)itemsPerRow);
			var tex = new Texture2D(itemsPerRow, totalRows, TextureFormat.RGBA32, false, true);
			tex.filterMode = FilterMode.Point;
			tex.wrapMode = TextureWrapMode.Clamp;
			tex.hideFlags = HideFlags.HideAndDontSave;
			int x = 0;
			int y = 0;
			for (int i = 0; i < colors.Length; i++) {
				x = i % itemsPerRow;
				y = totalRows - 1 - Mathf.CeilToInt(i / itemsPerRow);
				tex.SetPixel(x, y, colors[i]);
			}
			for (x++; x < tex.width; x++) {
				tex.SetPixel(x, y, Color.clear);
			}

			tex.Apply();

			return tex;
		}
	}
}