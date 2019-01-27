using System;
using System.IO;
using System.Collections.Generic;

namespace swatchr {
	public class SwatchASEFile {
		#pragma warning disable 0219
		bool hasWarnedCMYK = false;
		bool hasWarnedLAB = false;
		public class FloatThree {
			public float r,g,b;
			public FloatThree(float r, float g, float b) {
				this.r = r;
				this.g = g;
				this.b = b;
			}
		}

		public string title;
		public List<FloatThree> colors;

		public SwatchASEFile(string filePath)
			: this(File.ReadAllBytes(filePath)) {
		}

		public SwatchASEFile(byte[] bytes) {
			var byteReader = new BinaryReader(new MemoryStream(bytes, false), System.Text.Encoding.UTF32);

			char[] header = byteReader.ReadChars(4);
			int v_major = ReadBigEndianInt16(byteReader);
			int v_minor = ReadBigEndianInt16(byteReader);
			int chunk_count = ReadBigEndianInt32(byteReader);

			if (header[0] != 'A' || header[1] != 'S' || header[2] != 'E' || header[3] != 'F') {
				UnityEngine.Debug.LogErrorFormat("[SwatchASEFile] File header did not match ASEF {0}{1}{2}{3}", header[0],header[1],header[2],header[3]);
			}
			// assert header == "A" "S" "E" "F"

			colors = new List<FloatThree>();


			while (byteReader.BaseStream.Position != byteReader.BaseStream.Length) {
				byte b1 = byteReader.ReadByte();
				byte b2 = byteReader.ReadByte();
				if (b1 == 0x00 && b2 == 0x01) {
					FloatThree rgb;
					DictForChunk(byteReader, out title, out rgb);
					colors.Add(rgb);
				} else if (b1 == 0xC0 && b2 == 0x01) {
					FloatThree rgb;
					DictForChunk(byteReader, out title, out rgb);
					ParseSwatch(byteReader, colors);
				} else if (b1 == 0xC0 && b2 == 0x02) {
					byteReader.ReadBytes(4);
					// assert fd.read(4) == b'\x00\x00\x00\x00'
				} else {
					// the file is malformed?
					//Debug.Log("[Swatch] ParseASEFile got malformed ase file");
				}
			}
		}


		public void ParseSwatch(BinaryReader byteReader, List<FloatThree> colors) {
			
			byte b1 = byteReader.ReadByte();
			byte b2 = byteReader.ReadByte();
			while (b1 == 0x00 && (b2 == 0x01 || b2 == 0x02)) {
				string name;
				FloatThree color;

				DictForChunk(byteReader, out name, out color);
				colors.Add(color);
				b1 = byteReader.ReadByte();
				b2 = byteReader.ReadByte();
			}
			byteReader.BaseStream.Seek(-2, SeekOrigin.Current);
		}

