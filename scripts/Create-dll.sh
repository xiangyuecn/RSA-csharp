#!/usr/bin/env bash
#[zh_CN] 在Linux、macOS系统终端中运行这个脚本文件，自动完成.cs文件编译生成.NET Standard 2.0的dll。需先安装了.NET Core SDK（支持.NET Core 2.0及以上版本，.NET 5+）
#[en_US] Run this script file in the terminal of Linux and macOS system to automatically compile the .cs file and generate the dll of .NET Standard 2.0. The .NET Core SDK needs to be installed first (supports .NET Core 2.0 and above, .NET 5+)


#.NET Core --framework: https://learn.microsoft.com/en-us/dotnet/standard/frameworks
CoreTargetSDK=netstandard2.0


clear

isZh=0
if [ $(echo ${LANG/_/-} | grep -Ei "\\b(zh|cn)\\b") ]; then isZh=1; fi

function echo2(){
	if [ $isZh == 1 ]; then echo $1;
	else echo $2; fi
}
cd `dirname $0`
cd ../
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


echo2 "本脚本默认生成.NET Standard 2.0的dll，支持.NET Core 2.0及以上版本（.NET 5+），兼容.NET Framework 4.6.2及以上版本（请使用同名的bat脚本来创建Framework专用的dll）。" "This script generates .NET Standard 2.0 dll by default, supports .NET Core 2.0 and above (.NET 5+), and is compatible with .NET Framework 4.6.2 and above (Please use the bat script with the same name to create a Framework-specific dll)."
echo 
echo2 "请输入需要生成的dll版本号：" "Please enter the version number of the dll that needs to be generated:"
read -rp "> " dllVer


#.NET CLI telemetry https://learn.microsoft.com/en-us/dotnet/core/tools/telemetry
DOTNET_CLI_TELEMETRY_OPTOUT=1


projName=RSA-CSharp.NET-Standard
rootDir=target/$projName

echo2 "正在创.NET Core项目${rootDir}..." "Creating .NET Core project ${rootDir}..."
if [ ! -e $rootDir ]; then
	mkdir -p $rootDir
else
	rm ${rootDir}/* > /dev/null 2>&1
fi

cd $rootDir
dotnet new classlib -f $CoreTargetSDK
[ ! $? -eq 0    -o    ! -e $projName*proj ] && {
	echo 
	err "创建项目命令执行失败，请检查是否安装了.NET Core 2.0及以上版本的SDK（.NET 5+）" "The execution of the command to create a project failed. Please check whether the SDK of .NET Core 2.0 and above (.NET 5+) is installed"
	exit2;
}
echo 

prop="<Version>${dllVer}<\\/Version> <Copyright>`date '+%Y-%m-%d'`, MIT, Copyright `date '+%Y'` xiangyuecn<\\/Copyright> <Authors>xiangyuecn<\\/Authors> <Product>${CoreTargetSDK}, https:\\/\\/github.com\\/xiangyuecn\\/RSA-csharp<\\/Product> <Description>RSA-csharp, ${CoreTargetSDK}, `date '+%Y-%m-%d'`<\\/Description>"

projFile=`ls $projName*proj`;
sed -i -e 's/<PropertyGroup>/<PropertyGroup> <DefineConstants>\$(DefineConstants);RSA_BUILD__NET_CORE<\/DefineConstants> '"${prop}"'/g' $projFile;
sed -i -e 's/Nullable>enable/Nullable>disable/g' $projFile
echo2 "已修改proj项目配置文件：${projFile}，已启用RSA_BUILD__NET_CORE条件编译符号" "Modified proj project configuration file: ${projFile}, enabled RSA_BUILD__NET_CORE conditional compilation symbol"
echo 

rm *.cs > /dev/null 2>&1
cp ../../RSA_PEM.cs ./ > /dev/null
cp ../../RSA_Util.cs ./ > /dev/null



echo2 "正在编译.NET Core项目${rootDir}..." "Compiling .NET Core project ${rootDir}..."
echo 
dotnet build -c Release
[ ! $? -eq 0 ] && {
	echo 
	err "生成dll失败" "Failed to generate dll"
	exit2;
}

cd ../../
dllRaw=${rootDir}/bin/Release/${CoreTargetSDK}/${projName}.dll
dllPath=target/${projName}.dll
rm $dllPath > /dev/null 2>&1
[ -e $dllPath ] && {
	echo 
	err "无法删除旧文件：${dllPath}" "Unable to delete old file: ${dllPath}"
	exit2;
}
cp $dllRaw target

[ ! -e $dllPath ] && {
	echo 
	err "未定位到生成的dll文件路径，请到${rootDir}/bin寻找生成的dll文件  " "The generated dll file path is not located, please go to ${rootDir}/bin to find the generated dll file"
	exit2;
}
echo 
echo2 "已生成dll，文件在源码根目录：${dllPath}。  " "The dll has been generated, and the file is in the root directory of the source code: ${dllPath}."
echo 

exit2;
