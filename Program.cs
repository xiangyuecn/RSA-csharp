using System;

namespace com.github.xiangyuecn.rsacsharp {
	/// <summary>
	/// RSA、RSA_PEM测试控制台主程序，.NET Core、.NET Framework均可测试
	/// GitHub: https://github.com/xiangyuecn/RSA-csharp
	/// </summary>
	class Program {
		static void RSATest(bool fast) {
			//新生成一个RSA密钥，也可以通过已有的pem、xml文本密钥来创建RSA
			var rsa = new RSA_Util(512);
			// var rsa = new RSA_Util("pem或xml文本密钥");
			// var rsa = new RSA_Util(RSA_PEM.FromPEM("pem文本密钥"));
			// var rsa = new RSA_Util(RSA_PEM.FromXML("xml文本密钥"));

			if (!checkPlatform(rsa)) return;

			//提取密钥pem字符串
			string pem_pkcs1 = rsa.ToPEM().ToPEM_PKCS1();
			string pem_pkcs8 = rsa.ToPEM().ToPEM_PKCS8();
			//提取密钥xml字符串
			string xml = rsa.ToXML();

			AssertMsg("【" + rsa.KeySize + "私钥（XML）】：", rsa.KeySize == 512);
			Console.WriteLine(xml);
			Console.WriteLine();
			Console.WriteLine("【" + rsa.KeySize + "私钥（PKCS#1）】：");
			Console.WriteLine(pem_pkcs1);
			Console.WriteLine();
			Console.WriteLine("【" + rsa.KeySize + "公钥（PKCS#8）】：");
			Console.WriteLine(rsa.ToPEM().ToPEM_PKCS8(true));
			Console.WriteLine();

			var str = "abc内容123";
			var en = rsa.Encode(str);
			Console.WriteLine("【加密】：");
			Console.WriteLine(en);

			Console.WriteLine("【解密】：");
			var de = rsa.DecodeOrNull(en);
			AssertMsg(de, de == str);

			if (!fast) {
				var str2 = str; for (var i = 0; i < 15; i++) str2 += str2;
				Console.WriteLine("【长文本加密解密】：");
				AssertMsg(str2.Length + "个字 OK", rsa.DecodeOrNull(rsa.Encode(str2)) == str2);
			}

			Console.WriteLine("【签名SHA1】：");
			var sign = rsa.Sign("SHA1", str);
			Console.WriteLine(sign);
			AssertMsg("校验 OK", rsa.Verify("SHA1", sign, str));
			Console.WriteLine();

			//用pem文本创建RSA
			var rsa2 = new RSA_Util(RSA_PEM.FromPEM(pem_pkcs8));
			Console.WriteLine("【用PEM新创建的RSA是否和上面的一致】：");
			Assert("XML：", rsa2.ToXML() == rsa.ToXML());
			Assert("PKCS1：", rsa2.ToPEM().ToPEM_PKCS1() == rsa.ToPEM().ToPEM_PKCS1());
			Assert("PKCS8：", rsa2.ToPEM().ToPEM_PKCS8() == rsa.ToPEM().ToPEM_PKCS8());

			//用xml文本创建RSA
			var rsa3 = new RSA_Util(RSA_PEM.FromXML(xml));
			Console.WriteLine("【用XML新创建的RSA是否和上面的一致】：");
			Assert("XML：", rsa3.ToXML() == rsa.ToXML());
			Assert("PKCS1：", rsa3.ToPEM().ToPEM_PKCS1() == rsa.ToPEM().ToPEM_PKCS1());
			Assert("PKCS8：", rsa3.ToPEM().ToPEM_PKCS8() == rsa.ToPEM().ToPEM_PKCS8());

			//--------RSA_PEM私钥验证---------
			{
				RSA_PEM pem = rsa.ToPEM();
				Console.WriteLine("【RSA_PEM是否和原始RSA一致】：");
				Console.WriteLine(pem.KeySize + "位");
				Assert("XML：", pem.ToXML(false) == rsa.ToXML());
				Assert("PKCS1：", pem.ToPEM_PKCS1() == rsa.ToPEM().ToPEM_PKCS1());
				Assert("PKCS8：", pem.ToPEM_PKCS8() == rsa.ToPEM().ToPEM_PKCS8());
				Console.WriteLine("仅公钥：");
				Assert("XML：", pem.ToXML(true) == rsa.ToXML(true));
				Assert("PKCS1：", pem.ToPEM_PKCS1(true) == rsa.ToPEM().ToPEM_PKCS1(true));
				Assert("PKCS8：", pem.ToPEM_PKCS8(true) == rsa.ToPEM().ToPEM_PKCS8(true));
			}
			//--------RSA_PEM公钥验证---------
			{
				var rsaPublic = new RSA_Util(rsa.ToPEM(true));
				RSA_PEM pem = rsaPublic.ToPEM();
				Console.WriteLine("【RSA_PEM仅公钥是否和原始RSA一致】：");
				Console.WriteLine(pem.KeySize + "位");
				Assert("XML：", pem.ToXML(false) == rsa.ToXML(true));
				Assert("PKCS1：", pem.ToPEM_PKCS1() == rsa.ToPEM().ToPEM_PKCS1(true));
				Assert("PKCS8：", pem.ToPEM_PKCS8() == rsa.ToPEM().ToPEM_PKCS8(true));
			}

			if (!fast) {
				RSA_PEM pem = rsa.ToPEM();
				var rsa4 = new RSA_Util(new RSA_PEM(pem.Key_Modulus, pem.Key_Exponent, pem.Key_D));
				Console.WriteLine("【用n、e、d构造解密】");
				de = rsa4.DecodeOrNull(en);
				AssertMsg(de, de == str);
			}




			Console.WriteLine();
			Console.WriteLine();
			Console.WriteLine("【" + rsa.KeySize + "私钥（PKCS#8）】：");
			Console.WriteLine(rsa.ToPEM().ToPEM_PKCS8());
			Console.WriteLine();
			Console.WriteLine("【" + rsa.KeySize + "公钥（PKCS#1）】：不常见的公钥格式");
			Console.WriteLine(rsa.ToPEM().ToPEM_PKCS1(true));
		}


