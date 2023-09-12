// 1：直接编译调用.NET Framework 4.6以上版本或.NET Core代码，0：使用反射进行调用；使用.NET Framework 4.6以上版本框架或使用.NET Core框架时可改成1
#define RSA_Util_NewNET_CompileCode_0
#if (RSA_BUILD__NET_CORE || NETCOREAPP || NETSTANDARD || NET) //使用.NET Core框架时自动设为1。csproj:PropertyGroup.DefineConstants + https://docs.microsoft.com/zh-cn/dotnet/csharp/language-reference/preprocessor-directives
#define RSA_Util_NewNET_CompileCode_1
#endif

// 1：直接编译使用BouncyCastle的代码，0：使用反射进行调用；调用了RSA_Util.UseBouncyCastle方法时可改成1
#define RSA_Util_BouncyCastle_CompileCode_0


using System;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

#if RSA_Util_BouncyCastle_CompileCode_1
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Security;
using BcInt = Org.BouncyCastle.Math.BigInteger;
#endif

namespace com.github.xiangyuecn.rsacsharp {
	/// <summary>
	/// RSA操作类，.NET Core、.NET Framework均可用：.NET Core下由实际的RSA实现类提供支持，.NET Framework 4.5及以下由RSACryptoServiceProvider提供支持，.NET Framework 4.6及以上由RSACng提供支持；或者引入BouncyCastle加密增强库提供支持。
	/// GitHub: https://github.com/xiangyuecn/RSA-csharp
	/// </summary>
	public class RSA_Util {
		/// <summary>
		/// 导出XML格式密钥，如果convertToPublic含私钥的RSA将只返回公钥，仅含公钥的RSA不受影响
		/// </summary>
		public string ToXML(bool convertToPublic = false) {
			return ToPEM(convertToPublic).ToXML(convertToPublic);
		}
		/// <summary>
		/// 将密钥导出成PEM对象，如果convertToPublic含私钥的RSA将只返回公钥，仅含公钥的RSA不受影响
		/// </summary>
		public RSA_PEM ToPEM(bool convertToPublic = false) {
			return PEM__.CopyToNew(convertToPublic);
		}
		/// <summary>
		/// 【不安全、不建议使用】对调交换公钥指数（Key_Exponent）和私钥指数（Key_D）：把公钥当私钥使用（new.Key_D=this.Key_Exponent）、私钥当公钥使用（new.Key_Exponent=this.Key_D），返回一个新RSA对象；比如用于：私钥加密、公钥解密，这是非常规的用法。
		/// <br/><br/>当前密钥如果只包含公钥，将不会发生对调，返回的新RSA将允许用公钥进行解密和签名操作；但.NET自带的RSA不支持仅含公钥的密钥进行解密和签名，必须进行指数对调（如果是.NET Framework 4.5及以下版本，公钥私钥均不支持），使用NoPadding填充方式或IsUseBouncyCastle时无此问题。
		/// <br/><br/>注意：把公钥当私钥使用是非常不安全的，因为绝大部分生成的密钥的公钥指数为 0x10001（AQAB），太容易被猜测到，无法作为真正意义上的私钥。
		/// <br/><br/>部分私钥加密实现中，比如Java自带的RSA，使用非NoPadding填充方式时，用私钥对象进行加密可能会采用EMSA-PKCS1-v1_5填充方式（用私钥指数构造成公钥对象无此问题），因此在不同程序之间互通时，可能需要自行使用对应填充算法先对数据进行填充，然后再用NoPadding填充方式进行加密（解密也按NoPadding填充进行解密，然后去除填充数据）。
		/// </summary>
		public RSA_Util SwapKey_Exponent_D__Unsafe() {
			if (PEM__.Key_D == null) {
				var rsa = new RSA_Util(PEM__.CopyToNew(false));
				rsa.allowKeyDNull = true;
				return rsa;
			}
			return new RSA_Util(PEM__.SwapKey_Exponent_D__Unsafe());
		}



