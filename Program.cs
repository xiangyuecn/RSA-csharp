using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RSA {
	class Program {
		static void RSATest() {
			var rsa = new RSA(512);
			Console.WriteLine("【512密钥（XML）】：");
			Console.WriteLine(rsa.ToXML());
			Console.WriteLine();
			Console.WriteLine("【512密钥（PEM）】：");
			Console.WriteLine(rsa.ToPEM_PKCS1());
			Console.WriteLine();

			var en = rsa.Encode("abc内容123");
			Console.WriteLine("【加密】：");
			Console.WriteLine(en);

			Console.WriteLine("【解密】：");
			Console.WriteLine(rsa.DecodeOrNull(en));
			Console.WriteLine();

			var rsa2 = new RSA(rsa.ToPEM_PKCS8(), true);
			Console.WriteLine("【用PEM新创建的RSA是否和上面的一致】：");
			Console.WriteLine("XML：" + (rsa2.ToXML() == rsa.ToXML()));
			Console.WriteLine("PKCS1：" + (rsa2.ToPEM_PKCS1() == rsa.ToPEM_PKCS1()));
			Console.WriteLine("PKCS8：" + (rsa2.ToPEM_PKCS8() == rsa.ToPEM_PKCS8()));

			var rsa3 = new RSA(rsa.ToXML());
			Console.WriteLine("【用XML新创建的RSA是否和上面的一致】：");
			Console.WriteLine("XML：" + (rsa3.ToXML() == rsa.ToXML()));
			Console.WriteLine("PKCS1：" + (rsa3.ToPEM_PKCS1() == rsa.ToPEM_PKCS1()));
			Console.WriteLine("PKCS8：" + (rsa3.ToPEM_PKCS8() == rsa.ToPEM_PKCS8()));
		}




		static void Main(string[] args) {
			Console.WriteLine("---------------------------------------------------------");
			Console.WriteLine("◆◆◆◆◆◆◆◆◆◆◆◆ RSA测试 ◆◆◆◆◆◆◆◆◆◆◆◆");
			Console.WriteLine("---------------------------------------------------------");

			RSATest();

			Console.WriteLine("-------------------------------------------------------------");
			Console.WriteLine("◆◆◆◆◆◆◆◆◆◆◆◆ 回车退出... ◆◆◆◆◆◆◆◆◆◆◆◆");
			Console.WriteLine();
			Console.ReadLine();
		}
	}
}
