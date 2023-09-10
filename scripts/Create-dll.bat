@echo off
::[zh_CN] 在Windows系统中双击运行这个脚本文件，自动完成.cs文件编译生成通用的dll和exe。需先安装了.NET Framework 4.5+，和.NET Core SDK（支持.NET Core 2.0及以上版本，.NET 5+）   
::[en_US] Double-click to run this script file in the Windows system, and automatically complete the compilation of the .cs file to generate common dll and exe. Need to install .NET Framework 4.5+, and .NET Core SDK (support .NET Core 2.0 and above, .NET 5+)


::.NET Core --framework: https://learn.microsoft.com/en-us/dotnet/standard/frameworks
set CoreTarget=netstandard2.0
::TargetFrameworkVersion
set FrameworkTarget=v4.5


cls
::chcp 437
set isZh=0
ver | find "版本%qjkTTT%" > nul && set isZh=1
goto Run
:echo2
	if "%isZh%"=="1" echo %~1
	if "%isZh%"=="0" echo %~2
	goto:eof

:Run
cd /d %~dp0
cd ..\
call:echo2 "显示语言：简体中文    %cd%" "Language: English    %cd%"
echo.
call:echo2 "请输入编号：  " "Please enter the number:"
call:echo2 "    1. 配置生成dll的版本号（当前：%dllVer%）  " "    1. Configure the version number of the generated dll (currently: %dllVer%)"
call:echo2 "    2. 使用.NET Core进行编译生成.NET Standard 2.0的dll（支持.NET Core 2.0及以上版本，.NET 5+），兼容.NET Framework 4.6.2及以上版本  " "    2. Use .NET Core to compile and generate .NET Standard 2.0 dll (support .NET Core 2.0 and above, .NET 5+), compatible with .NET Framework 4.6.2 and above"
call:echo2 "    3. 使用.NET Framework 4.5进行编译生成Framework可用的dll  " "    3. Use .NET Framework 4.5 to compile and generate dll available to Framework"
call:echo2 "    4. 使用.NET Framework 4.5进行编译生成exe  " "    4. Use .NET Framework 4.5 to compile and generate exe"
call:echo2 "    5. 退出  " "    5. Exit"

set step=&set /p step=^> 
	if "%step%"=="1" goto SetVer
	if "%step%"=="5" goto End
	
	if "%dllVer%"=="" (
		call:echo2 "请先配置版本号！  " "Please configure the version number first!"
		goto Run
	)
	if "%step%"=="2" goto RunDotnet
	
	set FrameworkType=dll
	if "%step%"=="3" goto RunFramework
	if "%step%"=="4" (
		set FrameworkType=exe
		goto RunFramework
	)
	
	call:echo2 "编号无效，请重新输入！  " "The number is invalid, please re-enter!"
	goto Run

:SetVer
	call:echo2 "请输入生成dll的版本号：  " "Please enter the version number of the generated dll:"
	set dllVer=&set /p dllVer=^> 
	goto Run

:RunDotnet
::.NET CLI telemetry https://learn.microsoft.com/en-us/dotnet/core/tools/telemetry
set DOTNET_CLI_TELEMETRY_OPTOUT=1

set projName=RSA-CSharp.NET-Standard
set rootDir=target\%projName%
echo.
call:echo2 "正在创.NET Core项目%rootDir%..." "Creating .NET Core project %rootDir%..."
if not exist %rootDir% (
	md %rootDir%
) else (
	del %rootDir%\* /Q > nul
)

cd %rootDir%
dotnet new classlib -f %CoreTarget%
if errorlevel 1 goto if_dncE
if not exist %projName%*proj goto if_dncE
	goto dncE_if
	:if_dncE
		echo.
		call:echo2 "创建项目命令执行失败，请检查是否安装了.NET Core 2.0及以上版本的SDK（.NET 5+）  " "The execution of the command to create a project failed. Please check whether the SDK of .NET Core 2.0 and above (.NET 5+) is installed"
		goto Pause
	:dncE_if
echo.