		/// <summary>
		/// 内置加密解密填充方式列表
		/// </summary>
		static public string[] RSAPadding_Enc_DefaultKeys() {
			string s = "NO, PKCS1";
			s += ", OAEP+SHA1, OAEP+SHA256, OAEP+SHA224, OAEP+SHA384, OAEP+SHA512";
			s += ", OAEP+SHA-512/224, OAEP+SHA-512/256";
			s += ", OAEP+SHA3-256, OAEP+SHA3-224, OAEP+SHA3-384, OAEP+SHA3-512";
			s += ", OAEP+MD5";
			return s.Split(new string[] { ", " }, StringSplitOptions.None);
		}
		/// <summary>
		/// 将填充方式格式化成内置的RSA加密解密填充模式，padding取值和对应的填充模式：
		/// <code>
		/// <br/> null: 等同于PKCS1
		/// <br/>   "": 等同于PKCS1
		/// <br/>  RSA: 等同于PKCS1
		/// <br/> PKCS: 等同于PKCS1
		/// <br/>  RAW: 等同于NO
		/// <br/> OAEP: 等同于OAEP+SHA1
		/// <br/> RSA/ECB/OAEPPadding: 等同于OAEP+SHA1
		/// <br/> 
		/// <br/>    NO: RSA/ECB/NoPadding
		/// <br/> PKCS1: RSA/ECB/PKCS1Padding （默认值，等同于"RSA"）
		/// <br/> OAEP+SHA1  : RSA/ECB/OAEPwithSHA-1andMGF1Padding
		/// <br/> OAEP+SHA256: RSA/ECB/OAEPwithSHA-256andMGF1Padding
		/// <br/> OAEP+SHA224: RSA/ECB/OAEPwithSHA-224andMGF1Padding
		/// <br/> OAEP+SHA384: RSA/ECB/OAEPwithSHA-384andMGF1Padding
		/// <br/> OAEP+SHA512: RSA/ECB/OAEPwithSHA-512andMGF1Padding
		/// <br/> OAEP+SHA-512/224: RSA/ECB/OAEPwithSHA-512/224andMGF1Padding （SHA-512/*** 2012年发布）
		/// <br/> OAEP+SHA-512/256: RSA/ECB/OAEPwithSHA-512/256andMGF1Padding
		/// <br/> OAEP+SHA3-256: RSA/ECB/OAEPwithSHA3-256andMGF1Padding （SHA3-*** 2015年发布）
		/// <br/> OAEP+SHA3-224: RSA/ECB/OAEPwithSHA3-224andMGF1Padding
		/// <br/> OAEP+SHA3-384: RSA/ECB/OAEPwithSHA3-384andMGF1Padding
		/// <br/> OAEP+SHA3-512: RSA/ECB/OAEPwithSHA3-512andMGF1Padding
		/// <br/> OAEP+MD5     : RSA/ECB/OAEPwithMD5andMGF1Padding
		/// <br/> 
		/// <br/> 如果padding包含RSA字符串，将原样返回此值，用于提供可能支持的任何值
		/// <br/> 非以上取值，将会抛异常
		/// <br/> 
		/// <br/> 其中OAEP的掩码生成函数MGF1使用和OAEP相同的Hash算法
		/// <br/> 
		/// <br/> 以上填充模式全部可用于BouncyCastle的RSA实现；但如果是使用的.NET自带的RSA实现，将会有部分模式无法支持：不支持全部SHA224、SHA-512/256、SHA-512/224，SHA3需要.NET8以上才支持，.NET Framework 4.5及以下只持OAEP+SHA1不支持其他OAEP
		/// <br/>
		/// <br/> 参考：https://learn.microsoft.com/zh-cn/dotnet/api/system.security.cryptography.rsaencryptionpadding
		/// </code>
		/// </summary>
		static public string RSAPadding_Enc(string padding) {
			string val = padding;
			if (val == null || val.Length == 0) val = "PKCS1";
			val = val.ToUpper();

			if ("RSA" == val || "PKCS" == val) val = "PKCS1";
			if ("OAEP" == val || val.EndsWith("/OAEPPADDING")) val = "OAEP+SHA1";
			if ("RAW" == val) val = "NO";
			if (val.IndexOf("RSA") != -1) return padding;

			switch (val) {
				case "PKCS1": return "RSA/ECB/PKCS1Padding";
				case "NO": return "RSA/ECB/NoPadding";
			}
			if (val.StartsWith("OAEP+")) {
				val = val.Replace("OAEP+", "");
				switch (val) {
					case "SHA1":
					case "SHA256":
					case "SHA224":
					case "SHA384":
					case "SHA512":
					case "SHA512/224":
					case "SHA512/256":
						val = "SHA-" + val.Substring(3); break;
				}
				switch (val) {
					case "SHA-1":
					case "SHA-256":
					case "SHA-224":
					case "SHA-384":
					case "SHA-512":
					case "SHA3-256":
					case "SHA3-224":
					case "SHA3-384":
					case "SHA3-512":
					case "SHA-512/224":
					case "SHA-512/256":
					case "MD5":
						return "RSA/ECB/OAEPwith" + val + "andMGF1Padding";
				}
			}
			throw new Exception(T("RSAPadding_Enc未定义Padding: ", "RSAPadding_Enc does not define Padding: ") + padding);
		}

		/// <summary>
		/// 内置签名填充方式列表
		/// </summary>
		static public string[] RSAPadding_Sign_DefaultKeys() {
			string s = "PKCS1+SHA1, PKCS1+SHA256, PKCS1+SHA224, PKCS1+SHA384, PKCS1+SHA512";
			s += ", PKCS1+SHA-512/224, PKCS1+SHA-512/256";
			s += ", PKCS1+SHA3-256, PKCS1+SHA3-224, PKCS1+SHA3-384, PKCS1+SHA3-512";
			s += ", PKCS1+MD5";
			s += ", PSS+SHA1, PSS+SHA256, PSS+SHA224, PSS+SHA384, PSS+SHA512";
			s += ", PSS+SHA-512/224, PSS+SHA-512/256";
			s += ", PSS+SHA3-256, PSS+SHA3-224, PSS+SHA3-384, PSS+SHA3-512";
			s += ", PSS+MD5";
			return s.Split(new string[] { ", " }, StringSplitOptions.None);
		}
		/// <summary>
		/// 将填充方式转换成内置的RSA签名填充模式，hash取值和对应的填充模式：
		/// <code>
		/// <br/> SHA*** : 等同于PKCS1+SHA***，比如"SHA256" == "PKCS1+SHA256"
		/// <br/> MD5    : 等同于PKCS1+MD5
		/// <br/> RSASSA-PSS: 等同于PSS+SHA1
		/// <br/> 
		/// <br/> PKCS1+SHA1  : SHA1withRSA
		/// <br/> PKCS1+SHA256: SHA256withRSA
		/// <br/> PKCS1+SHA224: SHA224withRSA
		/// <br/> PKCS1+SHA384: SHA384withRSA
		/// <br/> PKCS1+SHA512: SHA512withRSA
		/// <br/> PKCS1+SHA-512/224: SHA512/224withRSA （SHA-512/*** 2012年发布）
		/// <br/> PKCS1+SHA-512/256: SHA512/256withRSA
		/// <br/> PKCS1+SHA3-256: SHA3-256withRSA （SHA3-*** 2015年发布）
		/// <br/> PKCS1+SHA3-224: SHA3-224withRSA
		/// <br/> PKCS1+SHA3-384: SHA3-384withRSA
		/// <br/> PKCS1+SHA3-512: SHA3-512withRSA
		/// <br/> PKCS1+MD5   : MD5withRSA
		/// <br/> 
		/// <br/> PSS+SHA1  : SHA1withRSA/PSS
		/// <br/> PSS+SHA256: SHA256withRSA/PSS
		/// <br/> PSS+SHA224: SHA224withRSA/PSS
		/// <br/> PSS+SHA384: SHA384withRSA/PSS
		/// <br/> PSS+SHA512: SHA512withRSA/PSS
		/// <br/> PSS+SHA-512/224: SHA512/224withRSA/PSS （SHA-512/*** 2012年发布）
		/// <br/> PSS+SHA-512/256: SHA512/256withRSA/PSS
		/// <br/> PSS+SHA3-256: SHA3-256withRSA/PSS （SHA3-*** 2015年发布）
		/// <br/> PSS+SHA3-224: SHA3-224withRSA/PSS
		/// <br/> PSS+SHA3-384: SHA3-384withRSA/PSS
		/// <br/> PSS+SHA3-512: SHA3-512withRSA/PSS
		/// <br/> PSS+MD5   : MD5withRSA/PSS
		/// <br/> 
		/// <br/> 如果hash包含RSA字符串，将原样返回此值，用于提供可能支持的任何值
		/// <br/> 非以上取值，将会抛异常
		/// <br/> 
		/// <br/> 其中PSS的salt字节数等于使用的Hash算法字节数，PSS的掩码生成函数MGF1使用和PSS相同的Hash算法，跟踪属性TrailerField取值固定为0xBC
		/// <br/> 
		/// <br/> 以上填充模式全部可用于BouncyCastle的RSA实现；但如果是使用的.NET自带的RSA实现，将会有部分模式无法支持：不支持全部SHA224、SHA-512/256、SHA-512/224，SHA3需要.NET8以上才支持，.NET Framework 4.5及以下不支持PSS
		/// <br/>
		/// <br/> 参考：https://learn.microsoft.com/zh-cn/dotnet/api/system.security.cryptography.rsasignaturepadding
		/// </code>
		/// </summary>
		static public string RSAPadding_Sign(string hash) {
			string val = hash == null ? "" : hash;
			val = val.ToUpper();

			if ("RSASSA-PSS" == val) val = "PSS+SHA1";
			if (val.IndexOf("RSA") != -1) return hash;

			string pss = "";
			if (val.StartsWith("PSS+")) {
				val = val.Substring(4);
				pss = "/PSS";
			} else if (val.StartsWith("PKCS1+")) {
				val = val.Substring(6);
			}
			switch (val) {
				case "SHA-1":
				case "SHA-256":
				case "SHA-224":
				case "SHA-384":
				case "SHA-512":
				case "SHA-512/224":
				case "SHA-512/256":
					val = val.Replace("-", ""); break;
			}
			switch (val) {
				case "SHA1":
				case "SHA256":
				case "SHA224":
				case "SHA384":
				case "SHA512":
				case "SHA3-256":
				case "SHA3-224":
				case "SHA3-384":
				case "SHA3-512":
				case "SHA512/224":
				case "SHA512/256":
				case "MD5":
					return val + "withRSA" + pss;
			}
			throw new Exception(T("RSAPadding_Sign未定义Hash: ", "RSAPadding_Sign does not define Hash: ") + hash);
		}

