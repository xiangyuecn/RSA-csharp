# :open_book:RSA-csharp的帮助文档

本项目核心功能为：支持`.NET Core`、`.NET Framework`环境下`PEM`（`PKCS#1`、`PKCS#8`）格式RSA密钥对导入、导出。

附带实现了一个RSA封装操作类，和一个测试控制台程序。

你可以只copy `RSA_PEM.cs` 文件到你的项目中使用，只需这一个文件你就拥有了通过PEM格式密钥创建`RSACryptoServiceProvider`的能力。clone整个项目代码用vs应该能够直接打开，经目测看起来没什么卵用的文件都svn:ignore掉了（svn滑稽。

【Java版】：[RSA-java](https://github.com/xiangyuecn/RSA-java)


## 提供支持

- 通过`XML格式`密钥对创建RSA
- 通过`PEM格式`密钥对创建RSA
- RSA加密、解密
- RSA签名、验证
- 导出`XML格式`公钥、私钥
- 导出`PEM格式`公钥、私钥
- `PEM格式`秘钥对和`XML格式`秘钥对互转




# :open_book:文档

## 【RSA_PEM.cs】
此文件不依赖任何文件，可以直接copy这个文件到你项目中用；通过`FromPEM`、`ToPEM` 和`FromXML`、`ToXML`这两对方法，可以实现PEM`PKCS#1`、`PKCS#8`相互转换，PEM、XML的相互转换。

项目里面需要引入程序集`System.Numerics`用来支持`BigInteger`，vs默认创建的项目是不会自动引入此程序集的，要手动引入。

注：openssl `RSAPublicKey_out`导出的公钥，字节码内并不带[OID](http://www.oid-info.com/get/1.2.840.113549.1.1.1)（目测是因为不带OID所以openssl自己都不支持用这个公钥来加密数据），RSA_PEM支持此格式公钥的导入，但不提供此种格式公钥的导出。

### 实例属性

byte[]：**Key_Modulus**(模数n，公钥、私钥都有)、**Key_Exponent**(公钥指数e，公钥、私钥都有)、**Key_D**(私钥指数d，只有私钥的时候才有)；有这3个足够用来加密解密。

byte[]：**Val_P**(prime1)、**Val_Q**(prime2)、**Val_DP**(exponent1)、**Val_DQ**(exponent2)、**Val_InverseQ**(coefficient)； (PEM中的私钥才有的更多的数值；可通过n、e、d反推出这些值（只是反推出有效值，和原始的值大概率不同）)。

int：**KeySize**(密钥位数)

bool：**HasPrivate**(是否包含私钥)

### 构造方法

**RSA_PEM(RSACryptoServiceProvider rsa, bool convertToPublic = false)**：通过RSA中的公钥和私钥构造一个PEM，如果convertToPublic含私钥的RSA将只读取公钥，仅含公钥的RSA不受影响。

**RSA_PEM(byte[] modulus, byte[] exponent, byte[] d, byte[] p, byte[] q, byte[] dp, byte[] dq, byte[] inverseQ)**：通过全量的PEM字段数据构造一个PEM，除了模数modulus和公钥指数exponent必须提供外，其他私钥指数信息要么全部提供，要么全部不提供（导出的PEM就只包含公钥）注意：所有参数首字节如果是0，必须先去掉。

**RSA_PEM(byte[] modulus, byte[] exponent, byte[] dOrNull)**：通过公钥指数和私钥指数构造一个PEM，会反推计算出P、Q但和原始生成密钥的P、Q极小可能相同。注意：所有参数首字节如果是0，必须先去掉。出错将会抛出异常。私钥指数可以不提供，导出的PEM就只包含公钥。


### 实例方法

**RSACryptoServiceProvider GetRSA()**：将PEM中的公钥私钥转成RSA对象，如果未提供私钥，RSA中就只包含公钥。

**string ToPEM(bool convertToPublic, bool usePKCS8)**：将RSA中的密钥对转换成PEM格式，usePKCS8=false时返回PKCS#1格式，否则返回PKCS#8格式，如果convertToPublic含私钥的RSA将只返回公钥，仅含公钥的RSA不受影响。

**string ToXML(bool convertToPublic)**：将RSA中的密钥对转换成XML格式，如果convertToPublic含私钥的RSA将只返回公钥，仅含公钥的RSA不受影响。


### 静态方法

**static RSA_PEM FromPEM(string pem)**：用PEM格式密钥对创建RSA，支持PKCS#1、PKCS#8格式的PEM，出错将会抛出异常。pem格式如：`-----BEGIN XXX KEY-----....-----END XXX KEY-----`。

**static RSA_PEM FromXML(string xml)**：将XML格式密钥转成PEM，支持公钥xml、私钥xml，出错将会抛出异常。




## 【RSA.cs】
此文件依赖`RSA_PEM.cs`，封装了加密、解密、签名、验证、秘钥导入导出操作。

### 构造方法

**RSA(int keySize)**：用指定密钥大小创建一个新的RSA，会生成新密钥，出错抛异常。

**RSA(string xml)**：通过XML格式密钥，创建一个RSA，xml内可以只包含一个公钥或私钥，或都包含，出错抛异常。`XML格式`如：`<RSAKeyValue><Modulus>...</RSAKeyValue>`

**RSA(string pem, bool noop)**：通过`PEM格式`密钥对创建RSA（noop参数随意填），PEM可以是公钥或私钥，支持`PKCS#1`、`PKCS#8`格式，pem格式如：`-----BEGIN XXX KEY-----....-----END XXX KEY-----`。

**RSA(RSA_PEM pem)**：通过一个pem对象创建RSA，pem为公钥或私钥，出错抛异常。


### 实例属性

RSACryptoServiceProvider：**RSAObject**(最底层的RSACryptoServiceProvider对象)

int：**KeySize**(密钥位数)

bool：**HasPrivate**(是否包含私钥)


### 实例方法

**string ToXML(bool convertToPublic = false)**：导出`XML格式`秘钥对。如果RSA包含私钥，默认会导出私钥，设置仅仅导出公钥时只会导出公钥；不包含私钥只会导出公钥。

**string ToPEM_PKCS1(bool convertToPublic = false)**：导出`PEM PKCS#1格式`秘钥对。如果RSA包含私钥，默认会导出私钥，设置仅仅导出公钥时只会导出公钥；不包含私钥只会导出公钥。

**string ToPEM_PKCS8(bool convertToPublic = false)**：导出`PEM PKCS#8格式`秘钥对。如果RSA包含私钥，默认会导出私钥，设置仅仅导出公钥时只会导出公钥；不包含私钥只会导出公钥。

**RSA_PEM ToPEM(bool convertToPublic = false)**：导出RSA_PEM对象，如果convertToPublic含私钥的RSA将只返回公钥，仅含公钥的RSA不受影响。

**string Encode(string str)**：加密操作，支持任意长度数据。

**byte[] Encode(byte[] data)**：加密数据，支持任意长度数据，出错抛异常。

**string DecodeOrNull(string str)**：解密字符串（utf-8），解密异常返回null。

**byte[] DecodeOrNull(byte[] data)**：解密数据，解密异常返回null。

**string Sign(string hash, string str)**：对str进行签名，并指定hash算法（如：SHA256）。

**byte[] Sign(string hash, byte[] data)**：对data进行签名，并指定hash算法（如：SHA256）。

**bool Verify(string hash, string sgin, string str)**：验证字符串str的签名是否是sgin，并指定hash算法（如：SHA256）。

**bool Verify(string hash, byte[] sgin, byte[] data)**：验证data的签名是否是sgin，并指定hash算法（如：SHA256）。






# :open_book:图例

控制台运行：

![控制台运行](images/1.png)

RSA工具（非开源）：

![RSA工具](images/2.png)





# :open_book:知识库

在写一个小转换工具时加入了RSA加密解密支持（见图RSA工具），秘钥输入框支持填写XML和PEM格式，操作类型里面支持XML->PEM、PEM->XML的转换。

实现相应功能发现原有RSA操作类不能良好工作，PEM->XML没问题，只要能通过PEM创建RSA，就能用`RSACryptoServiceProvider`自带方法导出XML。但XML->PEM没有找到相应的简单实现方法，大部分博客写的用BouncyCastle库来操作，代码是少，但BouncyCastle就有好几兆大小，我的小工具啊才100K；所以自己实现了一个支持导出`PKCS#1`、`PKCS#8`格式PEM密钥的方法`RSA_PEM.ToPEM`。

操作过程中发现原有RSA操作类不支持用`PKCS#8`格式PEM密钥来创建RSA对象（用的[RSACryptoServiceProviderExtension](https://www.cnblogs.com/adylee/p/3611461.html)的扩展方法来支持PEM密钥），仅支持`PKCS#1`，所以又自己实现了一个从PEM密钥来创建`RSACryptoServiceProvider`的方法`RSA_PEM.FromPEM`。

在实现导入导出PEM密钥过程中，对`PKCS#1`、`PKCS#8`格式的PEM密钥有了一定的了解，主要参考了：

《[RSA公钥文件（PEM）解析](https://blog.csdn.net/xuanshao_/article/details/51679824)》：公钥字节码分解。

《[RSA私钥文件（PEM）解析](https://blog.csdn.net/xuanshao_/article/details/51672547)》：私钥字节码分解。

《[iOS安全相关 - RSA中公钥的DER格式组成](https://www.jianshu.com/p/25803dd9527d)》：1字节和2字节长度表述方法，和为什么有些字段前面要加0x00。





## PEM密钥编码格式

> 此结构分析为初学RSA秘钥格式时总结出来的，比较粗糙但够用了。后面研究了ASN.1结构，然后实现了个ASN.1标准的解析器，按照ASN.1语法去解析和生成RSA PEM秘钥代码会简单严谨许多（另外在此基础上实现了个CSR解析生成器，有了ASN.1标准解析器，做啥都简单了）。

### 长度表述方法
PEM格式中，每段数据基本上都是`type+长度数据占用位数+长度数值+数据`这种格式。

长度数据占用位数有0x81和0x82两个值，分别代表长度数值占用了1字节和2字节。

但长度数据占用位数不一定存在，如果长度数值<0x80时（理由应该和下面这个加0x00一致），长度数值直接在type后面用1位来表述，变成了`type+长度数值(<0x80)+数据`。


### 什么情况下内容前面要加0x00

#### type=02 INTEGER时
如果内容的bit流的前4 bit十六进制值>=8就要在内容前面加0x00，其他不用加。

> 一个大整数，最高位为符号位，其为1时，就是负数，所以要在最高位填充0x00以保证不为负。

#### type=03 BIT STRING时
内容前面要加0（可能现在全部是加0吧，数据结尾这个字节不满8位？什么情况下会出现不够1字节？不够就用二进制0补齐，然后内容前面加补了几位）。


### PEM公钥编码格式
`PKCS#1`、`PKCS#8`公钥编码都是统一的格式。

```
/*****1024位公钥*****/
-----BEGIN PUBLIC KEY-----
MIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKBgQCYw9+M3+REzDtYqwBrs/as/Oy8
GRE5OmnqOV0EfkEiCIjiczbVEFnZ3qRjLbDATfmBxNQ6c6Fga8nX28glEH/aL/RG
2KdpI8KMUqKAszNydsHSeh9MSKcd5zgS4NuE0u+eR7CB8kOWipiLDQmY38kpM36p
RWdNQlpIaTDo5IhJJwIDAQAB
-----END PUBLIC KEY-----

/*****二进制表述*****/
30819F300D06092A864886F70D010101050003818D003081890281810098C3DF8CDFE444CC3B58AB006BB3F6ACFCECBC1911393A69EA395D047E41220888E27336D51059D9DEA4632DB0C04DF981C4D43A73A1606BC9D7DBC825107FDA2FF446D8A76923C28C52A280B3337276C1D27A1F4C48A71DE73812E0DB84D2EF9E47B081F243968A988B0D0998DFC929337EA945674D425A486930E8E48849270203010001


/*****二进制分解*****/

/*
后续（到结尾）内容长度为0x9F字节，相当于整个文件长度-当前这3字节。
格式：type[+长度数据占用位数(可选)]+后续长度数值，
	此处type=0x30，
	长度数据占用1位（参考前面长度表述方法），
	后续长度数值=0x9F个字节

直观JSON化ASN.1(后面新学的ASN.1结构)：
30_SEQUENCE{
	03_BIT_STRING: "二进制内容（看是什么，也许是纯二进制，或者是一个可以解析成ASN.1格式的内容）"
	, 05_NULL: "NULL，后面内容长度为0"
	, 30_SEQUENCE:{
		02_INTEGER: "整数"
		, 02_INTEGER: "整数"
		, 02_INTEGER: "整数"
	}
}
*/
30 81 9F //容器[30 SEQUENCE] [81 长度数据占1位] [9F 容器内有9F字节数据]

	/*
	固定内容，长度为0x0D个字节，为RSA OID(1.2.840.113549.1.1.1)

	RSAES-PKCS1-v1_5 encryption scheme: http://www.oid-info.com/get/1.2.840.113549.1.1.1
	*/
	30 0D //容器[30 SEQUENCE] [0D 容器内有0D字节数据]
		06 09 2A864886F70D010101 //06 OID 1.2.840.113549.1.1.1
		05 00 //NULL

	/*后续内容长度，后面内容长度为0x8D字节，和开头格式一致*/
	03 81 8D //03 BIT STRING，后面放的是二进制公钥数据，此内容其实还是一个结构
		00 //BIT STRING内容前面需要加00

		/*后续内容长度，后面内容长度为0x89字节，和开头格式一致*/
		30 81 89 //容器[30 SEQUENCE] [81 长度数据占1位] [89 容器内有0x89字节数据]

			/*
			RSA Modulus内容
			格式：type[+长度数据占用位数(可选)]+内容长度数值+内容，
				此处type=0x02，所有RSA相关参数都是用02，
				长度数据占用位数为1字节，
				内容长度数值=0x81个字节，
				内容为0x00-0x27这一段（参考前面什么情况下要加0x00）。
			*/
			02 81 81 //02 INTEGER整数[02 INTEGER] [81 长度数据占1位] [81 Modulus内容长x81字节]
				0098C3DF8CDFE444CC3B58AB006BB3F6ACFCECBC1911393A69EA395D047E41220888E27336D51059D9DEA4632DB0C04DF981C4D43A73A1606BC9D7DBC825107FDA2FF446D8A76923C28C52A280B3337276C1D27A1F4C48A71DE73812E0DB84D2EF9E47B081F243968A988B0D0998DFC929337EA945674D425A486930E8E4884927

			/*RSA Exponent内容，和Modulus一样，但此处长度数据占用位数不存在*/
			02 03 //02 INTEGER整数[02 INTEGER] [03 Exponent内容长x03字节]
				010001
```


### PEM PKCS#1私钥编码格式

```
/*****1024位PKCS#1私钥*****/
-----BEGIN RSA PRIVATE KEY-----
MIICXAIBAAKBgQCYw9+M3+REzDtYqwBrs/as/Oy8GRE5OmnqOV0EfkEiCIjiczbV
EFnZ3qRjLbDATfmBxNQ6c6Fga8nX28glEH/aL/RG2KdpI8KMUqKAszNydsHSeh9M
SKcd5zgS4NuE0u+eR7CB8kOWipiLDQmY38kpM36pRWdNQlpIaTDo5IhJJwIDAQAB
AoGAcGNSWRrynia+1onf4lzg8v2U0QGEKV0vRNF0/HRCSN6MjkUDJxdDc0UYHZsk
uSXklTMQi/w70msacQNRqOsNk32O6vVPxr4NfTVaIV59Jv9Z5SXGiRmRZXeRw0ks
KYdOwaDJJu9zETNHZoMFJm8sq/tGJPQCPNesoZRZssL7mjkCQQDOI6jKt60bvu6V
XvtQoyUUbyMj9eCOBatS49jRvv326TMc951e9TcbnD0cxJrV1N6yIi+++ejwfagb
eYf++N61AkEAvbc8KTlBbI9TMwnVkQpst+ckgm3gpRDhAfQ/Lt7r8g2KAHsJv+wb
AJCgu8PgqM9mQjVxZ+78+aLEQ+h5rvMV6wJAY1c9/ct8ihV+Zs+qL1cgBHP2rFrO
x8KlqMGS+KmhPD9v2XLfDScBUrX9oYKB17DJTXE6Lz/CaTs1K2BrEI4gzQJBAJIQ
s9chaAfHSc1v8uha2F23Ltrk8iLknfi9LrBNneedGPVJxbXoeNm0gKxQIXaXSCoN
r6TP0iH5eZa3NIjIS8UCQAbw+d2WJIon+vuUsKk2dtZTqZx8e53NreZUFMaIkoS5
JPJqI6/6hq8/2ARFO3P9/qkxDMkJv8mSjV91cZixB10=
-----END RSA PRIVATE KEY-----

/*****二进制表述*****/
3082025C0201000281810098C3DF8CDFE444CC3B58AB006BB3F6ACFCECBC1911393A69EA395D047E41220888E27336D51059D9DEA4632DB0C04DF981C4D43A73A1606BC9D7DBC825107FDA2FF446D8A76923C28C52A280B3337276C1D27A1F4C48A71DE73812E0DB84D2EF9E47B081F243968A988B0D0998DFC929337EA945674D425A486930E8E48849270203010001028180706352591AF29E26BED689DFE25CE0F2FD94D10184295D2F44D174FC744248DE8C8E45032717437345181D9B24B925E49533108BFC3BD26B1A710351A8EB0D937D8EEAF54FC6BE0D7D355A215E7D26FF59E525C6891991657791C3492C29874EC1A0C926EF73113347668305266F2CABFB4624F4023CD7ACA19459B2C2FB9A39024100CE23A8CAB7AD1BBEEE955EFB50A325146F2323F5E08E05AB52E3D8D1BEFDF6E9331CF79D5EF5371B9C3D1CC49AD5D4DEB2222FBEF9E8F07DA81B7987FEF8DEB5024100BDB73C2939416C8F533309D5910A6CB7E724826DE0A510E101F43F2EDEEBF20D8A007B09BFEC1B0090A0BBC3E0A8CF6642357167EEFCF9A2C443E879AEF315EB024063573DFDCB7C8A157E66CFAA2F57200473F6AC5ACEC7C2A5A8C192F8A9A13C3F6FD972DF0D270152B5FDA18281D7B0C94D713A2F3FC2693B352B606B108E20CD0241009210B3D7216807C749CD6FF2E85AD85DB72EDAE4F222E49DF8BD2EB04D9DE79D18F549C5B5E878D9B480AC50217697482A0DAFA4CFD221F97996B73488C84BC5024006F0F9DD96248A27FAFB94B0A93676D653A99C7C7B9DCDADE65414C6889284B924F26A23AFFA86AF3FD804453B73FDFEA9310CC909BFC9928D5F757198B1075D

/*****二进制分解（大部分和公钥格式相同）*****/

/*后续内容长度，后面内容长度为0x025C个字节，和公钥开头格式一致，参考公钥部分*/
30 82 025C

	/*固定版本号*/
	02 01
		00

	/*#####从这里开始后面就是内容了 注：KCS#8仅仅是在此处插入部分内容#####*/

	/*RSA Modulus内容，和公钥开头格式一致，参考公钥部分*/
	02 81 81 
		0098C3DF8CDFE444CC3B58AB006BB3F6ACFCECBC1911393A69EA395D047E41220888E27336D51059D9DEA4632DB0C04DF981C4D43A73A1606BC9D7DBC825107FDA2FF446D8A76923C28C52A280B3337276C1D27A1F4C48A71DE73812E0DB84D2EF9E47B081F243968A988B0D0998DFC929337EA945674D425A486930E8E4884927

	/*RSA Exponent*/
	02 03
		010001

	/*RSA D*/
	02 81 80
		706352591AF29E26BED689DFE25CE0F2FD94D10184295D2F44D174FC744248DE8C8E45032717437345181D9B24B925E49533108BFC3BD26B1A710351A8EB0D937D8EEAF54FC6BE0D7D355A215E7D26FF59E525C6891991657791C3492C29874EC1A0C926EF73113347668305266F2CABFB4624F4023CD7ACA19459B2C2FB9A39

	/*RSA P*/
	02 41
		00CE23A8CAB7AD1BBEEE955EFB50A325146F2323F5E08E05AB52E3D8D1BEFDF6E9331CF79D5EF5371B9C3D1CC49AD5D4DEB2222FBEF9E8F07DA81B7987FEF8DEB5

	/*RSA Q*/
	02 41
		00BDB73C2939416C8F533309D5910A6CB7E724826DE0A510E101F43F2EDEEBF20D8A007B09BFEC1B0090A0BBC3E0A8CF6642357167EEFCF9A2C443E879AEF315EB

	/*RSA DP*/
	02 40
		63573DFDCB7C8A157E66CFAA2F57200473F6AC5ACEC7C2A5A8C192F8A9A13C3F6FD972DF0D270152B5FDA18281D7B0C94D713A2F3FC2693B352B606B108E20CD

	/*RSA DQ*/
	02 41
		009210B3D7216807C749CD6FF2E85AD85DB72EDAE4F222E49DF8BD2EB04D9DE79D18F549C5B5E878D9B480AC50217697482A0DAFA4CFD221F97996B73488C84BC5

	/*RSA InverseQ*/
	02 40
		06F0F9DD96248A27FAFB94B0A93676D653A99C7C7B9DCDADE65414C6889284B924F26A23AFFA86AF3FD804453B73FDFEA9310CC909BFC9928D5F757198B1075D
```

### PEM PKCS#8私钥编码格式
```
/*****1024位PKCS#8私钥*****/
-----BEGIN PRIVATE KEY-----
MIICdgIBADANBgkqhkiG9w0BAQEFAASCAmAwggJcAgEAAoGBAJjD34zf5ETMO1ir
AGuz9qz87LwZETk6aeo5XQR+QSIIiOJzNtUQWdnepGMtsMBN+YHE1DpzoWBrydfb
yCUQf9ov9EbYp2kjwoxSooCzM3J2wdJ6H0xIpx3nOBLg24TS755HsIHyQ5aKmIsN
CZjfySkzfqlFZ01CWkhpMOjkiEknAgMBAAECgYBwY1JZGvKeJr7Wid/iXODy/ZTR
AYQpXS9E0XT8dEJI3oyORQMnF0NzRRgdmyS5JeSVMxCL/DvSaxpxA1Go6w2TfY7q
9U/Gvg19NVohXn0m/1nlJcaJGZFld5HDSSwph07BoMkm73MRM0dmgwUmbyyr+0Yk
9AI816yhlFmywvuaOQJBAM4jqMq3rRu+7pVe+1CjJRRvIyP14I4Fq1Lj2NG+/fbp
Mxz3nV71NxucPRzEmtXU3rIiL7756PB9qBt5h/743rUCQQC9tzwpOUFsj1MzCdWR
Cmy35ySCbeClEOEB9D8u3uvyDYoAewm/7BsAkKC7w+Coz2ZCNXFn7vz5osRD6Hmu
8xXrAkBjVz39y3yKFX5mz6ovVyAEc/asWs7HwqWowZL4qaE8P2/Zct8NJwFStf2h
goHXsMlNcTovP8JpOzUrYGsQjiDNAkEAkhCz1yFoB8dJzW/y6FrYXbcu2uTyIuSd
+L0usE2d550Y9UnFteh42bSArFAhdpdIKg2vpM/SIfl5lrc0iMhLxQJABvD53ZYk
iif6+5SwqTZ21lOpnHx7nc2t5lQUxoiShLkk8mojr/qGrz/YBEU7c/3+qTEMyQm/
yZKNX3VxmLEHXQ==
-----END PRIVATE KEY-----

/*****二进制表述*****/
30820276020100300D06092A864886F70D0101010500048202603082025C0201000281810098C3DF8CDFE444CC3B58AB006BB3F6ACFCECBC1911393A69EA395D047E41220888E27336D51059D9DEA4632DB0C04DF981C4D43A73A1606BC9D7DBC825107FDA2FF446D8A76923C28C52A280B3337276C1D27A1F4C48A71DE73812E0DB84D2EF9E47B081F243968A988B0D0998DFC929337EA945674D425A486930E8E48849270203010001028180706352591AF29E26BED689DFE25CE0F2FD94D10184295D2F44D174FC744248DE8C8E45032717437345181D9B24B925E49533108BFC3BD26B1A710351A8EB0D937D8EEAF54FC6BE0D7D355A215E7D26FF59E525C6891991657791C3492C29874EC1A0C926EF73113347668305266F2CABFB4624F4023CD7ACA19459B2C2FB9A39024100CE23A8CAB7AD1BBEEE955EFB50A325146F2323F5E08E05AB52E3D8D1BEFDF6E9331CF79D5EF5371B9C3D1CC49AD5D4DEB2222FBEF9E8F07DA81B7987FEF8DEB5024100BDB73C2939416C8F533309D5910A6CB7E724826DE0A510E101F43F2EDEEBF20D8A007B09BFEC1B0090A0BBC3E0A8CF6642357167EEFCF9A2C443E879AEF315EB024063573DFDCB7C8A157E66CFAA2F57200473F6AC5ACEC7C2A5A8C192F8A9A13C3F6FD972DF0D270152B5FDA18281D7B0C94D713A2F3FC2693B352B606B108E20CD0241009210B3D7216807C749CD6FF2E85AD85DB72EDAE4F222E49DF8BD2EB04D9DE79D18F549C5B5E878D9B480AC50217697482A0DAFA4CFD221F97996B73488C84BC5024006F0F9DD96248A27FAFB94B0A93676D653A99C7C7B9DCDADE65414C6889284B924F26A23AFFA86AF3FD804453B73FDFEA9310CC909BFC9928D5F757198B1075D


/*****二进制分解（和PKCS#1只是多了一段数据，详细结构参考PKCS#1的）*****/

/*后续内容长度*/
30 82 0276

	/*固定版本号*/
	02 01
		00

	/*#####相对于KCS#1仅仅是在此处开始插入部分数据 Begin#####*/

	/*
	固定内容 OID
	*/
	30 0D
		06 09 2A864886F70D010101
		05 00

	/*后续内容长度，后面内容长度为0x0260字节，和开头格式一致*/
	04 82 0260 //04 OCTET STRING

		/*后续内容长度，后面内容长度为0x025C字节，和开头格式一致*/
		30 82 025C

			/*固定版本号*/
			02 01
				00

			/*#####相对于KCS#1仅仅是在此处结束插入部分数据 End#####*/

			/*RSA Modulus内容*/
			02 81 81 
				0098C3DF8CDFE444CC3B58A...

	...后续内容省略...
```