		static void Assert(string msg, bool check) {
			AssertMsg(msg + check, check);
		}
		static void AssertMsg(string msg, bool check) {
			if (!check) throw new Exception(msg);
			Console.WriteLine(msg);
		}

		static bool checkPlatform(RSA_Util rsa) {
			Console.WriteLine(hr);
			Console.WriteLine(ht + " "
				+ (RSA_Util.UseCore == RSA_Util.IS_CORE ? "【默认RSA实现】" : "【强制切换RSA实现类】")
				+ "当前RSA实现类：" + rsa.RSAObject.GetType().Name
				+ " " + ht);
			Console.WriteLine(hr);
			if (RSA_Util.UseCore == RSA_Util.IS_CORE) {
				return true;
			}


			var isWindows = true;
#if (NETCOREAPP || NETSTANDARD || NET) //https://docs.microsoft.com/zh-cn/dotnet/csharp/language-reference/preprocessor-directives
			if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)) {
				isWindows = false;
			}
#endif

			//强制切换了RSA实现类，检查一下是否支持
			if (!rsa.RSAIsUseCore) {// RSACryptoServiceProvider实现类
				if (!isWindows) {
					Console.WriteLine("强制切换了RSA实现类，当前使用的RSACryptoServiceProvider不支持跨平台，只支持在Windows系统中使用。");
					Console.WriteLine("非Windows系统，不测试！");
					return false;
				}
			}
			return true;
		}

		static readonly string ht = "◆◆◆◆◆◆◆◆◆◆◆◆";
		static readonly string hr = "---------------------------------------------------------";
		static void Main(string[] _) {
			long startTime = DateTime.Now.Ticks;

			// for (var i = 0; i < 1000; i++) { Console.WriteLine("第" + i + "次>>>>>"); RSA_Util.UseCore = !RSA_Util.IS_CORE; RSATest(true); RSA_Util.UseCore = RSA_Util.IS_CORE; RSATest(true); }

			RSATest(false);

			Console.WriteLine(hr);
			Console.WriteLine();

			// 强制切换一下RSA实现类进行测试
			RSA_Util.UseCore = !RSA_Util.IS_CORE;
			RSATest(false);
			Console.WriteLine();

			Console.WriteLine(hr);
			Console.WriteLine(ht + " 耗时：" + (DateTime.Now.Ticks - startTime) / 10000 + "ms 回车退出... " + ht);
			Console.WriteLine(hr);
			Console.WriteLine();
			Console.ReadLine();
		}
	}
}