		static private string NetNotSupportMsg(string tag) {
			return T(".NET不支持" + tag + "，解决办法：", ".NET does not support " + tag + ", solution: ") + Msg_Bc;
		}
		static private string NetLowVerSupportMsg(string tag) {
			return T(".NET Framework版本低于4.6，不支持" + tag + "，解决办法：升级使用.NET Framework 4.6及以上版本，或者", "The .NET Framework version is lower than 4.6 and does not support " + tag + ". Solution: upgrade to .NET Framework 4.6 and above, or ") + Msg_Bc;
		}
		static private string Msg_Bc {
			get {
				return T("引入BouncyCastle加密增强库来扩充.NET加密功能（NuGet：Portable.BouncyCastle或BouncyCastle.Cryptography，文档 https://www.bouncycastle.org/csharp/ ），并且在程序启动时调用" + Msg_Bc_Reg + "进行注册即可得到全部支持。", "import the BouncyCastle encryption enhancement library to expand the .NET encryption function (NuGet: Portable.BouncyCastle or BouncyCastle.Cryptography, documentation https://www.bouncycastle.org/csharp/ ), and call" + Msg_Bc_Reg + "to register when the program starts to get full support.");
			}
		}
		static private readonly string Msg_Bc_Reg = " `RSA_Util.UseBouncyCastle( typeof(RsaEngine).Assembly )` ";
		/// <summary>
		/// 是否是因为.NET兼容性产生的错误
		/// </summary>
		static public bool IsDotNetSupportError(string errMsg) {
			return errMsg.Contains(Msg_Bc_Reg);
		}
		/// <summary>
		/// 将Hash算法名字转换成.NET对象，不支持的将返回null，hash需大写
		/// 。.NET 对 HashAlgorithm.Create 支持混乱，中间有些版本不允许调用
		/// </summary>
		static public HashAlgorithm HashFromName(string hash) {
			HashAlgorithm obj = null;
			try { obj = HashAlgorithm.Create(hash); } catch { }
			if (obj == null) try { obj = HashAlgorithm.Create(hash.Replace("SHA-", "SHA")); } catch { }
			if (obj != null) return obj;
			try {
				var types = typeof(SHA1).Assembly.GetTypes();
				var name1 = hash.Replace("-", "");//SHA256
				var name2 = hash.Replace("-", "_");//SHA3_256
				foreach (var type in types) {
					var name = type.Name.ToUpper();
					if (name == name1 || name == name2) {
						var fn = type.GetMethod("Create", new Type[0]);
						if (fn != null && typeof(HashAlgorithm).IsAssignableFrom(fn.ReturnType)) {
							return (HashAlgorithm)fn.Invoke(null, new object[0]);
						}
					}
				}
			} catch { }
			return null;
		}
		static private void checkHashSupport(string hash) {
			if (HashFromName(hash) == null) {
				throw new Exception(T("本机.NET版本不支持" + hash + "摘要算法，升级使用更高版本的.NET可能会得到支持，或者", "The native .NET version does not support the " + hash + " digest algorithm, upgrading to a later version of .NET may be supported, or ") + Msg_Bc);
			}
		}
		/// <summary>
		/// 简版多语言支持，根据当前语言返回中文或英文，简化调用<see cref="RSA_PEM.T(string, string)"/>
		/// </summary>
		static private string T(string zh, string en) {
			return RSA_PEM.T(zh, en);
		}