set prop="<Version>%dllVer%</Version> <Copyright>%date:~,10%, MIT, Copyright %date:~,4% xiangyuecn</Copyright> <Authors>xiangyuecn</Authors> <Product>%CoreTarget%, https://github.com/xiangyuecn/RSA-csharp</Product> <Description>RSA-csharp, %CoreTarget%, %date:~,10%</Description>"

setlocal enabledelayedexpansion
for /f "delims=" %%f in ('dir /b %projName%*proj') do (
	for /f "delims=" %%v in (%%f) do (
		set a=%%v
		set "a=!a:<PropertyGroup>=<PropertyGroup> <DefineConstants>$(DefineConstants);RSA_BUILD__NET_CORE</DefineConstants> %prop:~1,-1%!"
		set "a=!a:Nullable>enable=Nullable>disable!"
		echo !a!>>tmp.txt
	)
	move tmp.txt %%f > nul
	call:echo2 "已修改proj项目配置文件：%%f，已启用RSA_BUILD__NET_CORE条件编译符号  " "Modified proj project configuration file: %%f, enabled RSA_BUILD__NET_CORE conditional compilation symbol"
	echo.
)

del *.cs /Q > nul
xcopy ..\..\RSA_PEM.cs /Y > nul
xcopy ..\..\RSA_Util.cs /Y > nul



echo.
call:echo2 "正在编译.NET Core项目%rootDir%..." "Compiling .NET Core project %rootDir%..."
echo.
dotnet build -c Release
if errorlevel 1 (
	echo.
	call:echo2 "生成dll失败  " "Failed to generate dll"
	goto Pause
)

cd ..\..
set dllRaw=%rootDir%\bin\Release\%CoreTarget%\%projName%.dll
set dllPath=target\%projName%.dll
del %dllPath% /Q > nul 2>&1
if exist %dllPath% (
	echo.
	call:echo2 "无法删除旧文件：%dllPath%  " "Unable to delete old file: %dllPath%"
	goto Pause
)
xcopy %dllRaw% target /Y

if not exist %dllPath% (
	echo.
	call:echo2 "未定位到生成的dll文件路径，请到%rootDir%\bin寻找生成的dll文件  " "The generated dll file path is not located, please go to %rootDir%\bin to find the generated dll file"
	goto Pause
)
echo.
call:echo2 "已生成dll，文件在源码根目录：%dllPath%。  " "The dll has been generated, and the file is in the root directory of the source code: %dllPath%."
echo.
goto Pause



:RunFramework
cd /d C:\Windows\Microsoft.NET\Framework\v4.*
set FwDir=%cd%\
::set FwDir=C:\Windows\Microsoft.NET\Framework\xxxx\
echo .NET Framework Path: %FwDir%

call:echo2 "正在读取.NET Framework版本：  " "Reading .NET Framework Version:"
%FwDir%MSBuild /ver
if errorlevel 1 (
	echo.
	call:echo2 "需要安装.NET Framework 4.5及以上版本才能使用.NET Framework模式编译运行.cs文件，或者尝试选择.NET Core模式进行编译。可以到 https://dotnet.microsoft.com/zh-cn/download/dotnet-framework 下载安装.NET Framework  " "You need to install .NET Framework 4.5 or above to compile and run .cs files using .NET Framework mode, or try to select .NET Core mode for compilation. You can go to https://dotnet.microsoft.com/en-us/download/dotnet-framework to download and install .NET Framework"
	goto Pause
)
cd /d %~dp0
cd ..\


if "%FrameworkType%"=="exe" (
	set projName=RSA-CSharp.NET-Framework-v%dllVer%-Test
) else (
	set projName=RSA-CSharp.NET-Framework
)
set rootDir=target\%projName%
echo.
call:echo2 "正在创建.NET Framework项目%rootDir%..." "Creating .NET Framework project %rootDir%..."
if not exist %rootDir% (
	md %rootDir%
) else (
	del %rootDir%\* /Q > nul
)
cd %rootDir%

