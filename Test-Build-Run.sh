#!/usr/bin/env bash
#[zh_CN] 在Linux、macOS系统终端中运行这个脚本文件，自动完成.cs文件编译和运行。需先安装了.NET Core SDK，支持.NET Core 2.0及以上版本（.NET 5+）
#如果你有BouncyCastle加密增强库（BouncyCastle.xxxx.dll），请直接复制对应版本的dll文件到源码根目录，编译运行后即可获得全部加密签名模式支持

#[en_US] Run this script file in the Linux and macOS system terminals to automatically complete the compilation and operation of the .cs file. .NET Core SDK needs to be installed first, support .NET Core 2.0 and above (.NET 5+)
#If you have BouncyCastle encryption enhancement library (BouncyCastle.xxxx.dll), please directly copy the corresponding version of the dll file to the root directory of this source code. After compiling and running, you can get all encryption signature mode support


clear

isZh=0
if [ $(echo ${LANG/_/-} | grep -Ei "\\b(zh|cn)\\b") ]; then isZh=1; fi

function echo2(){
	if [ $isZh == 1 ]; then echo $1;
	else echo $2; fi
}
cd `dirname $0`
echo2 "显示语言：简体中文    `pwd`" "Language: English    `pwd`"
function err(){
	if [ $isZh == 1 ]; then echo -e "\e[31m$1\e[0m";
	else echo -e "\e[31m$2\e[0m"; fi
}
function exit2(){
	if [ $isZh == 1 ]; then read -n1 -rp "请按任意键退出..." key;
	else read -n1 -rp "Press any key to exit..."; fi
	exit
}


dllName="RSA-CSharp.NET-Standard.dll"
dllPath="target/$dllName"
if [ ! -e $dllPath ]; then dllPath=""; fi
if [ "$dllPath" != "" ]; then
	echo2 "检测到已生成的dll：${dllPath}，是否使用此dll参与测试？(Y/N) N" "Generated dll detected: ${dllPath}, do you want to use this dll to participate in the test? (Y/N) N"
	read -rp "> " step
	if [ "${step^^}" != "Y" ]; then dllPath=""; fi
	if [ "$dllPath" != "" ]; then
		echo2 "dll参与测试：$dllPath" "dll participates in the test: $dllPath"
		echo 
	fi
fi


#.NET CLI telemetry https://learn.microsoft.com/en-us/dotnet/core/tools/telemetry
DOTNET_CLI_TELEMETRY_OPTOUT=1

echo2 "正在读取.NET Core版本：" "Reading .NET Core Version:"

dotnet --version
[ ! $? -eq 0 ] && {
	echo 
	err "需要安装.NET Core SDK [支持.NET Core 2.0及以上版本，.NET 5+] 才能使用本脚本编译运行.cs文件，可以到 https://learn.microsoft.com/zh-cn/dotnet/core/install/ 下载安装SDK"\
		"You need to install .NET Core SDK [support .NET Core 2.0 and above, .NET 5+] to use this script to compile and run the .cs file. You can download and install the SDK at https://learn.microsoft.com/en-us/dotnet/core/install/";
	exit2;
}


rootDir=rsaTest
echo 
echo2 "正在创.NET Core项目${rootDir}..." "Creating .NET Core project ${rootDir}..."
if [ ! -e $rootDir ]; then
	mkdir -p $rootDir
else
	rm ${rootDir}/* > /dev/null 2>&1
fi

cd $rootDir
dotnet new console
[ ! $? -eq 0    -o    ! -e $rootDir*proj ] && {
	echo 
	err "创建项目命令执行失败" "The command to create a project failed to execute"
	exit2;
}
echo 

projFile=`ls $rootDir*proj`;
sed -i -e 's/<PropertyGroup>/<PropertyGroup> <DefineConstants>\$(DefineConstants);RSA_BUILD__NET_CORE<\/DefineConstants>/g' $projFile;
sed -i -e 's/Nullable>enable/Nullable>disable/g' $projFile
if [ "$dllPath" != "" ]; then
	sed -i -e 's/<\/Project>/<ItemGroup><Reference Include="rsaDLL"><HintPath>'"${dllName}"'<\/HintPath><Private>True<\/Private><\/Reference><\/ItemGroup><\/Project>/g' $projFile
fi
echo2 "已修改proj项目配置文件：${projFile}，已启用RSA_BUILD__NET_CORE条件编译符号" "Modified proj project configuration file: ${projFile}, enabled RSA_BUILD__NET_CORE conditional compilation symbol"
echo 

cd ..
if [ "$dllPath" == "" ]; then
	cp *.cs $rootDir > /dev/null
else
	cp Program.cs $rootDir > /dev/null
	cp $dllPath $rootDir > /dev/null
fi
if [ -e *.dll ]; then
	cp *.dll $rootDir > /dev/null
fi
cd $rootDir



echo2 "正在编译.NET Core项目${rootDir}..." "Compiling .NET Core project ${rootDir}..."
echo 
dotnet run -cmd=1 -zh=${isZh}

exit2;