		/// <summary>
		/// 加密任意长度字符串（utf-8）返回base64，出错抛异常。本方法线程安全。padding指定填充方式（如：PKCS1、OAEP+SHA256大写），使用空值时默认为PKCS1，取值参考<see cref="RSAPadding_Enc(string)"/>
		/// </summary>
		public string Encrypt(string padding, string str) {
			return Convert.ToBase64String(__Encrypt(padding, Encoding.UTF8.GetBytes(str)));
		}
		/// <summary>
		/// 加密任意长度数据，出错抛异常。本方法线程安全。padding指定填充方式（如：PKCS1、OAEP+SHA256大写），使用空值时默认为PKCS1，取值参考<see cref="RSAPadding_Enc(string)"/>
		/// </summary>
		public byte[] Encrypt(string padding, byte[] data) {
			return __Encrypt(padding, data);
		}
		/// <summary>
		/// 解密任意长度密文（base64）得到字符串（utf-8），出错抛异常。本方法线程安全。padding指定填充方式（如：PKCS1、OAEP+SHA256大写），使用空值时默认为PKCS1，取值参考<see cref="RSAPadding_Enc(string)"/>
		/// </summary>
		public string Decrypt(string padding, string str) {
			if (string.IsNullOrEmpty(str)) {
				return "";
			}
			byte[] byts = Convert.FromBase64String(str);
			var val = __Decrypt(padding, byts);
			return Encoding.UTF8.GetString(val);
		}
		/// <summary>
		/// 解密任意长度数据，出错抛异常。本方法线程安全。padding指定填充方式（如：PKCS1、OAEP+SHA256大写），使用空值时默认为PKCS1，取值参考<see cref="RSAPadding_Enc(string)"/>
		/// </summary>
		public byte[] Decrypt(string padding, byte[] data) {
			return __Decrypt(padding, data);
		}



		/// <summary>
		/// 对字符串str进行签名，返回base64结果，出错抛异常。本方法线程安全。hash指定签名摘要算法和填充方式（如：SHA256、PSS+SHA1大写），取值参考<see cref="RSAPadding_Sign(string)"/>
		/// </summary>
		public string Sign(string hash, string str) {
			return Convert.ToBase64String(__Sign(hash, Encoding.UTF8.GetBytes(str)));
		}
		/// <summary>
		/// 对data进行签名，出错抛异常。本方法线程安全。hash指定签名摘要算法和填充方式（如：SHA256、PSS+SHA1大写），取值参考<see cref="RSAPadding_Sign(string)"/>
		/// </summary>
		public byte[] Sign(string hash, byte[] data) {
			return __Sign(hash, data);
		}
		/// <summary>
		/// 验证字符串str的签名是否是sign（base64），出错抛异常。本方法线程安全。hash指定签名摘要算法和填充方式（如：SHA256、PSS+SHA1大写），取值参考<see cref="RSAPadding_Sign(string)"/>
		/// </summary>
		public bool Verify(string hash, string sign, string str) {
			byte[] byts = null;
			try { byts = Convert.FromBase64String(sign); } catch { }
			if (byts == null) {
				return false;
			}
			return __Verify(hash, byts, Encoding.UTF8.GetBytes(str));
		}
		/// <summary>
		/// 验证data的签名是否是sign，出错抛异常。本方法线程安全。hash指定签名摘要算法和填充方式（如：SHA256、PSS+SHA1大写），取值参考<see cref="RSAPadding_Sign(string)"/>
		/// </summary>
		public bool Verify(string hash, byte[] sign, byte[] data) {
			return __Verify(hash, sign, data);
		}





		/// <summary>
		/// 用指定密钥大小创建一个新的RSA，会生成新密钥，出错抛异常
		/// </summary>
		public RSA_Util(int keySize) {
			RSA rsa = null;
			if (IS_CORE) {
				rsa = RSA.Create();
				rsa.KeySize = keySize;
			}
			if (rsa == null || rsa is RSACryptoServiceProvider) {
				var rsaParams = new CspParameters();
				rsaParams.Flags = CspProviderFlags.UseMachineKeyStore;
				rsa = new RSACryptoServiceProvider(keySize, rsaParams);
			}
			SetPEM__(new RSA_PEM(rsa, false));
		}
		/// <summary>
		/// 通过指定的pem文件密钥或xml字符串密钥，创建一个RSA，pem或xml内可以只包含一个公钥或私钥，或都包含，出错抛异常
		/// </summary>
		public RSA_Util(string pemOrXML) {
			if (pemOrXML.Trim().StartsWith("<")) {
				SetPEM__(RSA_PEM.FromXML(pemOrXML));
			} else {
				SetPEM__(RSA_PEM.FromPEM(pemOrXML));
			}
		}
		/// <summary>
		/// 通过一个pem对象创建RSA，pem为公钥或私钥，出错抛异常
		/// </summary>
		public RSA_Util(RSA_PEM pem) {
			SetPEM__(pem);
		}
		/// <summary>
		/// 本方法会先生成RSA_PEM再创建RSA：通过公钥指数和私钥指数构造一个PEM，会反推计算出P、Q但和原始生成密钥的P、Q极小可能相同
		/// 注意：所有参数首字节如果是0，必须先去掉
		/// 出错将会抛出异常
		/// </summary>
		/// <param name="modulus">必须提供模数</param>
		/// <param name="exponent">必须提供公钥指数</param>
		/// <param name="dOrNull">私钥指数可以不提供，导出的PEM就只包含公钥</param>
		public RSA_Util(byte[] modulus, byte[] exponent, byte[] dOrNull) {
			SetPEM__(new RSA_PEM(modulus, exponent, dOrNull));
		}
		/// <summary>
		/// 本方法会先生成RSA_PEM再创建RSA：通过全量的PEM字段数据构造一个PEM，除了模数modulus和公钥指数exponent必须提供外，其他私钥指数信息要么全部提供，要么全部不提供（导出的PEM就只包含公钥）
		/// 注意：所有参数首字节如果是0，必须先去掉
		/// </summary>
		public RSA_Util(byte[] modulus, byte[] exponent, byte[] d, byte[] p, byte[] q, byte[] dp, byte[] dq, byte[] inverseQ) {
			SetPEM__(new RSA_PEM(modulus, exponent, d, p, q, dp, dq, inverseQ));
		}




		/// <summary>
		/// 密钥位数
		/// </summary>
		public int KeySize { get; private set; }
		/// <summary>
		/// 是否包含私钥
		/// </summary>
		public bool HasPrivate { get; private set; }


		/// <summary>
		/// 获取最底层的RSA对象，.NET Core下为实际的RSA实现类，.NET Framework 4.5及以下RSACryptoServiceProvider，.NET Framework 4.6及以上RSACng；注意：IsUseBouncyCastle时将不会使用.NET的RSA
		/// </summary>
		public RSA RSAObject {
			get {
				return createRSA();
			}
		}
		/// <summary>
		/// 最底层的RSA对象是否是使用的.NET Core（RSA），否则将是使用的.NET Framework（4.5及以下RSACryptoServiceProvider、4.6及以上RSACng）；注意：IsUseBouncyCastle时将不会使用.NET的RSA
		/// </summary>
		public bool RSAIsUseCore {
			get {
				return IS_CORE && !(createRSA() is RSACryptoServiceProvider);
			}
		}