xcopy ..\..\RSA_PEM.cs /Y > nul
xcopy ..\..\RSA_Util.cs /Y > nul
if "%FrameworkType%"=="exe" xcopy ..\..\Program.cs /Y > nul
call:createFrameworkProj

echo.
call:echo2 "正在编译.NET Framework项目%rootDir%..." "Compiling .NET Framework project %rootDir%..."
echo.
::https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-command-line-reference
%FwDir%MSBuild build.proj -property:Configuration=Release
if errorlevel 1 (
	echo.
	call:echo2 "项目%rootDir%编译失败  " "Compilation failed for project %rootDir%"
	goto Pause
)

cd ..\..
set fileExt=%FrameworkType%
set dllRaw=%rootDir%\bin\Release\%projName%.%fileExt%
set dllPath=target\%projName%.%fileExt%
del %dllPath% /Q > nul 2>&1
if exist %dllPath% (
	echo.
	call:echo2 "无法删除旧文件：%dllPath%  " "Unable to delete old file: %dllPath%"
	goto Pause
)
xcopy %dllRaw% target /Y

if not exist %dllPath% (
	echo.
	call:echo2 "未定位到生成的%fileExt%文件路径，请到%rootDir%\bin寻找生成的%fileExt%文件  " "The generated %fileExt% file path is not located, please go to %rootDir%\bin to find the generated %fileExt% file"
	goto Pause
)
echo.
call:echo2 "已生成%fileExt%，文件在源码根目录：%dllPath%。  " "The %fileExt% has been generated, and the file is in the root directory of the source code: %dllPath%."
echo.
goto Pause




:createFrameworkProj
	echo using System.Reflection;>AssemblyInfo.cs
	echo using System.Runtime.CompilerServices;>>AssemblyInfo.cs
	echo using System.Runtime.InteropServices;>>AssemblyInfo.cs
	echo [assembly: AssemblyTitle("%projName%")]>>AssemblyInfo.cs
	echo [assembly: AssemblyCopyright("%date:~,10%, MIT, Copyright %date:~,4% xiangyuecn")]>>AssemblyInfo.cs
	echo [assembly: AssemblyCompany("xiangyuecn")]>>AssemblyInfo.cs
	echo [assembly: AssemblyProduct(".NET Framework %FrameworkTarget%, https://github.com/xiangyuecn/RSA-csharp")]>>AssemblyInfo.cs
	echo [assembly: AssemblyDescription("RSA-csharp, .NET Framework %FrameworkTarget%, %date:~,10%")]>>AssemblyInfo.cs
	echo [assembly: AssemblyVersion("%dllVer%.0.0")]>>AssemblyInfo.cs
	echo [assembly: AssemblyFileVersion("%dllVer%.0.0")]>>AssemblyInfo.cs
	
	
	echo ^<Project ToolsVersion="15.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003"^>>build.proj
	echo ^<PropertyGroup^>>>build.proj
	echo ^<TargetFrameworkVersion^>%FrameworkTarget%^</TargetFrameworkVersion^>>>build.proj
	if "%FrameworkType%"=="exe" (
		echo ^<OutputType^>Exe^</OutputType^>>>build.proj
	) else (
		echo ^<OutputType^>Library^</OutputType^>>>build.proj
	)
	echo ^<OutputPath^>bin\Release\^</OutputPath^>>>build.proj
	echo ^<AssemblyName^>%projName%^</AssemblyName^>>>build.proj
	echo ^</PropertyGroup^>>>build.proj

	echo ^<ItemGroup^>>>build.proj
	echo ^<Reference Include="System" /^>>>build.proj
	echo ^<Reference Include="System.Core" /^>>>build.proj
	echo ^<Reference Include="System.Numerics" /^>>>build.proj
	echo ^<Reference Include="Microsoft.CSharp" /^>>>build.proj

	echo ^<Compile Include="*.cs"/^>>>build.proj
	echo ^</ItemGroup^>>>build.proj
	echo ^<Import Project="$(MSBuildToolsPath)\Microsoft.CSharp.targets" /^>>>build.proj
	echo ^</Project^>>>build.proj
	goto:eof


:Pause
pause
goto Run
:End