		public void DictForChunk(BinaryReader byteReader, out string name, out FloatThree rgb) {
			var chunk_length = ReadBigEndianInt32(byteReader);
			byte[] bytes = byteReader.ReadBytes(chunk_length);

			int title_length = 2 * (bytes[0] << 8 | bytes[1]);
			name = ReadBigEndianU16String(bytes, 2, title_length).Trim('\0');
			int color_data_start = 2 + title_length;
			if (color_data_start < chunk_length) {
				char[] colorMode = new char[] {
					(char)bytes[color_data_start],
					(char)bytes[color_data_start + 1],
					(char)bytes[color_data_start + 2],
					(char)bytes[color_data_start + 3]
				};
				string colorModeStr = new string(colorMode).Trim().Trim('\0');
				switch (colorModeStr) {
				case "RGB": {
					float r = ReadBigEndianFloat(bytes, color_data_start+4);
					float g = ReadBigEndianFloat(bytes, color_data_start+8);
					float b = ReadBigEndianFloat(bytes, color_data_start+12);
					short swatch_type = ReadBigEndianInt16(bytes, color_data_start+16);
					rgb = new FloatThree(r,g,b);
					break;
				}
				case "CMYK": {
					if (!hasWarnedCMYK) {
						UnityEngine.Debug.LogWarning("[SwatchASEFile] CMYK Color conversion ignores color space and won't be that good.");
						hasWarnedCMYK = true;
					}
					float c = ReadBigEndianFloat(bytes, color_data_start+4);
					float m = ReadBigEndianFloat(bytes, color_data_start+8);
					float y = ReadBigEndianFloat(bytes, color_data_start+12);
					float k = ReadBigEndianFloat(bytes, color_data_start+16);
					short swatch_type = ReadBigEndianInt16(bytes, color_data_start+20);
					rgb = CMYKtoRGB(c,m,y,k);
					break;
				}
				case "Gray": {
					float g = ReadBigEndianFloat(bytes, color_data_start+4);
					short swatch_type = ReadBigEndianInt16(bytes, color_data_start+16);
					rgb = new FloatThree(g,g,g);
					break;
				}
				case "HSB": {
					float h = ReadBigEndianFloat(bytes, color_data_start+4);
					float s = ReadBigEndianFloat(bytes, color_data_start+8);
					float b = ReadBigEndianFloat(bytes, color_data_start+12);
					short swatch_type = ReadBigEndianInt16(bytes, color_data_start+16);
					rgb = HSVToRGB(h,s,b);
					break;
				}
				case "LAB": {
					if (!hasWarnedLAB) {
						UnityEngine.Debug.LogError("[SwatchASEFile] LAB Color format not supported");
						hasWarnedLAB = true;
					}
					float l = ReadBigEndianFloat(bytes, color_data_start+4);
					float a = ReadBigEndianFloat(bytes, color_data_start+8);
					float b = ReadBigEndianFloat(bytes, color_data_start+12);
					short swatch_type = ReadBigEndianInt16(bytes, color_data_start+16);
					rgb = new FloatThree(1,1,1);
					break;
				}
				default:
					rgb = new FloatThree(1,1,1);
					break;
				}

				
			}
			else {
				rgb = new FloatThree(0,0,0);
			}
		}
		public static int ReadBigEndianInt32(BinaryReader byteReader) {
			byte[] p = byteReader.ReadBytes(4);
			return p[0] << 24 | p[1] << 16 | p[2] << 8 | p[3];
		}

		public static short ReadBigEndianInt16(BinaryReader byteReader) {
			byte[] p = byteReader.ReadBytes(2);

			return (short)(p[0] << 8 | p[1]);
		}
		public static short ReadBigEndianInt16(byte[] bytes, int index) {
			return (short)(bytes[index] << 8 | bytes[index+1]);
		}

		public static uint ReadBigEndianUInt32(BinaryReader byteReader) {
			byte[] p = byteReader.ReadBytes(4);
			return (uint)(p[0] << 24 | p[1] << 16 | p[2] << 8 | p[3]);
		}

		public static ushort ReadBigEndianUInt16(BinaryReader byteReader) {
			byte[] p = byteReader.ReadBytes(2);
			return (ushort)(p[0] << 8 | p[1]);
		}

		public static float ReadBigEndianFloat(byte[] bytes, int index) {
			byte[] floatBytes = new byte[4];
			Array.Copy(bytes, index, floatBytes, 0, 4);
			Array.Reverse(floatBytes);
			return BitConverter.ToSingle(floatBytes, 0);
		}

		public static string ReadBigEndianU16String(byte[] bytes, int index, int length) {
			return System.Text.Encoding.BigEndianUnicode.GetString(bytes, index, length);
		}

		public static FloatThree CMYKtoRGB( float c, float m, float y, float k ) {
			var black = 1f - k;
			float r = (1f - c) * black;
			float g = (1f - m) * black;
			float b = (1f - y) * black;
			return new FloatThree( r, g, b);
		}

		public static FloatThree HSVToRGB( float h, float s, float v) {
			UnityEngine.Color c = UnityEngine.Color.HSVToRGB(h, s, v);
			return new FloatThree(c.r, c.g, c.b);
		}

		#pragma warning restore 0219
	}
}