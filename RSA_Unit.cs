using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RSA {
	/// <summary>
	/// 封装的一些通用方法
	/// </summary>
	public class RSA_Unit {
		static public string Base64EncodeBytes(byte[] byts) {
			return Convert.ToBase64String(byts);
		}
		static public byte[] Base64DecodeBytes(string str) {
			try {
				return Convert.FromBase64String(str);
			} catch {
				return null;
			}
		}
		/// <summary>
		/// 把字符串按每行多少个字断行
		/// </summary>
		static public string TextBreak(string text, int line) {
			var idx = 0;
			var len = text.Length;
			var str = new StringBuilder();
			while (idx < len) {
				if (idx > 0) {
					str.Append('\n');
				}
				if (idx + line >= len) {
					str.Append(text.Substring(idx));
				} else {
					str.Append(text.Substring(idx, line));
				}
				idx += line;
			}
			return str.ToString();
		}
	}

	static public class Extensions {
		/// <summary>
		/// 从数组start开始到指定长度复制一份
		/// </summary>
		static public T[] sub<T>(this T[] arr, int start, int count) {
			T[] val = new T[count];
			for (var i = 0; i < count; i++) {
				val[i] = arr[start + i];
			}
			return val;
		}
		static public void writeAll(this Stream stream, byte[] byts) {
			stream.Write(byts, 0, byts.Length);
		}
	}
}