		private void SetPEM__(RSA_PEM pem) {
			PEM__ = pem;
			KeySize = pem.KeySize;
			HasPrivate = pem.HasPrivate;
		}
		private RSA_PEM PEM__;
		private RSA createRSA() {
			if (IS_CORE) return PEM__.GetRSA_ForCore();
			if (IS_CoreOr46) { //必须使用RSACng，不然新填充方式会抛出不支持
				return GetRSA_WindowsCng(PEM__);
			}
			return PEM__.GetRSA_ForWindows();
		}


		private bool allowKeyDNull;
		private void checkKeyD(bool usePub) {
			if (usePub) return;
			if (PEM__.Key_D != null) return;
			if (allowKeyDNull) return;
			throw new Exception(T("当前是公钥，常规情况下不允许进行Decrypt或Sign操作，可以调用SwapKey方法来允许进行此操作", "Currently it is a public key. Decrypt or Sign operations are not allowed under normal circumstances. You can call the SwapKey method to allow this operation."));
		}







		/******************底层加密解密调用*******************/

		/// <summary>
		/// 加密
		/// </summary>
		private byte[] __Encrypt(string padding, byte[] data) {
			string ctype = RSAPadding_Enc(padding), CType = ctype.ToUpper();

			int blockLen = KeySize / 8;
			if (CType.IndexOf("OAEP") != -1) {
				//OAEP填充占用 2*hashLen+2 字节：https://www.rfc-editor.org/rfc/rfc8017.html#section-7.1.1
				int shaLen; string _;
				__OaepParam(ctype, out _, out _, out shaLen);
				int sub = 2 * shaLen / 8 + 2;
				blockLen -= sub;
				if (blockLen < 1) {
					string min = "NaN"; if (sub > 0) min = (int)Math.Pow(2, Math.Ceiling(Math.Log(sub * 8) / Math.Log(2))) + "";
					throw new Exception("RSA[" + ctype + "][keySize=" + KeySize + "] " + T("密钥位数不能小于", "Key digits cannot be less than ") + min);
				}
			} else if (CType.IndexOf("NOPADDING") != -1) {
				//NOOP 无填充，不够数量时会在开头给0
			} else {
				//PKCS1填充占用11字节：https://www.rfc-editor.org/rfc/rfc8017.html#section-7.2.1
				blockLen -= 11;
			}

			return __EncDec(true, ctype, data, blockLen);
		}
		/// <summary>
		/// 解密
		/// </summary>
		private byte[] __Decrypt(string padding, byte[] data) {
			string ctype = RSAPadding_Enc(padding);

			int blockLen = KeySize / 8;
			return __EncDec(false, ctype, data, blockLen);
		}
		static private Regex OAEP_Exp = new Regex("^RSA/(.+?)/OAEPWITHSHA(3-|-?512/)?[\\-/]?(\\d+)ANDMGF1PADDING$");
		static private void __OaepParam(string ctype, out string outType, out string outHash, out int outLen) {
			string CType = ctype.ToUpper(); bool isMd5 = false;
			if (CType.IndexOf("MD5") != -1) {
				isMd5 = true; CType = CType.Replace("MD5", "SHA-128");//伪装成SHA简化逻辑
			}
			Match m = OAEP_Exp.Match(CType);
			if (!m.Success) {
				throw new Exception(ctype + T("不在预定义列表内，无法识别出Hash算法", " is not in the predefined list, and the Hash algorithm cannot be recognized"));
			}
			int shaN = Convert.ToInt32(m.Groups[3].Value);
			outLen = shaN == 1 ? 160 : shaN;//sha1 为 160位
			outType = "RSA/" + m.Groups[1].Value + "/OAEPPadding";

			string hash;
			if (isMd5) {
				hash = "MD5";
			} else {
				hash = "SHA-" + shaN; string m2 = m.Groups[2].Value;
				if (m2 != null && m2.Length != 0) {
					if (m2.IndexOf("512") != -1) {
						hash = "SHA-512/" + shaN;
					} else {
						hash = "SHA3-" + shaN;
					}
				}
			}
			outHash = hash;
		}
		private byte[] __EncDec(bool isEnc, string ctype, byte[] data, int blockLen) {
			checkKeyD(isEnc);
			string ctype0 = ctype, CType = ctype.ToUpper();
			bool isNO = false, isOaep = false;

			string hash = null; int shaLen;
			if (CType.IndexOf("OAEP") != -1) {
				isOaep = true;
				__OaepParam(ctype, out ctype, out hash, out shaLen);
			} else if (CType.IndexOf("NOPADDING") != -1) {
				isNO = true;
			}

			Func<int, int, byte[]> process;
			Action destory;

			if (rsaBouncyCastle != null) {
				//使用BouncyCastle进行加密解密
#if RSA_Util_BouncyCastle_CompileCode_1
				ICipherParameters key = Bc_Key(isEnc);
				IAsymmetricBlockCipher cipher = new RsaEngine();
				if (isNO) {
					//NOOP 无填充，不够数量时会在开头给0
				} else if (isOaep) {
					IDigest hashObj = DigestUtilities.GetDigest(hash);
					cipher = new OaepEncoding(cipher, hashObj, hashObj, null);
				} else {
					cipher = new Pkcs1Encoding(cipher);
				}
				cipher.Init(isEnc, key);
				destory = () => { cipher = null; };
				process = (offset, len) => {
					return cipher.ProcessBlock(data, offset, len);
				};
#else
				object key = Bc_Key(isEnc);
				object cipher = rsaBouncyCastle.GetType(BcName_RsaEngine).GetConstructor(new Type[0]).Invoke(new object[0]);
				if (isNO) {
					//NOOP 无填充，不够数量时会在开头给0
				} else if (isOaep) {
					object hashObj = rsaBouncyCastle.GetType("Org.BouncyCastle.Security.DigestUtilities").GetMethod("GetDigest", new Type[] { typeof(string) }).Invoke(null, new object[] { hash });
					cipher = FindCtor(rsaBouncyCastle.GetType("Org.BouncyCastle.Crypto.Encodings.OaepEncoding"), new string[] { "iasym", "idigest", "idigest", "byte" }).Invoke(new object[] { cipher, hashObj, hashObj, null });
				} else {
					cipher = FindCtor(rsaBouncyCastle.GetType("Org.BouncyCastle.Crypto.Encodings.Pkcs1Encoding"), new string[] { "iasym" }).Invoke(new object[] { cipher });
				}
				FindFunc(cipher.GetType(), "Init", new string[] { "bool", "" }).Invoke(cipher, new object[] { isEnc, key });
				destory = () => { cipher = null; };
				var processBlock = FindFunc(cipher.GetType(), "ProcessBlock", new string[] { "byte", "int", "int" });
				process = (offset, len) => {
					return (byte[])processBlock.Invoke(cipher, new object[] { data, offset, len });
				};
#endif
			} else if (isNO) {
				//.NET不支持NoPadding，手动实现一下
				var n = RSA_PEM.BigX(PEM__.Key_Modulus);
				var e = RSA_PEM.BigX(PEM__.Key_Exponent);
				if (!isEnc && PEM__.Key_D != null) {//如果未提供私钥，将用公钥解密
					e = RSA_PEM.BigX(PEM__.Key_D);
				}
				process = (offset, len) => {
					if (isEnc) {
						byte[] pad0 = new byte[blockLen];
						Array.Copy(data, offset, pad0, pad0.Length - len, len);
						var m = RSA_PEM.BigX(pad0);
						var c = BigInteger.ModPow(m, e, n);
						return RSA_PEM.BigB(c);
					} else {
						var enc = new byte[len];
						Array.Copy(data, offset, enc, 0, len);
						var m = RSA_PEM.BigX(enc);
						var c = BigInteger.ModPow(m, e, n);
						return RSA_PEM.BigB(c);
					}
				};
				destory = () => { };
			} else if (IS_CoreOr46) {
				//使用高版本RSA进行加密解密，4.6+ 或 Core
				if (isNO) throw new Exception(NetNotSupportMsg(ctype0 + T("加密填充模式", " encryption padding mode")));
				string hashName = null;
				if (isOaep) {
					checkHashSupport(hash);
					hashName = hash.Replace("SHA-", "SHA");
				}

#if RSA_Util_NewNET_CompileCode_1
				RSAEncryptionPadding padding = RSAEncryptionPadding.Pkcs1;
				if (isOaep) {
					padding = RSAEncryptionPadding.CreateOaep(new HashAlgorithmName(hashName));
				}
				RSA rsa = createRSA();
#else
				dynamic padding = Type_RSAEncryptionPadding.GetProperty("Pkcs1").GetValue(null);
				if (isOaep) {
					padding = FindFunc(Type_RSAEncryptionPadding, "CreateOaep", new string[] { "hashalg" }).Invoke(null, new object[] { Get_HashAlgorithmName(hashName) });
				}
				dynamic rsa = createRSA();
#endif
				destory = () => { rsa.Dispose(); rsa = null; };
				process = (offset, len) => {
					byte[] bytes = new byte[len];
					Array.Copy(data, offset, bytes, 0, len);
					if (isEnc) return rsa.Encrypt(bytes, padding);
					return rsa.Decrypt(bytes, padding);
				};
			} else {
				//使用低版本RSA进行加密解密，4.6以下版本
				if (isNO) throw new Exception(NetNotSupportMsg(ctype0 + T("加密填充模式", " encryption padding mode")));
				if (isOaep && hash != "SHA-1") throw new Exception(NetLowVerSupportMsg(ctype0 + T("加密填充模式（只支持SHA-1）", " encryption padding mode (only SHA-1 is supported)")));

				RSACryptoServiceProvider rsa = (RSACryptoServiceProvider)createRSA();
				destory = () => { rsa.Dispose(); rsa = null; };
				process = (offset, len) => {
					byte[] bytes = new byte[len];
					Array.Copy(data, offset, bytes, 0, len);
					if (isEnc) return rsa.Encrypt(bytes, isOaep);
					return rsa.Decrypt(bytes, isOaep);
				};
			}

			//数据分段进行加密解密
			using (var stream = new MemoryStream()) {
				int start = 0;
				while (start < data.Length) {
					int len = blockLen;
					if (start + len > data.Length) {
						len = data.Length - start;
					}

					byte[] val = process(start, len);
					if (!isEnc && isNO) {
						//没有填充时，去掉开头的0
						int idx = 0;
						for (; idx < val.Length; idx++) {
							if (val[idx] != 0) break;
						}
						byte[] val2 = new byte[val.Length - idx];
						Array.Copy(val, idx, val2, 0, val2.Length);
						val = val2;
					}
					stream.Write(val, 0, val.Length);
					start += len;
				}
				destory();
				return stream.ToArray();
			}
		}



