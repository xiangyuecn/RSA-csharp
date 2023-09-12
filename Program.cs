using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace com.github.xiangyuecn.rsacsharp {
	/// <summary>
	/// RSA、RSA_PEM测试控制台主程序，.NET Core、.NET Framework均可测试
	/// GitHub: https://github.com/xiangyuecn/RSA-csharp
	/// </summary>
	class Program {
		static public void Main(string[] args) {
			//【请在这里编写你自己的测试代码】

			ShowMenu(args);
		}

		static void RSATest(bool fast) {
			//新生成一个RSA密钥，也可以通过已有的pem、xml文本密钥来创建RSA
			var rsa = new RSA_Util(512);
			// var rsa = new RSA_Util("pem或xml文本密钥");
			// var rsa = new RSA_Util(RSA_PEM.FromPEM("pem文本密钥"));
			// var rsa = new RSA_Util(RSA_PEM.FromXML("xml文本密钥"));

			//得到pem对象
			RSA_PEM pem = rsa.ToPEM(false);
			//提取密钥pem字符串
			string pem_pkcs1 = pem.ToPEM_PKCS1();
			string pem_pkcs8 = pem.ToPEM_PKCS8();
			//提取密钥xml字符串
			string xml = rsa.ToXML();

			AssertMsg(T("【" + rsa.KeySize + "私钥（XML）】：", "[ " + rsa.KeySize + " Private Key (XML) ]:"), rsa.KeySize == 512);
			S(xml);
			S();
			ST("【" + rsa.KeySize + "私钥（PKCS#1）】：", "[ " + rsa.KeySize + " Private Key (PKCS#1) ]:");
			S(pem_pkcs1);
			S();
			ST("【" + rsa.KeySize + "公钥（PKCS#8）】：", "[ " + rsa.KeySize + " Public Key (PKCS#8) ]:");
			S(pem.ToPEM_PKCS8(true));
			S();

			var str = T("abc内容123", "abc123");
			var en = rsa.Encrypt("PKCS1", str);
			ST("【加密】：", "[ Encrypt ]:");
			S(en);

			ST("【解密】：", "[ Decrypt ]:");
			var de = rsa.Decrypt("PKCS1", en);
			AssertMsg(de, de == str);

			if (!fast) {
				var str2 = str; for (var i = 0; i < 15; i++) str2 += str2;
				ST("【长文本加密解密】：", "[ Long text encryption and decryption ]:");
				AssertMsg(str2.Length + T("个字 OK", " characters OK"), rsa.Decrypt("PKCS1", rsa.Encrypt("PKCS1", str2)) == str2);
			}

			ST("【签名SHA1】：", "[ Signature SHA1 ]:");
			var sign = rsa.Sign("SHA1", str);
			Console.WriteLine(sign);
			AssertMsg(T("校验 OK", "Verify OK"), rsa.Verify("SHA1", sign, str));
			Console.WriteLine();

			//用pem文本创建RSA
			var rsa2 = new RSA_Util(RSA_PEM.FromPEM(pem_pkcs8));
			ST("【用PEM新创建的RSA是否和上面的一致】：", "[ Is the newly created RSA with PEM consistent with the above ]:");
			Assert("XML：", rsa2.ToXML() == rsa.ToXML());
			Assert("PKCS1：", rsa2.ToPEM().ToPEM_PKCS1() == pem.ToPEM_PKCS1());
			Assert("PKCS8：", rsa2.ToPEM().ToPEM_PKCS8() == pem.ToPEM_PKCS8());

			//用xml文本创建RSA
			var rsa3 = new RSA_Util(RSA_PEM.FromXML(xml));
			ST("【用XML新创建的RSA是否和上面的一致】：", "[ Is the newly created RSA with XML consistent with the above ]:");
			Assert("XML：", rsa3.ToXML() == rsa.ToXML());
			Assert("PKCS1：", rsa3.ToPEM().ToPEM_PKCS1() == pem.ToPEM_PKCS1());
			Assert("PKCS8：", rsa3.ToPEM().ToPEM_PKCS8() == pem.ToPEM_PKCS8());

			//--------RSA_PEM私钥验证---------
			//使用PEM全量参数构造pem对象
			RSA_PEM pemX = new RSA_PEM(pem.Key_Modulus, pem.Key_Exponent, pem.Key_D, pem.Val_P, pem.Val_Q, pem.Val_DP, pem.Val_DQ, pem.Val_InverseQ);
			ST("【RSA_PEM是否和原始RSA一致】：", "[ Is RSA_PEM consistent with the original RSA ]:");
			S(pemX.KeySize + T("位", " bits"));
			Assert("XML：", pemX.ToXML(false) == pem.ToXML(false));
			Assert("PKCS1：", pemX.ToPEM_PKCS1() == pem.ToPEM_PKCS1());
			Assert("PKCS8：", pemX.ToPEM_PKCS8() == pem.ToPEM_PKCS8());
			ST("仅公钥：", "Public Key Only:");
			Assert("XML：", pemX.ToXML(true) == pem.ToXML(true));
			Assert("PKCS1：", pemX.ToPEM_PKCS1(true) == pem.ToPEM_PKCS1(true));
			Assert("PKCS8：", pemX.ToPEM_PKCS8(true) == pem.ToPEM_PKCS8(true));

			//--------RSA_PEM公钥验证---------
			RSA_PEM pemY = new RSA_PEM(pem.Key_Modulus, pem.Key_Exponent, null);
			ST("【RSA_PEM仅公钥是否和原始RSA一致】：", "[ RSA_PEM only public key is consistent with the original RSA ]:");
			S(pemY.KeySize + T("位", " bits"));
			Assert("XML：", pemY.ToXML(false) == pem.ToXML(true));
			Assert("PKCS1：", pemY.ToPEM_PKCS1() == pem.ToPEM_PKCS1(true));
			Assert("PKCS8：", pemY.ToPEM_PKCS8() == pem.ToPEM_PKCS8(true));

			if (!fast) {
				//使用n、e、d构造pem对象
				RSA_PEM pem4 = new RSA_PEM(pem.Key_Modulus, pem.Key_Exponent, pem.Key_D);
				RSA_Util rsa4 = new RSA_Util(pem4);
				ST("【用n、e、d构造解密】", "[ Construct decryption with n, e, d ]");
				de = rsa4.Decrypt("PKCS1", en);
				AssertMsg(de, de == str);
				AssertMsg(T("校验 OK", "Verify OK"), rsa4.Verify("SHA1", sign, str));


				//对调交换公钥私钥
				ST("【Unsafe|对调公钥私钥，私钥加密公钥解密】", "[ Unsafe | Swap the public key and private key, private key encryption and public key decryption ]");
				var rsaPri = rsa.SwapKey_Exponent_D__Unsafe();
				var rsaPub = new RSA_Util(rsa.ToPEM(true)).SwapKey_Exponent_D__Unsafe();
				if (!RSA_Util.IsUseBouncyCastle) {
					rsaPub = rsaPri;
					ST(".NET自带的RSA不支持仅含公钥的密钥进行解密和签名，使用NoPadding填充方式或IsUseBouncyCastle时无此问题", "The RSA that comes with .NET does not support decryption and signing with keys containing only public keys. This problem does not occur when using NoPadding or IsUseBouncyCastle.");
				}
				try {
					var enPri = rsaPri.Encrypt("PKCS1", str);
					var signPub = rsaPub.Sign("SHA1", str);
					de = rsaPub.Decrypt("PKCS1", enPri);
					AssertMsg(de, de == str);
					AssertMsg(T("校验 OK", "Verify OK"), rsaPri.Verify("SHA1", signPub, str));
				} catch (Exception e) {
					if (!RSA_Util.IS_CoreOr46 && !RSA_Util.IsUseBouncyCastle) {
						S(T("不支持在RSACryptoServiceProvider中使用：", "Not supported in RSACryptoServiceProvider: ") + e.Message);
					} else {
						throw e;
					}
				}

				rsa4 = rsaPri.SwapKey_Exponent_D__Unsafe();
				de = rsa4.Decrypt("PKCS1", en);
				AssertMsg(de, de == str);
				AssertMsg(T("校验 OK", "Verify OK"), rsa4.Verify("SHA1", sign, str));
			}


			if (!fast) {
				S();
				ST("【测试一遍所有的加密、解密填充方式】  按回车键继续测试...", "[ Test all the encryption and decryption padding mode ]   Press Enter to continue testing...");
				ReadIn();
				RSA_Util rsa5 = new RSA_Util(2048);
				testPaddings(false, rsa5, new RSA_Util(rsa5.ToPEM(true)), true);
			}
		}
		static Type Type_RuntimeInformation(Type[] outOSPlatform) {
#if (RSA_BUILD__NET_CORE || NETCOREAPP || NETSTANDARD || NET) //csproj:PropertyGroup.DefineConstants + https://docs.microsoft.com/zh-cn/dotnet/csharp/language-reference/preprocessor-directives
			if (outOSPlatform != null) outOSPlatform[0] = typeof(OSPlatform);
			return typeof(RuntimeInformation);
#else
			//.NET Framework 4.7.1 才有 都在mscorlib.dll里面
			var type = typeof(ComVisibleAttribute).Assembly.GetType("System.Runtime.InteropServices.RuntimeInformation");
			if (type != null && outOSPlatform != null) outOSPlatform[0] = type.Assembly.GetType("System.Runtime.InteropServices.OSPlatform");
			return type;
#endif
		}
		static bool NET_IsWindows() {
			Type[] typeOSPlatform = new Type[1];
			Type type = Type_RuntimeInformation(typeOSPlatform);
			if (type != null) {
				dynamic a1 = typeOSPlatform[0].GetProperty("Windows").GetValue(null);
				return (bool)RSA_Util.FindFunc(type, "IsOSPlatform", new string[] { "os" }).Invoke(null, new object[] { a1 });
			}
			var ver = Environment.OSVersion.VersionString.ToLower();
			return ver.Contains("microsoft") && ver.Contains("windows");
		}
		static string NET_Ver() {
			string val, os;
			Type type = Type_RuntimeInformation(null);
			if (type != null) {
				val = (string)type.GetProperty("FrameworkDescription").GetValue(null);
				os = (string)type.GetProperty("OSDescription").GetValue(null);
			} else {
				val = "EnvVer-" + Environment.Version;
				os = (NET_IsWindows() ? "Windows" : "Linux?");
			}
			val += " | " + os;
			return val;
		}




		static void Assert(string msg, bool check) {
			AssertMsg(msg + check, check);
		}
		static void AssertMsg(string msg, bool check) {
			if (!check) throw new Exception(msg);
			Console.WriteLine(msg);
		}


		/// <summary>控制台输出一个换行</summary>
		static private void S() {
			Console.WriteLine();
		}
		/// <summary>控制台输出内容</summary>
		static private void S(string s) {
			Console.WriteLine(s);
		}
		/// <summary>控制台输出内容 + 简版多语言支持，根据当前语言返回中文或英文，简化调用<see cref="RSA_PEM.T(string, string)"/> </summary>
		static private void ST(string zh, string en) {
			Console.WriteLine(T(zh, en));
		}
		/// <summary>简版多语言支持，根据当前语言返回中文或英文，简化调用<see cref="RSA_PEM.T(string, string)"/> </summary>
		static private string T(string zh, string en) {
			return RSA_PEM.T(zh, en);
		}
		static string ReadIn() {
			return Console.ReadLine();
		}
		static string ReadPath(string tips, string tips2) {
			while (true) {
				ST("请输入" + tips + "路径" + tips2 + ": ", "Please enter " + tips + " path" + tips2 + ":");
				Console.Write("> ");
				string path = ReadIn().Trim();
				if (path.Length == 0 || path.StartsWith("+")) {
					return path;
				}
				if (!File.Exists(path) && !Directory.Exists(path)) {
					ST("文件[" + path + "]不存在", "File [" + path + "] does not exist");
					continue;
				}
				return path;
			}
		}
		static byte[] ReadFile(string path) {
			return File.ReadAllBytes(path);
		}
		static void WriteFile(string path, byte[] val) {
			File.WriteAllBytes(path, val);
		}
		static readonly string HR = "---------------------------------------------------------";


		static private Assembly Bc__Assembly = null;
		static private string[] Bc__Dlls = new string[] {
			"BouncyCastle.Crypto.dll", "BouncyCastle.Cryptography.dll"
		};
		static bool CanLoad_BouncyCastle() {
			if (Bc__Assembly != null) return true;
			Assembly bc = null;
			foreach (var dll in Bc__Dlls) {
				try {
					bc = Assembly.LoadFrom(dll);
					Bc__Assembly = bc;
					break;
				} catch { }
			}
			return bc != null;
		}
		static void printEnv() {
			S(".NET Version: " + NET_Ver() + "   RSA_PEM.Lang=" + RSA_PEM.Lang);
			if (RSA_Util.IsUseBouncyCastle) return;

			var errs = "";
			if (!RSA_Util.IS_CoreOr46) {
				errs += errs.Length > 0 ? T("、", ", ") : "";
				errs += T("除OAEP+SHA1以外的所有OAEP加密填充模式", "All OAEP encryption padding modes except for OAEP+SHA1");

				errs += errs.Length > 0 ? T("、", ", ") : "";
				errs += T("PSS签名填充模式（其他填充模式不影响）", "PSS signature padding mode (other padding modes do not affect)");
			}

			bool _;
			if (!SupportHash("SHA-512/256", false, out _)) {
				errs += errs.Length > 0 ? T("、", ", ") : "";
				errs += T("SHA-512/224（/256）摘要算法", "SHA-512/224 (/256) digest algorithm");
			}
			if (!SupportHash("SHA3-256", false, out _)) {
				errs += errs.Length > 0 ? T("、", ", ") : "";
				errs += T("SHA3系列摘要算法", "SHA3 series digest algorithm");
			}
			ST("*** .NET不支持NoPadding加密填充模式、不支持SHA-512/224（/256）摘要算法、需要.NET8以上才支持SHA3系列摘要算法，可通过引入BouncyCastle加密增强库来扩充.NET加密功能。", "*** .NET does not support the NoPadding encryption padding mode, does not support the SHA-512/224 (/256) digest algorithm, and requires .NET8 or higher to support the SHA3 series digest algorithm. You can expand the .NET encryption function by introducing the BouncyCastle encryption enhancement library.");
			if (errs.Length > 0) {
				ST("*** 当前.NET版本太低，不支持：" + errs + "；如需获得这些功能支持，解决办法1：升级使用高版本.NET来运行本测试程序(可能支持)；解决办法2：引入BouncyCastle即可得到全部支持。", "*** The current .NET version is too low and does not support: " + errs + "; if you need to obtain support for these functions, solution 1: upgrade to a higher version of .NET to run this test program (may be supported); solution 2: Full support is available with the introduction of BouncyCastle.");
			}
			ST("*** 如需获得全部加密签名模式支持，可按此方法引入BouncyCastle加密增强库：到 https://www.nuget.org/packages/Portable.BouncyCastle 下载得到NuGet包（或使用 BouncyCastle.Cryptography 包），用压缩软件提取其中lib目录内对应.NET版本下的BouncyCastle.Crypto.dll，放到本测试程序目录内，然后通过测试菜单B进行注册即可得到全部支持。", "*** If you need full encryption and signature mode support, you can introduce BouncyCastle encryption enhancement library in this way: Go to https://www.nuget.org/packages/Portable.BouncyCastle to download the NuGet package (or use the BouncyCastle.Cryptography package), and use compression software to extract it The lib directory corresponds to BouncyCastle.Crypto.dll under the .NET version, place it in the directory of this test program, and then register it through test menu B to get full support.");
		}
		static bool SupportHash(string hash, bool checkBc, out bool isBc) {
			object obj = RSA_Util.HashFromName(hash);
			var val = obj != null;
			isBc = false;
			if (val || !checkBc) {
				return val;
			}
			if (BcAssembly != null) {
				try {
					obj = BcAssembly.GetType("Org.BouncyCastle.Security.DigestUtilities").GetMethod("GetDigest", new Type[] { typeof(string) }).Invoke(null, new object[] { hash });
				} catch {
					obj = null;
				}
				if (obj != null) {
					isBc = true;
					return true;
				}
			}
			return val;
		}
		static Assembly BcAssembly = null;
		static void testProvider(bool checkOpenSSL) {
			if (CanLoad_BouncyCastle()) {
				if (BcAssembly == null) {
					ST("检测到BouncyCastle加密增强库，是否要进行注册？(Y/N) Y", "The BouncyCastle encryption enhancement library is detected, do you want to register? (Y/N) Y");
				} else {
					ST("已注册BouncyCastle加密增强库，是否要保持注册？(Y/N) Y", "BouncyCastle encryption enhancement library has been registered, do you want to keep it registered? (Y/N) Y");
				}
				Console.Write("> ");
				string val = ReadIn().Trim().ToUpper();
				try {
					if (BcAssembly == null && "N" != val) {
						BcAssembly = Bc__Assembly;
						RSA_Util.UseBouncyCastle(BcAssembly);
						ST("已注册BouncyCastle加密增强库", "BouncyCastle encryption enhancement library registered");
					}
					if (BcAssembly != null && "N" == val) {
						RSA_Util.UseBouncyCastle(null);
						BcAssembly = null;
						ST("已取消注册BouncyCastle加密增强库", "Unregistered BouncyCastle encryption enhancement library");
					}
				} catch (Exception e) {
					S(T("BouncyCastle操作失败：", "BouncyCastle operation failed: ") + e.Message);
				}
			}
			printEnv();
			S();

			RSA_Util rsa = new RSA_Util(2048);
			string[] Hashs = new string[] {
				"SHA-1","SHA-256","SHA-224","SHA-384","SHA-512"
				,"SHA3-256","SHA3-224","SHA3-384","SHA3-512"
				,"SHA-512/224","SHA-512/256","MD5"
			};

			S("MessageDigest" + T("支持情况：", " support status:"));
			{
				var Ss = new List<string>(Hashs);
				Ss.Add("MD2");
				Ss.Add("SHAKE128"); Ss.Add("SHAKE256");//https://blog.csdn.net/weixin_42579622/article/details/111644921
				foreach (var s in Ss) {
					var key = s; bool isBc;

					if (SupportHash(key, true, out isBc)) {
						S("      " + key + " | Provider: " + (isBc ? "BouncyCastle" : ".NET"));
					} else {
						S("  [x] " + key);
					}
				}
			}

			S("Encrypt Padding Mode" + T("支持情况：", " support status:"));
			for (int i = 0; i < 1; i++) {
				var v1 = i == 9999 ? "NONE" : "ECB";
				var Ss = new List<string>(new string[] {"NoPadding"
					,"PKCS1Padding"
					,"OAEPPadding"});
				foreach (var s in Hashs) {
					Ss.Add("OAEPwith" + s + "andMGF1Padding");
				}
				foreach (var s in Ss) {
					string key = "RSA/" + v1 + "/" + s, key2 = key;
					RSA_Util.UseBouncyCastle(null);
					for (var n = 0; n < 2; n++) {
						try {
							rsa.Encrypt(key, "123");
							S("      " + key + " | Provider: " + (n == 1 ? "BouncyCastle" : ".NET"));
						} catch {
							if (n == 0 && BcAssembly != null) {
								RSA_Util.UseBouncyCastle(BcAssembly);
								continue;
							}
							S("  [x] " + key);
						}
						break;
					}
					RSA_Util.UseBouncyCastle(BcAssembly);
				}
			}

			S("Signature Padding Mode" + T("支持情况：", " support status:"));
			for (int i = 0; i < 3; i++) {
				string v2 = i == 1 ? "/PSS" : "";
				string[] Ss = i == 2 ? new string[] { "RSASSA-PSS" } : Hashs;
				foreach (var s in Ss) {
					string key = i == 2 ? s : (s.Replace("SHA-", "SHA") + "withRSA" + v2), key2 = key;
					RSA_Util.UseBouncyCastle(null);
					for (var n = 0; n < 2; n++) {
						try {
							rsa.Sign(key, "123");
							S("      " + key + " | Provider: " + (n == 1 ? "BouncyCastle" : ".NET"));
						} catch {
							if (n == 0 && BcAssembly != null) {
								RSA_Util.UseBouncyCastle(BcAssembly);
								continue;
							}
							S("  [x] " + key);
						}
						break;
					}
					RSA_Util.UseBouncyCastle(BcAssembly);
				}
			}

			S(HR);
			ST("测试一遍所有的加密、解密填充方式：", "Test all the encryption and decryption padding mode:");
			testPaddings(checkOpenSSL, rsa, new RSA_Util(rsa.ToPEM(true)), true);

			S(HR);
			ST("Unsafe|是否要对调公钥私钥（私钥加密公钥解密）重新测试一遍？(Y/N) N", "Unsafe | Do you want to swap the public and private keys (private key encryption and public key decryption) and test again? (Y/N) N");
			Console.Write("> ");
			string yn = ReadIn().Trim().ToUpper();
			if (yn == "Y") {
				var rsaPri = rsa.SwapKey_Exponent_D__Unsafe();
				var rsaPub = new RSA_Util(rsa.ToPEM(true)).SwapKey_Exponent_D__Unsafe();
				testPaddings(checkOpenSSL, rsaPub, rsaPri, true);
			}
		}
		/// <summary>测试一遍所有的加密、解密填充方式</summary>
		static int testPaddings(bool checkOpenSSL, RSA_Util rsaPri, RSA_Util rsaPub, bool log) {
			int errCount = 0;
			var errMsgs = new List<string>();
			var txt = "1234567890";
			if (!checkOpenSSL) {
				txt += txt + txt + txt + txt; txt += txt;//100
				txt += txt + txt + txt + txt; txt += txt + "a";//1001
			}
			byte[] txtData = Encoding.UTF8.GetBytes(txt);

			if (checkOpenSSL) {
				try {
					runOpenSSL(rsaPri.HasPrivate ? rsaPri : rsaPub, txtData);
				} catch (Exception e) {
					S(T("运行OpenSSL失败：", "Failed to run OpenSSL: ") + e.Message);
					return errCount;
				}
			}

			var encKeys = RSA_Util.RSAPadding_Enc_DefaultKeys();
			foreach (var type in encKeys) {
				var errMsg = "";
				try {
					{
						byte[] enc = rsaPub.Encrypt(type, txtData);
						byte[] dec = rsaPri.Decrypt(type, enc);
						bool isOk = true;
						if (dec.Length != txtData.Length) {
							isOk = false;
						} else {
							for (int i = 0; i < dec.Length; i++) {
								if (dec[i] != txtData[i]) {
									isOk = false; break;
								}
							}
						}
						if (!isOk) {
							errMsg = T("解密结果不一致", "Decryption results are inconsistent");
							throw new Exception(errMsg);
						}
					}
					if (checkOpenSSL) {
						byte[] enc;
						try {
							enc = testOpenSSL(true, type);
						} catch (Exception e) {
							errMsg = "+OpenSSL: " + T("OpenSSL加密出错", "OpenSSL encryption error");
							throw e;
						}
						byte[] dec = rsaPri.Decrypt(type, enc);
						bool isOk = true;
						if (dec.Length != txtData.Length) {
							isOk = false;
						} else {
							for (int i = 0; i < dec.Length; i++) {
								if (dec[i] != txtData[i]) {
									isOk = false; break;
								}
							}
						}
						if (!isOk) {
							errMsg = "+OpenSSL: " + T("解密结果不一致", "Decryption results are inconsistent");
							throw new Exception(errMsg);
						}
					}
					if (log) {
						S("     " + (checkOpenSSL ? " [+OpenSSL]" : "") + " " + T("加密解密：", "Encryption decryption: ") + type + " | " + RSA_Util.RSAPadding_Enc(type));
					}
				} catch (Exception e) {
					if (!log && RSA_Util.IsDotNetSupportError(e.Message)) {
						//NOOP
					} else {
						errCount++;
						if (errMsg.Length == 0) errMsg = T("加密解密出现异常", "An exception occurred in encryption decryption");
						errMsg = "  [x] " + errMsg + ": " + type + " | " + RSA_Util.RSAPadding_Enc(type);
						S(errMsg);
						errMsgs.Add(errMsg + T("。", ". ") + e.Message);
					}
				}
			}

			var signKeys = RSA_Util.RSAPadding_Sign_DefaultKeys();
			foreach (var type in signKeys) {
				var errMsg = "";
				try {
					{
						byte[] sign = rsaPri.Sign(type, txtData);
						var isOk = rsaPub.Verify(type, sign, txtData);
						if (!isOk) {
							errMsg = T("未通过校验", "Failed verification");
							throw new Exception(errMsg);
						}
					}
					if (checkOpenSSL) {
						byte[] sign;
						try {
							sign = testOpenSSL(false, type);
						} catch (Exception e) {
							errMsg = "+OpenSSL: " + T("OpenSSL签名出错", "OpenSSL signature error");
							throw e;
						}
						var isOk = rsaPub.Verify(type, sign, txtData);
						if (!isOk) {
							errMsg = "+OpenSSL: " + T("未通过校验", "Failed verification");
							throw new Exception(errMsg);
						}
					}
					if (log) {
						S("     " + (checkOpenSSL ? " [+OpenSSL]" : "") + " " + T("签名验证：", "Signature verification: ") + type + " | " + RSA_Util.RSAPadding_Sign(type));
					}
				} catch (Exception e) {
					if (!log && RSA_Util.IsDotNetSupportError(e.Message)) {
						//NOOP
					} else {
						errCount++;
						if (errMsg.Length == 0) errMsg = T("签名验证出现异常", "An exception occurred in signature verification");
						errMsg = "  [x] " + errMsg + ": " + type + " | " + RSA_Util.RSAPadding_Sign(type);
						S(errMsg);
						errMsgs.Add(errMsg + T("。", ". ") + e.Message);
					}
				}
			}
			if (log) {
				if (errMsgs.Count == 0) {
					ST("填充方式全部测试通过。", "All padding mode tests passed.");
				} else {
					ST("按回车键显示详细错误消息...", "Press Enter to display detailed error message...");
					ReadIn();
				}
			}
			if (errMsgs.Count > 0) {
				S(string.Join("\n", errMsgs));
			}
			closeOpenSSL();
			return errCount;
		}
		/// <summary>多线程并发调用同一个RSA</summary>
		static void threadRun() {
			int ThreadCount = Math.Max(5, Environment.ProcessorCount - 1);
			bool Abort = false;
			int Count = 0;
			int ErrCount = 0;
			RSA_Util rsa = new RSA_Util(2048);
			RSA_Util rsaPub = new RSA_Util(rsa.ToPEM(true));
			S(T("正在测试中，线程数：", "Under test, number of threads: ") + ThreadCount + T("，按回车键结束测试...", ", press enter to end the test..."));

			for (int i = 0; i < ThreadCount; i++) {
				new Thread(() => {
					while (!Abort) {
						int err = testPaddings(false, rsa, rsaPub, false);
						if (err > 0) {
							Interlocked.Add(ref ErrCount, err);
						}
						Interlocked.Increment(ref Count);
					}
				}).Start();
			}

			long t1 = DateTime.Now.Ticks;
			new Thread(() => {
				while (!Abort) {
					Console.Write("\r" + T("已测试" + Count + "次，", "Tested " + Count + " times, ")
							+ ErrCount + T("个错误，", " errors, ")
							+ T("耗时", "") + (DateTime.Now.Ticks - t1) / 10000 / 1000 + T("秒", " seconds total"));
					try {
						Thread.Sleep(1000);
					} catch { }
				}
			}).Start();

			ReadIn();
			Abort = true;
			ST("多线程并发调用同一个RSA测试已结束。", "Multiple threads concurrently calling the same RSA test is over.");
			S();
		}



		static void keyTools() {
			ST("===== RSA密钥工具：生成密钥、转换密钥格式 ====="
		 , "===== RSA key tool: generate key, convert key format =====");
			ST("请使用下面可用命令进行操作，命令[]内的为可选参数，参数可用\"\"包裹。", "Please use the following commands to operate. The parameters in the command `[]` are optional parameters, and the parameters can be wrapped with \"\".");
			S(HR);
			S("`new 1024 [-pkcs8] [saveFile [puboutFile]]`: " + T("生成新的RSA密钥，指定位数和格式：xml、pkcs1、或pkcs8（默认），提供saveFile可保存私钥到文件，提供puboutFile可额外保存一个公钥文件", "Generate a new RSA key, specify the number of digits and format: xml, pkcs1, or pkcs8 (default), provide saveFile to save the private key to a file, and provide puboutFile to save an additional public key file"));
			S(HR);
			S("`convert -pkcs1 [-pubout] [-swap] oldFile [newFile]`: " + T("转换密钥格式，提供已有密钥文件oldFile（支持xml、pem格式公钥或私钥），指定要转换成的格式：xml、pkcs1、或pkcs8，提供了-pubout时只导出公钥，提供了-swap时交换公钥指数私钥指数（非常规的：私钥加密公钥解密），提供newFile可保存到文件", "To convert the key format, provide the existing key file oldFile (support xml, pem format public key or private key), specify the format to be converted into: xml, pkcs1, or pkcs8, only export the public key when -pubout is provided, swap public key exponent and private key exponent when -swap is provided (unconventional: private key encryption and public key decryption), and provide newFile Can save to file"));
			S(HR);
			S("`exit`: " + T("输入 exit 退出工具", "Enter exit to quit the tool"));
			while (true) {
			loop:
				Console.Write("> ");
				var inStr = ReadIn().Trim();
				if (inStr.Length == 0) {
					ST("输入为空，请重新输入！如需退出请输入exit", "The input is empty, please re-enter! If you need to exit, please enter exit");
					continue;
				}
				if (inStr.ToLower() == "exit") {
					ST("bye! 已退出。", "bye! has exited.");
					S();
					return;
				}
				var args = new List<string>();
				Regex exp = new Regex("(-?)(?:([^\"\\s]+)|\"(.*?)\")\\s*");
				var sb = exp.Replace(inStr, (m) => {
					var m1 = m.Groups[1].Value;
					var m2 = m.Groups[2] == null ? "" : m.Groups[2].Value;
					if (m2.Length > 0) {
						args.Add(m1 + m2);
					} else {
						args.Add(m1 + m.Groups[3].Value);
					}
					return "";
				});
				if (sb.Length > 0) {
					ST("参数无效：" + sb, "Invalid parameter: " + sb);
					continue;
				}

				var cmdName = args[0].ToLower(); args.RemoveAt(0);
				bool nextSave = false;
				RSA_Util rsa = null; string type = "", save = "", save2 = ""; bool pubOut = false;

				if (cmdName == "new") {// 生成新的pem密钥
					type = "pkcs8"; string len = "";
					while (args.Count > 0) {
						string param = args[0], p = param.ToLower(); args.RemoveAt(0);

						var m = new Regex("^(\\d+)$").Match(p);
						if (m.Success) { len = m.Groups[1].Value; continue; }

						m = new Regex("^-(xml|pkcs1|pkcs8)$").Match(p);
						if (m.Success) { type = m.Groups[1].Value; continue; }

						if (save.Length == 0 && !p.StartsWith("-")) { save = param; continue; }
						if (save2.Length == 0 && !p.StartsWith("-")) { save2 = param; continue; }

						ST("未知参数：" + param, "Unknown parameter: " + param);
						goto loop;
					}
					if (len.Length == 0) { ST("请提供密钥位数！", "Please provide key digits!"); goto loop; }
					try {
						rsa = new RSA_Util(Convert.ToInt32(len));
					} catch (Exception e) {
						S(T("生成密钥出错：", "Error generating key: ") + e.Message);
						goto loop;
					}
					nextSave = true;
				}

				if (cmdName == "convert") {// 转换密钥格式
					string old = ""; bool swap = false;
					while (args.Count > 0) {
						string param = args[0], p = param.ToLower(); args.RemoveAt(0);

						var m = new Regex("^-(xml|pkcs1|pkcs8)$").Match(p);
						if (m.Success) { type = m.Groups[1].Value; continue; }

						if (p == "-pubout") { pubOut = true; continue; }
						if (p == "-swap") { swap = true; continue; }

						if (old.Length == 0 && !p.StartsWith("-")) { old = param; continue; }

						if (save.Length == 0 && !p.StartsWith("-")) { save = param; continue; }

						ST("未知参数：" + param, "Unknown parameter: " + param);
						goto loop;
					}
					if (type.Length == 0) { ST("请提供要转换成的格式！", "Please provide the format to convert to!"); goto loop; }
					if (old.Length == 0) { ST("请提供已有密钥文件！", "Please provide an existing key file!"); goto loop; }
					try {
						var oldTxt = Encoding.UTF8.GetString(ReadFile(old));
						rsa = new RSA_Util(oldTxt);
						if (swap) rsa = rsa.SwapKey_Exponent_D__Unsafe();
					} catch (Exception e) {
						S(T("读取密钥文件出错", "Error reading key file ") + " (" + old + "): " + e.Message);
						goto loop;
					}
					nextSave = true;
				}

				while (nextSave) {
					string val;
					if (type == "xml") {
						val = rsa.ToXML(pubOut);
					} else {
						bool pkcs8 = type == "pkcs8";
						val = rsa.ToPEM(false).ToPEM(pubOut, pkcs8, pkcs8);
					}
					if (save.Length == 0) {
						S(val);
					} else {
						save = Path.GetFullPath(save);
						try {
							WriteFile(save, Encoding.UTF8.GetBytes(val));
						} catch (Exception e) {
							S(T("保存文件出错", "Error saving file ") + " (" + save + "): " + e.Message);
						}
						S(T("密钥文件已保存到：", "The key file has been saved to: ") + save);
					}
					if (save2.Length > 0) {
						save = save2; save2 = "";
						pubOut = true;
						continue;
					}
					S();
					goto loop;
				}
				ST("未知命令：" + cmdName, "Unknown command: " + cmdName);
			}
		}


		static RSA_PEM loadKey = null; static string loadKeyFile = "";
		/// <summary>设置：加载密钥PEM文件</summary>
		static void setLoadKey() {
			string path = ReadPath(T("密钥文件", "Key File")
				, T("，或文件夹（内含private.pem、test.txt）。或输入'+1024 pkcs8'生成一个新密钥（填写位数、pkcs1、pkcs8）", ", or a folder (containing private.pem, test.txt). Or enter '+1024 pkcs8' to generate a new key (fill in digits, pkcs1, pkcs8) "));
			if (path.StartsWith("+")) {//创建一个新密钥
				Match m = new Regex("^\\+(\\d+)\\s+pkcs([18])$", RegexOptions.IgnoreCase).Match(path);
				if (!m.Success) {
					ST("格式不正确，请重新输入！", "The format is incorrect, please re-enter!");
					setLoadKey();
				} else {
					int keySize = Convert.ToInt32(m.Groups[1].Value);
					RSA_Util rsa = new RSA_Util(keySize);
					bool isPkcs8 = m.Groups[2].Value == "8";
					RSA_PEM pem = rsa.ToPEM(false);
					S(keySize + T("位私钥已生成，请复制此文本保存到private.pem文件：", " bit private key has been generated. Please copy this text and save it to the private.pem file:"));
					S(pem.ToPEM(false, isPkcs8, isPkcs8));
					S(keySize + T("位公钥已生成，请复制此文本保存到public.pem文件：", " bit public key has been generated. Please copy this text and save it to the public.pem file:"));
					S(pem.ToPEM(true, isPkcs8, isPkcs8));
					waitAnyKey = true;
				}
				return;
			}
			if (path.Length == 0 && loadKeyFile.Length == 0) {
				ST("未输入文件，已取消操作", "No file input, operation cancelled");
				return;
			}
			if (path.Length == 0) {
				path = loadKeyFile;
				ST("重新加载密钥文件", "Reload key file");
			}

			if (Directory.Exists(path)) {
				string txtPath = path + Path.DirectorySeparatorChar + "test.txt";
				path = path + Path.DirectorySeparatorChar + "private.pem";
				if (!File.Exists(path)) {
					ST("此文件夹中没有private.pem文件！", "There is no private.pem file in this folder!");
					setLoadKey();
					return;
				}
				if (File.Exists(txtPath)) {//顺带加载文件夹里面的目标源文件
					loadSrcBytes = ReadFile(txtPath);
					loadSrcFile = txtPath;
				}
			}
			string txt = Encoding.UTF8.GetString(ReadFile(path));
			loadKey = RSA_PEM.FromPEM(txt);
			loadKeyFile = path;
		}

		static byte[] loadSrcBytes = null; static string loadSrcFile = "";
		/// <summary>设置：加载目标源文件</summary>
		static void setLoadSrcBytes() {
			string path = ReadPath(T("目标源文件", "Target Source File"), "");
			if (path.Length == 0 && loadSrcFile.Length == 0) {
				ST("未输入文件，已取消操作", "No file input, operation cancelled");
				return;
			}
			if (path.Length == 0) {
				path = loadSrcFile;
				ST("重新加载目标源文件", "Reload target source file");
			}
			loadSrcBytes = ReadFile(path);
			loadSrcFile = path;
		}

		static string encType = "";
		/// <summary>设置加密填充模式</summary>
		static bool setEncType() {
			S(T("请输入加密填充模式", "Please enter the encryption Padding mode")
			+ (encType.Length > 0 ? T("，回车使用当前值", ", press Enter to use the current value ") + encType : "")
			+ T("；填充模式取值可选：", "; Padding mode values: ") + string.Join(", ", RSA_Util.RSAPadding_Enc_DefaultKeys())
			+ T(", 或其他支持的值", ", or other supported values"));
			Console.Write("> ");
			string val = ReadIn().Trim();
			if (val.Length > 0) {
				encType = val;
			}
			if (encType.Length == 0) {
				ST("未设置，已取消操作", "Not set, operation canceled");
			}
			return encType.Length > 0;
		}
		/// <summary>加密</summary>
		static void execEnc() {
			string save = loadSrcFile + ".enc.bin";
			S(T("密钥文件：", "Key file: ") + loadKeyFile);
			S(T("目标文件：", "Target file: ") + loadSrcFile);
			S(T("填充模式：", "Padding mode: ") + encType + " | " + RSA_Util.RSAPadding_Enc(encType));
			ST("正在加密目标源文件...", "Encrypting target source file...");
			RSA_Util rsa = new RSA_Util(loadKey);
			long t1 = DateTime.Now.Ticks;
			byte[] data = rsa.Encrypt(encType, loadSrcBytes);
			S(T("加密耗时：", "Encryption time: ") + (DateTime.Now.Ticks - t1) / 10000 + "ms");
			WriteFile(save, data);
			S(T("已加密，结果已保存：", "Encrypted, the result is saved: ") + save);
		}
		/// <summary>解密对比</summary>
		static void execDec() {
			string encPath = loadSrcFile + ".enc.bin";
			S(T("密钥文件：", "Key file: ") + loadKeyFile);
			S(T("密文文件：", "Ciphertext file: ") + encPath);
			S(T("对比文件：", "Compare files: ") + loadSrcFile);
			S(T("填充模式：", "Padding mode: ") + encType + " | " + RSA_Util.RSAPadding_Enc(encType));
			byte[]
			data = ReadFile(encPath);
			ST("正在解密文件...", "Decrypting file...");
			RSA_Util rsa = new RSA_Util(loadKey);
			long t1 = DateTime.Now.Ticks;
			byte[] val = rsa.Decrypt(encType, data);
			S(T("解密耗时：", "Decryption time: ") + (DateTime.Now.Ticks - t1) / 10000 + "ms");
			WriteFile(loadSrcFile + ".dec.txt", val);
			bool isOk = true;
			if (val.Length != loadSrcBytes.Length) {
				isOk = false;
			} else {
				for (int i = 0; i < val.Length; i++) {
					if (val[i] != loadSrcBytes[i]) {
						isOk = false; break;
					}
				}
			}
			if (isOk) {
				ST("解密成功，和对比文件的内容一致。", "The decryption is successful, which is consistent with the content of the comparison file.");
				return;
			}
			throw new Exception(T("解密结果和对比文件的内容不一致！", "The decryption result is inconsistent with the content of the comparison file!"));
		}


		static string signType = "";
		/// <summary>设置签名hash+填充模式</summary>
		static bool setSignType() {
			S(T("请输入签名Hash+填充模式", "Please enter the signature Hash+Padding mode")
				+ (signType.Length > 0 ? T("，回车使用当前值", ", press Enter to use the current value ") + signType : "")
				+ T("；签名模式取值可选：", "; Signature mode values: ") + string.Join(", ", RSA_Util.RSAPadding_Sign_DefaultKeys())
				+ T(", 或其他支持的值", ", or other supported values"));
			Console.Write("> ");
			string val = ReadIn().Trim();
			if (val.Length > 0) {
				signType = val;
			}
			if (signType.Length == 0) {
				ST("未设置，已取消操作", "Not set, operation canceled");
			}
			return signType.Length > 0;
		}
		/// <summary>签名</summary>
		static void execSign() {
			string save = loadSrcFile + ".sign.bin";
			S(T("密钥文件：", "Key file: ") + loadKeyFile);
			S(T("目标文件：", "Target file: ") + loadSrcFile);
			S(T("签名模式：", "Signature mode: ") + signType + " | " + RSA_Util.RSAPadding_Sign(signType));
			ST("正在给目标源文件签名...", "Signing target source file...");
			RSA_Util rsa = new RSA_Util(loadKey);
			byte[] data = rsa.Sign(signType, loadSrcBytes);
			WriteFile(save, data);
			S(T("已签名，结果已保存：", "Signed, results saved: ") + save);
		}
		/// <summary>验证签名</summary>
		static void execVerify() {
			string binPath = loadSrcFile + ".sign.bin";
			S(T("密钥文件：", "Key file: ") + loadKeyFile);
			S(T("目标文件：", "Target file: ") + loadSrcFile);
			S(T("签名文件：", "Signature file: ") + binPath);
			S(T("签名模式：", "Signature mode: ") + signType + " | " + RSA_Util.RSAPadding_Sign(signType));
			byte[] data = ReadFile(binPath);
			ST("正在验证签名...", "Verifying signature...");
			RSA_Util rsa = new RSA_Util(loadKey);
			bool val = rsa.Verify(signType, data, loadSrcBytes);
			if (val) {
				ST("签名验证成功。", "Signature verification successful.");
				return;
			}
			throw new Exception(T("签名验证失败！", "Signature verification failed!"));
		}





		/// <summary>调用openssl相关测试代码</summary>
		static void runOpenSSL(RSA_Util rsa, byte[] data) {
			var shell = "/bin/bash";
			if (NET_IsWindows()) {
				shell = "cmd";
			}

			S(T("正在打开OpenSSL...", "Opening OpenSSL...") + "    Shell: " + shell);
			closeOpenSSL();
			openSSLProc = new Process();
			ProcessStartInfo info = openSSLProc.StartInfo;
			info.FileName = shell;
			info.UseShellExecute = false;
			info.RedirectStandardError = true;
			info.RedirectStandardInput = true;
			info.RedirectStandardOutput = true;
			info.CreateNoWindow = true;
			openSSLProc.Start();

			openSSLBuffer = new StringBuilder();
			openSSLErrBuffer = new StringBuilder();
			var threadSync = openSSLThreadSync;
			openSSLThread1 = new Thread(() => {
				try {
					while (openSSLThreadSync == threadSync) {
						var line = openSSLProc.StandardOutput.ReadLine();
						if (line != null) {
							openSSLBuffer.Append(line).Append('\n');
						}
					}
				} catch { }
			});
			openSSLThread2 = new Thread(() => {
				try {
					while (openSSLThreadSync == threadSync) {
						var line = openSSLProc.StandardError.ReadLine();
						if (line != null) {
							openSSLErrBuffer.Append(line).Append('\n');
						}
					}
				} catch { }
			});
			openSSLThread1.Start();
			openSSLThread2.Start();

			WriteFile("test_openssl_key.pem", Encoding.UTF8.GetBytes(rsa.ToPEM(false).ToPEM_PKCS8(false)));
			WriteFile("test_openssl_data.txt", data);

			byte[] no = new byte[rsa.KeySize / 8];
			Array.Copy(data, 0, no, no.Length - data.Length, data.Length);
			WriteFile("test_openssl_data.txt.nopadding.txt", no);

			openSSLProc.StandardInput.Write("openssl version\necho " + openSSLBoundary + "\n");
			openSSLProc.StandardInput.Flush();
			while (true) {
				if (openSSLBuffer.ToString().IndexOf(openSSLBoundary) != -1) {
					if (openSSLErrBuffer.Length > 0) {
						closeOpenSSL();
						throw new Exception(T("打开OpenSSL出错：", "Error opening OpenSSL: ") + openSSLErrBuffer.ToString().Trim());
					}
					S("OpenSSL Version: " + openSSLBuffer.ToString().Trim());
					break;
				}
				Thread.Sleep(10);
			}
		}
		static private Process openSSLProc;
		static private StringBuilder openSSLBuffer, openSSLErrBuffer;
		static private Thread openSSLThread1, openSSLThread2;
		static private int openSSLThreadSync;
		static private readonly string openSSLBoundary = "--openSSL boundary--";
		static void closeOpenSSL() {
			openSSLThreadSync++;
			if (openSSLProc == null) return;
			try {
				openSSLProc.Kill();
				openSSLProc.Dispose();
			} catch { }
			openSSLProc = null;
		}
		static byte[] testOpenSSL(bool encOrSign, string mode) {
			bool debug = false; string cmd = "";
			string keyFile = "test_openssl_key.pem", txtFile = "test_openssl_data.txt";
			string save = txtFile + (encOrSign ? ".enc.bin" : ".sign.bin");
			if (encOrSign) {//加密
				if (mode == "NO") {
					cmd = "openssl pkeyutl -encrypt -pkeyopt rsa_padding_mode:none -in " + txtFile + ".nopadding.txt -inkey " + keyFile + " -out " + save;
				} else if (mode == "PKCS1") {
					cmd = "openssl pkeyutl -encrypt -pkeyopt rsa_padding_mode:pkcs1 -in " + txtFile + " -inkey " + keyFile + " -out " + save;
				} else if (mode.StartsWith("OAEP+")) {
					string hash = mode.Replace("OAEP+", "").Replace("-512/", "512-");
					cmd = "openssl pkeyutl -encrypt -pkeyopt rsa_padding_mode:oaep -pkeyopt rsa_oaep_md:" + hash + " -in " + txtFile + " -inkey " + keyFile + " -out " + save;
				}
			} else {//签名
				if (mode.StartsWith("PKCS1+")) {
					string hash = mode.Replace("PKCS1+", "").Replace("-512/", "512-");
					cmd = "openssl dgst -" + hash + " -binary -sign " + keyFile + " -out " + save + " " + txtFile;
				} else if (mode.StartsWith("PSS+")) {
					string hash = mode.Replace("PSS+", "").Replace("-512/", "512-");
					cmd = "openssl dgst -" + hash + " -binary -out " + txtFile + ".hash " + txtFile;
					cmd += "\n";
					cmd += "openssl pkeyutl -sign -pkeyopt digest:" + hash + " -pkeyopt rsa_padding_mode:pss -pkeyopt rsa_pss_saltlen:-1 -in " + txtFile + ".hash -inkey " + keyFile + " -out " + save;
				}
			}
			if (cmd.Length == 0) {
				string msg = T("无效mode：", "Invalid mode: ") + mode;
				S("[OpenSSL Code Error] " + msg);
				throw new Exception(msg);
			}
			if (File.Exists(save)) {
				File.Delete(save);
			}

			if (debug) S("[OpenSSL Cmd][" + mode + "]" + cmd);
			openSSLBuffer.Length = 0;
			openSSLErrBuffer.Length = 0;
			openSSLProc.StandardInput.Write(cmd + "\n");
			openSSLProc.StandardInput.Write("echo " + openSSLBoundary + "\n");
			openSSLProc.StandardInput.Flush();

			while (true) {
				if (openSSLBuffer.ToString().IndexOf(openSSLBoundary) != -1) {
					if (openSSLErrBuffer.Length > 0) {
						if (debug) S("[OpenSSL Error]\n" + openSSLErrBuffer + "\n[End]");
						throw new Exception("OpenSSL Error: " + openSSLErrBuffer.ToString().Trim());
					}
					if (debug) S("[OpenSSL Output]\n" + openSSLBuffer + "\n[End] save:" + Path.GetFullPath(save));
					break;
				}
				Thread.Sleep(10);
			}
			return ReadFile(save);
		}

		static void showOpenSSLTips() {
			ST("===== OpenSSL中RSA相关的命令行调用命令 ====="
		 , "===== RSA-related command-line invocation commands in OpenSSL =====");
			S();
			ST("::先准备一个测试文件 test.txt 里面填少量内容，openssl不支持自动分段加密"
			 , "::First prepare a test file test.txt and fill in a small amount of content, openssl does not support automatic segmentation encryption");
			S();
			ST("::生成新密钥", "::Generate new key");
			S("openssl genrsa -out private.pem 1024");
			S();
			ST("::提取公钥PKCS#8", "::Extract public key PKCS#8");
			S("openssl rsa -in private.pem -pubout -out public.pem");
			S();
			ST("::转换成RSAPublicKey PKCS#1", "::Convert to RSAPublicKey PKCS#1");
			S("openssl rsa -pubin -in public.pem -RSAPublicKey_out -out public.pem.rsakey");
			ST("::测试RSAPublicKey PKCS#1，不出意外会出错。因为这个公钥里面没有OID，通过RSA_PEM转换成PKCS#8自动带上OID就能正常加密"
			 , "::Test RSAPublicKey PKCS#1, no accident will go wrong. Because there is no OID in this public key, it can be encrypted normally by converting RSA_PEM into PKCS#8 and automatically bringing OID");
			S("echo abcd123 | openssl rsautl -encrypt -inkey public.pem.rsakey -pubin");
			S();
			S();
			S();
			ST("::加密和解密，填充方式：PKCS1"
			 , "::Encryption and decryption, padding mode: PKCS1");
			S("openssl pkeyutl -encrypt -pkeyopt rsa_padding_mode:pkcs1 -in test.txt -pubin -inkey public.pem -out test.txt.enc.bin");
			S("openssl pkeyutl -decrypt -pkeyopt rsa_padding_mode:pkcs1 -in test.txt.enc.bin -inkey private.pem -out test.txt.dec.txt");
			S();
			ST("::加密和解密，填充方式：OAEP+SHA256，掩码生成函数MGF1使用相同的hash算法"
			 , "::Encryption and decryption, padding mode: OAEP+SHA256, mask generation function MGF1 uses the same hash algorithm");
			S("openssl pkeyutl -encrypt -pkeyopt rsa_padding_mode:oaep -pkeyopt rsa_oaep_md:sha256 -in test.txt -pubin -inkey public.pem -out test.txt.enc.bin");
			S("openssl pkeyutl -decrypt -pkeyopt rsa_padding_mode:oaep -pkeyopt rsa_oaep_md:sha256 -in test.txt.enc.bin -inkey private.pem -out test.txt.dec.txt");
			S();
			S();
			ST("::命令行参数中的sha256可以换成md5、sha1等；如需sha3系列，就换成sha3-256即可"
			 , "::The sha256 in the command line parameters can be replaced by md5, sha1, etc.; if you need the sha3 series, you can replace it with sha3-256");
			S();
			S();
			ST("::签名和验证，填充方式：PKCS1+SHA256", "::Signature and verification, padding mode: PKCS1+SHA256");
			S("openssl dgst -sha256 -binary -sign private.pem -out test.txt.sign.bin test.txt");
			S("openssl dgst -sha256 -binary -verify public.pem -signature test.txt.sign.bin test.txt");
			S();
			ST("::签名和验证，填充方式：PSS+SHA256 ，salt=-1使用hash长度=256/8，掩码生成函数MGF1使用相同的hash算法"
			, "::Signature and verification, padding mode: PSS+SHA256, salt=-1 use hash length=256/8, mask generation function MGF1 uses the same hash algorithm");
			S("openssl dgst -sha256 -binary -out test.txt.hash test.txt");
			S("openssl pkeyutl -sign -pkeyopt digest:sha256 -pkeyopt rsa_padding_mode:pss -pkeyopt rsa_pss_saltlen:-1 -in test.txt.hash -inkey private.pem -out test.txt.sign.bin");
			S("openssl pkeyutl -verify -pkeyopt digest:sha256 -pkeyopt rsa_padding_mode:pss -pkeyopt rsa_pss_saltlen:-1 -in test.txt.hash -pubin -inkey public.pem -sigfile test.txt.sign.bin");
			S();
			S();
		}




		static bool waitAnyKey = true;
		static void ShowMenu(string[] args) {
			if (args != null && args.Length > 0) {
				foreach (var v in args) {
					if (v.StartsWith("-zh=")) {
						RSA_PEM.Lang = v.StartsWith("-zh=1") ? "zh" : "en";
					}
				}
				S(args.Length + T("个启动参数：", " startup parameters: ") + string.Join(" ", args));
				S();
			}

			bool newRun = true;
			while (true) {
				if (newRun) {
					newRun = false;
					S("======  https://github.com/xiangyuecn/RSA-csharp  ======");
					printEnv();
					S(HR);
				}

				var isSet = loadKeyFile.Length > 0 && loadSrcFile.Length > 0;
				var setTips = isSet ? "" : "        " + T("[不可用]请先设置4、5", "[Unavailable] Please set 4, 5 first") + "  ";
				var floadTips = T("[已加载，修改后需重新加载]", "[loaded, need to reload after modification]");
				var fileName = loadSrcFile.Length > 0 ? Path.GetFileName(loadSrcFile) : "test.txt";

				S(T("【功能菜单】", "[ Menu ]") + "    .NET Version: " + NET_Ver());
				S("1. " + T("测试：运行基础功能测试（1次）", "Test: Run basic functional tests (1 time)"));
				S("2. " + T("测试：运行基础功能测试（1000次）", "Test: Run basic functional tests (1000 times)"));
				S("3. " + T("测试：多线程并发调用同一个RSA", "Test: Multiple threads call the same RSA concurrently"));
				S(HR);
				S("4. " + T("设置：加载密钥PEM文件", "Setup: Load key PEM file") + (loadKeyFile.Length > 0 ? "  " + floadTips + Path.GetFileName(loadKeyFile) + " " + loadKey.KeySize + " bits" : ""));
				S("5. " + T("设置：加载目标源文件", "Setup: Load Target Source File") + (loadSrcFile.Length > 0 ? "   " + floadTips + fileName + " " + loadSrcBytes.Length + " Bytes" : ""));
				S("6. " + T("加密    ", "Encrypt") + setTips + "  " + fileName + " -> " + fileName + ".enc.bin");
				S("7. " + T("解密对比", "Decrypt") + setTips + "  " + fileName + ".enc.bin -> " + fileName + ".dec.txt");
				S("8. " + T("签名    ", "Sign   ") + setTips + "  " + fileName + " -> " + fileName + ".sign.bin");
				S("9. " + T("验证签名", "Verify ") + setTips + "  " + fileName + ".sign.bin");
				S(HR);
				S("A. " + T("RSA密钥工具：生成密钥、转换密钥格式", "RSA key tool: generate key, convert key format"));
				S("B. " + T("显示当前环境支持的加密和签名填充模式，输入 B2 可同时对比OpenSSL结果", "Display the encryption and signature padding modes supported by the current environment, enter B2 to compare OpenSSL results at the same time")
					+ "   (" + (CanLoad_BouncyCastle() ? (BcAssembly == null ?
						T("可注册BouncyCastle加密增强库", "Can register BouncyCastle encryption enhancement library")
						: T("已注册BouncyCastle加密增强库", "BouncyCastle encryption enhancement library registered")
					) : T("未检测到BouncyCastle加密增强库", "BouncyCastle encryption enhancement library was not detected")) + ")");
				S("C. " + T("显示OpenSSL中RSA相关的命令行调用命令", "Display RSA-related command line calls in OpenSSL"));
				S("*. " + T("输入 exit 退出，输入 lang=zh|en 切换显示语言", "Enter exit to exit, enter lang=zh|en to switch display language")
					+ (RSA_Util.IS_CORE ? "" : T("，输入 net45 或 net46 切换高低版本Framework兼容模式", ", enter net45 or net46 to switch between high and low version Framework compatibility mode")
					+ " (" + T("当前为：", "Currently: ") + (RSA_Util.IS_CoreOr46 ? "net46" : "net45") + ")"));
				S();
				ST("请输入菜单序号：", "Please enter the menu number:");
				Console.Write("> ");

				waitAnyKey = true;
				while (true) {
					var inTxt = ReadIn().Trim().ToUpper();

					try {
						if (inTxt == "1") {
							RSATest(false);
						} else if (inTxt == "2") {
							for (int i = 0; i < 1000; i++) { ST("第" + i + "次>>>>>", i + "th time>>>>>"); RSATest(true); }
						} else if (inTxt == "3") {
							waitAnyKey = false;
							threadRun();
						} else if (inTxt == "4") {
							waitAnyKey = false;
							setLoadKey();
						} else if (inTxt == "5") {
							waitAnyKey = false;
							setLoadSrcBytes();
						} else if (isSet && inTxt == "6") {
							bool next = setEncType();
							if (next) {
								execEnc();
							}
						} else if (isSet && inTxt == "7") {
							bool next = setEncType();
							if (next) {
								execDec();
							}
						} else if (isSet && inTxt == "8") {
							bool next = setSignType();
							if (next) {
								execSign();
							}
						} else if (isSet && inTxt == "9") {
							bool next = setSignType();
							if (next) {
								execVerify();
							}
						} else if (inTxt == "A") {
							waitAnyKey = false;
							keyTools();
						} else if (inTxt == "B" || inTxt == "B2") {
							testProvider(inTxt == "B2");
						} else if (inTxt == "C") {
							showOpenSSLTips();
						} else if (inTxt.StartsWith("LANG=")) {
							waitAnyKey = false; newRun = true;
							if (inTxt == "LANG=ZH") {
								RSA_PEM.Lang = "zh";
								S("已切换语言成简体中文");
							} else if (inTxt == "LANG=EN") {
								RSA_PEM.Lang = "en";
								S("Switched language to English-US");
							} else {
								waitAnyKey = true; newRun = false;
								ST("语言设置命令无效！", "Invalid language setting command!");
							}
						} else if (inTxt == "NET45" || inTxt == "NET46") {
							if (RSA_Util.IS_CORE) {
								ST(".NET Core下无需进行此配置", "This configuration is not required under .NET Core");
							} else if (inTxt == "NET45") {
								RSA_Util.IS_CoreOr46_Test_Set(-1);
								ST("已配置使用.NET Framework 4.5及以下版本模式进行测试", "Configured to use .NET Framework 4.5 and below version mode for testing");
							} else {
								RSA_Util.IS_CoreOr46_Test_Set(1);
								ST("已配置使用.NET Framework 4.6及以上版本模式进行测试", "Configured to use .NET Framework 4.6 and above version mode for testing");
							}
						} else if (inTxt == "EXIT") {
							S("bye!");
							return;
						} else {
							inTxt = "";
							ST("序号无效，请重新输入菜单序号！", "The menu number is invalid, please re-enter the menu number!");
							Console.Write("> ");
							continue;
						}
					} catch (Exception e) {
						S(e.ToString());
						Thread.Sleep(100);
						waitAnyKey = true;
					}
					break;
				}

				if (waitAnyKey) {
					ST("按任意键继续...", "Press any key to continue...");
					Console.ReadKey();
				}
				S();
			}
		}


	}
}
