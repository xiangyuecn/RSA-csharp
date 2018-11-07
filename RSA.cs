using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RSA {
	/// <summary>
	/// RSA操作类
	/// </summary>
	public class RSA {
		/// <summary>
		/// 导出XML格式密钥对，如果convertToPublic含私钥的RSA将只返回公钥，仅含公钥的RSA不受影响
		/// </summary>
		public string ToXML(bool convertToPublic = false) {
			return rsa.ToXmlString(!rsa.PublicOnly && !convertToPublic);
		}
		/// <summary>
		/// 导出PEM PKCS#1格式密钥对，如果convertToPublic含私钥的RSA将只返回公钥，仅含公钥的RSA不受影响
		/// </summary>
		public string ToPEM_PKCS1(bool convertToPublic = false) {
			return RSA_PEM.ToPEM(rsa, convertToPublic, false);
		}
		/// <summary>
		/// 导出PEM PKCS#8格式密钥对，如果convertToPublic含私钥的RSA将只返回公钥，仅含公钥的RSA不受影响
		/// </summary>
		public string ToPEM_PKCS8(bool convertToPublic = false) {
			return RSA_PEM.ToPEM(rsa, convertToPublic, true);
		}



		
		/// <summary>
		/// 加密字符串（utf-8），出错抛异常
		/// </summary>
		public string Encode(string str) {
			return RSA_Unit.Base64EncodeBytes(Encode(Encoding.UTF8.GetBytes(str)));
		}
		/// <summary>
		/// 加密数据，出错抛异常
		/// </summary>
		public byte[] Encode(byte[] data) {
			int blockLen = rsa.KeySize / 8 - 11;
			if (data.Length <= blockLen) {
				return rsa.Encrypt(data, false);
			}

			using (var dataStream = new MemoryStream(data))
			using (var enStream = new MemoryStream()) {
				Byte[] buffer = new Byte[blockLen];
				int len = dataStream.Read(buffer, 0, blockLen);

				while (len > 0) {
					Byte[] block = new Byte[len];
					Array.Copy(buffer, 0, block, 0, len);

					Byte[] enBlock = rsa.Encrypt(block, false);
					enStream.Write(enBlock, 0, enBlock.Length);

					len = dataStream.Read(buffer, 0, blockLen);
				}

				return enStream.ToArray();
			}
		}
		/// <summary>
		/// 解密字符串（utf-8），解密异常返回null
		/// </summary>
		public string DecodeOrNull(string str) {
			if (String.IsNullOrEmpty(str)) {
				return null;
			}
			var byts = RSA_Unit.Base64DecodeBytes(str);
			if (byts == null) {
				return null;
			}
			var val = DecodeOrNull(byts);
			if (val == null) {
				return null;
			}
			return Encoding.UTF8.GetString(val);
		}
		/// <summary>
		/// 解密数据，解密异常返回null
		/// </summary>
		public byte[] DecodeOrNull(byte[] data) {
			try {
				int blockLen = rsa.KeySize / 8;
				if (data.Length <= blockLen) {
					return rsa.Decrypt(data, false);
				}

				using (var dataStream = new MemoryStream(data))
				using (var deStream = new MemoryStream()) {
					Byte[] buffer = new Byte[blockLen];
					int len = dataStream.Read(buffer, 0, blockLen);

					while (len > 0) {
						Byte[] block = new Byte[len];
						Array.Copy(buffer, 0, block, 0, len);

						Byte[] deBlock = rsa.Decrypt(block, false);
						deStream.Write(deBlock, 0, deBlock.Length);

						len = dataStream.Read(buffer, 0, blockLen);
					}

					return deStream.ToArray();
				}
			} catch {
				return null;
			}
		}
		/// <summary>
		/// 对str进行签名，并指定hash算法（如：SHA256）
		/// </summary>
		public string Sign(string hash, string str) {
			return RSA_Unit.Base64EncodeBytes(Sign(hash, Encoding.UTF8.GetBytes(str)));
		}
		/// <summary>
		/// 对data进行签名，并指定hash算法（如：SHA256）
		/// </summary>
		public byte[] Sign(string hash, byte[] data) {
			return rsa.SignData(data, hash);
		}
		/// <summary>
		/// 验证字符串str的签名是否是sgin，并指定hash算法（如：SHA256）
		/// </summary>
		public bool Verify(string hash, string sgin, string str) {
			var byts = RSA_Unit.Base64DecodeBytes(sgin);
			if (byts == null) {
				return false;
			}
			return Verify(hash, byts, Encoding.UTF8.GetBytes(str));
		}
		/// <summary>
		/// 验证data的签名是否是sgin，并指定hash算法（如：SHA256）
		/// </summary>
		public bool Verify(string hash, byte[] sgin, byte[] data) {
			try {
				return rsa.VerifyData(data, hash, sgin);
			} catch {
				return false;
			}
		}




		private RSACryptoServiceProvider rsa;
		private void _init() {
			var rsaParams = new CspParameters();
			rsaParams.Flags = CspProviderFlags.UseMachineKeyStore;
			rsa = new RSACryptoServiceProvider(rsaParams);
		}

		/// <summary>
		/// 用指定密钥大小创建一个新的RSA，出错抛异常
		/// </summary>
		public RSA(int keySize) {
			_init();

			var rsaParams = new CspParameters();
			rsaParams.Flags = CspProviderFlags.UseMachineKeyStore;
			rsa = new RSACryptoServiceProvider(keySize, rsaParams);
		}
		/// <summary>
		/// 通过指定的密钥，创建一个RSA，xml内可以只包含一个公钥或私钥，或都包含，出错抛异常
		/// </summary>
		public RSA(string xml) {
			_init();

			rsa.FromXmlString(xml);
		}
		/// <summary>
		/// 通过一个pem文件创建RSA，pem为公钥或私钥，出错抛异常
		/// </summary>
		public RSA(string pem, bool noop) {
			_init();

			rsa = RSA_PEM.FromPEM(pem);
		}
	}
}