		/******************底层签名验证调用*******************/

		private byte[] __Sign(string hash, byte[] data) {
			byte[] val; bool _;
			__SignVerify(true, hash, data, null, out val, out _);
			return val;
		}
		private bool __Verify(string hash, byte[] sign, byte[] data) {
			byte[] _; bool val;
			__SignVerify(false, hash, data, sign, out _, out val);
			return val;
		}
		static private Regex HS_Exp = new Regex("^SHA(3-|-?512/)?[\\-/]?(\\d+)WITHRSA$");
		private void __SignVerify(bool isSign, string hashType, byte[] data, byte[] signData, out byte[] signVal, out bool verifyVal) {
			checkKeyD(!isSign);
			string stype = RSAPadding_Sign(hashType), SType = stype.ToUpper();

			bool isPss = SType.EndsWith("/PSS");
			if (isPss) {
				SType = SType.Substring(0, stype.Length - 4);
			}
			bool isMd5 = SType.IndexOf("MD5") != -1;
			if (isMd5) {
				SType = SType.Replace("MD5", "SHA-128");//伪装成SHA简化逻辑
			}

			Match m = HS_Exp.Match(SType);
			if (!m.Success) {
				throw new Exception(stype + T("不在预定义列表内，无法识别出Hash算法", " is not in the predefined list, and the Hash algorithm cannot be recognized"));
			}
			int shaN = Convert.ToInt32(m.Groups[2].Value);
			int shaLen = shaN == 1 ? 160 : shaN;//sha1 为 160位

			string hash;
			if (isMd5) {
				hash = "MD5";
			} else {
				hash = "SHA-" + shaN; string m2 = m.Groups[1].Value;
				if (m2 != null && m2.Length != 0) {
					if (m2.IndexOf("512") != -1) {
						hash = "SHA-512/" + shaN;
					} else {
						hash = "SHA3-" + shaN;
					}
				}
			}

			if (rsaBouncyCastle != null) {
				//使用BouncyCastle进行签名验证
#if RSA_Util_BouncyCastle_CompileCode_1
				ICipherParameters key = Bc_Key(!isSign);
				IDigest hashObj = DigestUtilities.GetDigest(hash);
				ISigner signer;
				if (isPss) {
					signer = new PssSigner(new RsaEngine(), hashObj, hashObj, shaLen / 8, 0xBC);
				} else {
					signer = new RsaDigestSigner(hashObj);
				}
				signer.Init(isSign, key);
				signer.BlockUpdate(data, 0, data.Length);
				if (isSign) {
					signVal = signer.GenerateSignature();
					verifyVal = false;
				} else {
					signVal = null;
					verifyVal = signer.VerifySignature(signData);
				}
#else
				object key = Bc_Key(!isSign);
				object hashObj = rsaBouncyCastle.GetType("Org.BouncyCastle.Security.DigestUtilities").GetMethod("GetDigest", new Type[] { typeof(string) }).Invoke(null, new object[] { hash });
				object signer;
				if (isPss) {
					object cipher = rsaBouncyCastle.GetType(BcName_RsaEngine).GetConstructor(new Type[0]).Invoke(new object[0]);
					signer = FindCtor(rsaBouncyCastle.GetType("Org.BouncyCastle.Crypto.Signers.PssSigner"), new string[] { "iasym", "idigest", "idigest", "int", "byte" }).Invoke(new object[] { cipher, hashObj, hashObj, shaLen / 8, (byte)0xBC });
				} else {
					signer = FindCtor(rsaBouncyCastle.GetType("Org.BouncyCastle.Crypto.Signers.RsaDigestSigner"), new string[] { "idigest" }).Invoke(new object[] { hashObj });
				}
				FindFunc(signer.GetType(), "Init", new string[] { "bool", "" }).Invoke(signer, new object[] { isSign, key });
				FindFunc(signer.GetType(), "BlockUpdate", new string[] { "byte", "int", "int" }).Invoke(signer, new object[] { data, 0, data.Length });
				if (isSign) {
					signVal = (byte[])FindFunc(signer.GetType(), "GenerateSignature", new string[0]).Invoke(signer, new object[0]);
					verifyVal = false;
				} else {
					signVal = null;
					verifyVal = (bool)FindFunc(signer.GetType(), "VerifySignature", new string[] { "byte" }).Invoke(signer, new object[] { signData });
				}
#endif
				return;
			}

			var hashName = hash.Replace("SHA-", "SHA");
			if (IS_CoreOr46) {
				//使用高版本RSA进行加密解密，4.6+ 或 Core
				checkHashSupport(hash);

#if RSA_Util_NewNET_CompileCode_1
				RSA rsa = createRSA();
				var hashObj = new HashAlgorithmName(hashName);
				var padding = RSASignaturePadding.Pkcs1;
				if (isPss) {
					padding = RSASignaturePadding.Pss;
				}
#else
				Type SP = typeof(RSA).Assembly.GetType(Space_Cryptography + "RSASignaturePadding");
				dynamic rsa = createRSA();
				dynamic hashObj = Get_HashAlgorithmName(hashName);
				dynamic padding = SP.GetProperty("Pkcs1").GetValue(null);
				if (isPss) {
					padding = SP.GetProperty("Pss").GetValue(null);
				}
#endif
				if (isSign) {
					signVal = rsa.SignData(data, hashObj, padding);
					verifyVal = false;
				} else {
					signVal = null;
					verifyVal = rsa.VerifyData(data, signData, hashObj, padding);
				}
				rsa.Dispose();
				return;
			} else {
				//使用低版本RSA进行加密解密，4.6以下版本
				if (isPss) throw new Exception(NetLowVerSupportMsg(T("所有PSS签名填充模式", "All PSS signature padding modes")));
				checkHashSupport(hash);

				RSACryptoServiceProvider rsa = (RSACryptoServiceProvider)createRSA();
				if (isSign) {
					signVal = rsa.SignData(data, hashName);
					verifyVal = false;
				} else {
					signVal = null;
					verifyVal = rsa.VerifyData(data, hashName, signData);
				}
				rsa.Dispose();
				return;
			}
		}




