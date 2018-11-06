using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RSA {
	public class RSA {
		public string ToXML() {
			return rsa.ToXmlString(!rsa.PublicOnly);
		}
		public string ToPEM_PKCS1() {
			return RSA_PEM.ToPEM(rsa, false);
		}
		public string ToPEM_PKCS8() {
			return RSA_PEM.ToPEM(rsa, true);
		}


		public string Encode(string str) {
			return RSA_Unit.Base64EncodeBytes(Encode(Encoding.UTF8.GetBytes(str)));
		}
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




		private RSACryptoServiceProvider rsa;
		private void _init() {
			var rsaParams = new CspParameters();
			rsaParams.Flags = CspProviderFlags.UseMachineKeyStore;
			rsa = new RSACryptoServiceProvider(rsaParams);
		}

		/// <summary>
		/// 用指定密钥大小创建一个新的RSA
		/// </summary>
		public RSA(int keySize) {
			_init();

			var rsaParams = new CspParameters();
			rsaParams.Flags = CspProviderFlags.UseMachineKeyStore;
			rsa = new RSACryptoServiceProvider(keySize, rsaParams);
		}
		/// <summary>
		/// 通过指定的密钥，创建一个RSA，xml内可以只包含一个公钥或私钥，或都包含
		/// </summary>
		public RSA(string xml) {
			_init();

			rsa.FromXmlString(xml);
		}
		/// <summary>
		/// 通过一个pem文件创建RSA，pem为公钥或私钥
		/// </summary>
		public RSA(string pem, bool noop) {
			_init();

			rsa = RSA_PEM.FromPEM(pem);
		}
	}
}