		/// <summary>
		/// 反射查找出参数匹配的方法，方法名字为".ctor"时查找构造方法；参数名字为空匹配任意参数，小写前缀匹配
		/// </summary>
		static public MethodBase FindFunc(Type type, string func, string[] paramNames) {
			MethodBase[] arr; bool isCtor = false;
			if (func == ".ctor") {
				arr = type.GetConstructors(); isCtor = true;
			} else {
				arr = type.GetMethods();
			}
			foreach (var m in arr) {
				if (!isCtor && m.Name != func) continue;
				var ps = m.GetParameters(); MethodBase find = null;
				if (ps.Length == paramNames.Length) {
					find = m;
					for (int i = 0; i < ps.Length; i++) {
						var n = paramNames[i];
						if (n.Length > 0 && !ps[i].ParameterType.Name.ToLower().StartsWith(n)) {
							find = null; break;
						}
					}
				}
				if (find != null) return find;
			}
			throw new Exception(T(type.FullName + "中未找到方法", "Method not found in " + type.FullName + ": ") + func + "(" + string.Join(",", paramNames) + ")");
		}
		static public ConstructorInfo FindCtor(Type type, string[] paramNames) {
			return (ConstructorInfo)FindFunc(type, ".ctor", paramNames);
		}




		/****************平台差异兼容处理****************/

		/// <summary>
		/// 使用BouncyCastle的RSA实现进行加密，提供BouncyCastle的程序集
		/// </summary>
		static private Assembly rsaBouncyCastle;
		/// <summary>
		/// 是否强制使用BouncyCastle加密增强库进行RSA操作，为true时将不会使用.NET的RSA
		/// </summary>
		static public bool IsUseBouncyCastle {
			get { return rsaBouncyCastle != null; }
		}
		/// <summary>
		/// 强制使用BouncyCastle加密增强库进行RSA操作。只需在程序启动后调用一次即可，直接调用一下BouncyCastle里面的类，传入程序集：<c>UseBouncyCastle(typeof(RsaEngine).Assembly)</c>，传入null取消使用
		/// </summary>
		static public void UseBouncyCastle(Assembly bouncyCastleAssembly) {
			if (bouncyCastleAssembly != null && bouncyCastleAssembly.GetType(BcName_RsaEngine) == null) {
				throw new Exception(T("UseBouncyCastle方法必须传入BouncyCastle的Assembly", "The UseBouncyCastle method must pass in the Assembly of BouncyCastle"));
			}
			rsaBouncyCastle = bouncyCastleAssembly;
		}
		static private readonly string BcName_RsaEngine = "Org.BouncyCastle.Crypto.Engines.RsaEngine";
		private dynamic Bc_Key(bool usePub) {
			var k = PEM__;
			Func<byte[], byte[]> BigX = (bytes) => {
				byte[] val = new byte[bytes.Length + 1];
				Array.Copy(bytes, 0, val, 1, bytes.Length);
				return val;
			};
#if RSA_Util_BouncyCastle_CompileCode_1
			BcInt[] ks = new BcInt[8];
			ks[0] = new BcInt(BigX(k.Key_Modulus));
			ks[1] = new BcInt(BigX(k.Key_Exponent));
			checkKeyD(usePub);
			if (usePub || k.Key_D == null) {
				return new RsaKeyParameters(!usePub, ks[0], ks[1]);
			}
			ks[2] = new BcInt(BigX(k.Key_D));
			ks[3] = new BcInt(BigX(k.Val_P));
			ks[4] = new BcInt(BigX(k.Val_Q));
			ks[5] = new BcInt(BigX(k.Val_DP));
			ks[6] = new BcInt(BigX(k.Val_DQ));
			ks[7] = new BcInt(BigX(k.Val_InverseQ));
			return new RsaPrivateCrtKeyParameters(ks[0], ks[1], ks[2], ks[3], ks[4], ks[5], ks[6], ks[7]);
#else
			var BInt = rsaBouncyCastle.GetType("Org.BouncyCastle.Math.BigInteger").GetConstructor(new Type[] { typeof(byte[]) });
			object[] ks = new object[8];
			ks[0] = BInt.Invoke(new object[] { BigX(k.Key_Modulus) });
			ks[1] = BInt.Invoke(new object[] { BigX(k.Key_Exponent) });
			checkKeyD(usePub);
			if (usePub || k.Key_D == null) {//如果未提供私钥，将用公钥解密、签名
				return FindCtor(rsaBouncyCastle.GetType("Org.BouncyCastle.Crypto.Parameters.RsaKeyParameters"), new string[] { "bool", "big", "big" }).Invoke(new object[] { !usePub, ks[0], ks[1] });
			}
			ks[2] = BInt.Invoke(new object[] { BigX(k.Key_D) });
			ks[3] = BInt.Invoke(new object[] { BigX(k.Val_P) });
			ks[4] = BInt.Invoke(new object[] { BigX(k.Val_Q) });
			ks[5] = BInt.Invoke(new object[] { BigX(k.Val_DP) });
			ks[6] = BInt.Invoke(new object[] { BigX(k.Val_DQ) });
			ks[7] = BInt.Invoke(new object[] { BigX(k.Val_InverseQ) });
			return FindCtor(rsaBouncyCastle.GetType("Org.BouncyCastle.Crypto.Parameters.RsaPrivateCrtKeyParameters"), new string[] { "big", "big", "big", "big", "big", "big", "big", "big" }).Invoke(ks);
#endif
		}


		/// <summary>
		/// 当前运行环境是否为.NET Core，false为.NET Framework
		/// </summary>
		static public bool IS_CORE {
			get {
				if (is__core == null) {
					is__core = new bool[] { !typeof(RSA).Assembly.ToString().ToLower().Contains("mscorlib") };
				}
				return is__core[0];
			}
		}
		static private bool[] is__core;
		/// <summary>
		/// 当前运行环境是否是.NET Framework 4.6以上或.NET Core
		/// </summary>
		static public bool IS_CoreOr46 {
			get {
				if (IS_CORE) return true;
				if (is__core_or_46 == null) {
					Type type = Type_RSAEncryptionPadding;
					is__core_or_46 = new int[] { type != null ? 1 : -1 };
				}
				return is__core_or_46[0] > 0;
			}
		}
		static private int[] is__core_or_46;
		/// <summary>
		/// .NET Framework 下测试，可以指定以高版本运行还是低版本运行，方便测试，取值：0重设为默认，1高版本，-1低版本
		/// </summary>
		static public void IS_CoreOr46_Test_Set(int val) {
			is__core_or_46 = val == 0 ? null : new int[] { val };
		}



		//.NET Framework 低版本兼容，4.6以上或Core才有的类
		/// <summary>
		/// 4.6以上使用RSACng，RSACng支持的部分填充方式如果换成RSACryptoServiceProvider会抛不支持的异常
		/// </summary>
		static private RSA GetRSA_WindowsCng(RSA_PEM pem) {
			//.NET Core里面没有RSACng，兼容编译
			//统一反射进行获取，Framework全在System.Core.dll里面
			var type = typeof(ECDsa).Assembly.GetType(Space_Cryptography + "RSACng");
			if (type == null) return null; //Core
			var rsa = (RSA)type.GetConstructor(new Type[0]).Invoke(new object[0]);
			pem.GetRSA__ImportParameters(rsa);
			return rsa;
		}
		static private readonly string Space_Cryptography = "System.Security.Cryptography.";
		static private Type Type_RSAEncryptionPadding {
			get {
				return typeof(RSA).Assembly.GetType(Space_Cryptography + "RSAEncryptionPadding");
			}
		}
		static private dynamic Get_HashAlgorithmName(string hash) {
			return typeof(RSA).Assembly.GetType(Space_Cryptography + "HashAlgorithmName").GetConstructor(new Type[] { typeof(string) }).Invoke(new object[] { hash });
		}


	}
}
