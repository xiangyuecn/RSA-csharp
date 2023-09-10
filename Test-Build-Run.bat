@echo off
::[zh_CN] ��Windowsϵͳ��˫����������ű��ļ����Զ����.cs�ļ���������С����Ȱ�װ��.NET Framework 4.5+������.NET Core SDK��֧��.NET Core 2.0�����ϰ汾��.NET 5+��  
::�������BouncyCastle������ǿ�⣨BouncyCastle.xxxx.dll������ֱ�Ӹ��ƶ�Ӧ�汾��dll�ļ�����Դ���Ŀ¼���������к󼴿ɻ��ȫ������ǩ��ģʽ֧��  

::[en_US] Double-click to run this script file in the Windows system, and automatically complete the compilation and operation of the .cs file. Need to install .NET Framework 4.5+ or .NET Core SDK (support .NET Core 2.0 and above, .NET 5+)
::If you have BouncyCastle encryption enhancement library (BouncyCastle.xxxx.dll), please directly copy the corresponding version of the dll file to the root directory of this source code. After compiling and running, you can get all encryption signature mode support


cls
::chcp 437
set isZh=0
ver | find "�汾%qjkTTT%" > nul && set isZh=1
goto Run
:echo2
	if "%isZh%"=="1" echo %~1
	if "%isZh%"=="0" echo %~2
	goto:eof

:Run
cd /d %~dp0
call:echo2 "��ʾ���ԣ���������    %cd%" "Language: English    %cd%"
echo.
call:echo2 "ѡ���������ģʽ���������ţ�  " "Select the compilation and running mode, please enter the number:"
call:echo2 "    1. ʹ��.NET Framework���б��루֧��.NET Framework 4.5�����ϰ汾��  " "    1. Use .NET Framework to compile (support .NET Framework 4.5 and above)"
call:echo2 "    2. ʹ��.NET Core���б��루֧��.NET Core 2.0�����ϰ汾��.NET 5+��  " "    2. Use .NET Core to compile (support .NET Core 2.0 and above, .NET 5+)"
call:echo2 "    3. �˳�  " "    3. Exit"

set step=&set /p step=^> 
	if "%step%"=="1" goto RunFramework
	if "%step%"=="2" goto RunDotnet
	if "%step%"=="3" goto End
	call:echo2 "�����Ч�����������룡  " "The number is invalid, please re-enter!"
	goto Run


:findDLL
	set dllName=%~1
	set dllPath=target\%~1
	if not exist %dllPath% set dllPath=
	if "%dllPath%"=="" goto dllPath_End
		call:echo2 "��⵽�����ɵ�dll��%dllPath%���Ƿ�ʹ�ô�dll������ԣ�(Y/N) N  " "Generated dll detected: %dllPath%, do you want to use this dll to participate in the test? (Y/N) N"
		set step=&set /p step=^> 
		if /i not "%step%"=="Y" set dllPath=
		if not "%dllPath%"=="" (
			call:echo2 "dll������ԣ�%dllPath%" "dll participates in the test: %dllPath%"
			echo.
		)
	:dllPath_End
	goto:eof


:RunDotnet
call:findDLL "RSA-CSharp.NET-Standard.dll"

::.NET CLI telemetry https://learn.microsoft.com/en-us/dotnet/core/tools/telemetry
set DOTNET_CLI_TELEMETRY_OPTOUT=1

call:echo2 "���ڶ�ȡ.NET Core�汾��  " "Reading .NET Core Version:"

dotnet --version
if errorlevel 1 (
	echo.
	call:echo2 "��Ҫ��װ.NET Core SDK [֧��.NET Core 2.0�����ϰ汾��.NET 5+] ����ʹ��.NET Coreģʽ��������.cs�ļ������߳���ѡ��.NET Frameworkģʽ���б��롣���Ե� https://dotnet.microsoft.com/zh-cn/download/dotnet ���ذ�װ.NET Core SDK  " "You need to install .NET Core SDK [support .NET Core 2.0 and above, .NET 5+] to compile and run .cs files using .NET Core mode, or try to select .NET Framework mode for compilation. You can go to https://dotnet.microsoft.com/en-us/download/dotnet to download and install the .NET Core SDK"
	goto Pause
)


set rootDir=rsaTest
echo.
call:echo2 "���ڴ�.NET Core��Ŀ%rootDir%..." "Creating .NET Core project %rootDir%..."
if not exist %rootDir% (
	md %rootDir%
) else (
	del %rootDir%\* /Q > nul
)

cd %rootDir%
dotnet new console
if errorlevel 1 goto if_dncE
if not exist %rootDir%*proj goto if_dncE
	goto dncE_if
	:if_dncE
		echo.
		call:echo2 "������Ŀ����ִ��ʧ��  " "The command to create a project failed to execute"
		goto Pause
	:dncE_if
echo.

setlocal enabledelayedexpansion
for /f "delims=" %%f in ('dir /b %rootDir%*proj') do (
	for /f "delims=" %%v in (%%f) do (
		set a=%%v
		set "a=!a:<PropertyGroup>=<PropertyGroup> <DefineConstants>$(DefineConstants);RSA_BUILD__NET_CORE</DefineConstants>!"
		set "a=!a:Nullable>enable=Nullable>disable!"
		if not "%dllPath%"=="" (
			set "a=!a:</Project>=<ItemGroup><Reference Include='rsaDLL'><HintPath>%dllName%</HintPath><Private>True</Private></Reference></ItemGroup></Project>!"
		)
		echo !a!>>tmp.txt
	)
	move tmp.txt %%f > nul
	call:echo2 "���޸�proj��Ŀ�����ļ���%%f��������RSA_BUILD__NET_CORE�����������  " "Modified proj project configuration file: %%f, enabled RSA_BUILD__NET_CORE conditional compilation symbol"
	echo.
)

cd ..
if "%dllPath%"=="" (
	xcopy *.cs %rootDir% /Y > nul
) else (
	xcopy Program.cs %rootDir% /Y > nul
	xcopy %dllPath% %rootDir% /Y > nul
)
if exist *.dll (
	xcopy *.dll %rootDir% /Y > nul
)
cd %rootDir%



echo.
call:echo2 "���ڱ���.NET Core��Ŀ%rootDir%..." "Compiling .NET Core project %rootDir%..."
echo.
dotnet run -cmd=1 -zh=%isZh%
goto Pause






:RunFramework
call:findDLL "RSA-CSharp.NET-Framework.dll"
if not exist target\RSA-CSharp.NET-Framework.dll (
	call:findDLL "RSA-CSharp.NET-Standard.dll"
)

cd /d C:\Windows\Microsoft.NET\Framework\v4.*
set FwDir=%cd%\
::set FwDir=C:\Windows\Microsoft.NET\Framework\xxxx\
echo .NET Framework Path: %FwDir%

call:echo2 "���ڶ�ȡ.NET Framework�汾��  " "Reading .NET Framework Version:"
%FwDir%MSBuild /ver
if errorlevel 1 (
	echo.
	call:echo2 "��Ҫ��װ.NET Framework 4.5�����ϰ汾����ʹ��.NET Frameworkģʽ��������.cs�ļ������߳���ѡ��.NET Coreģʽ���б��롣���Ե� https://dotnet.microsoft.com/zh-cn/download/dotnet-framework ���ذ�װ.NET Framework  " "You need to install .NET Framework 4.5 or above to compile and run .cs files using .NET Framework mode, or try to select .NET Core mode for compilation. You can go to https://dotnet.microsoft.com/en-us/download/dotnet-framework to download and install .NET Framework"
	goto Pause
)
cd /d %~dp0


set rootDir=rsaTestFw
echo.
call:echo2 "���ڴ���.NET Framework��Ŀ%rootDir%..." "Creating .NET Framework project %rootDir%..."
if not exist %rootDir% (
	md %rootDir%
) else (
	del %rootDir%\* /Q > nul
)

if "%dllPath%"=="" (
	xcopy *.cs %rootDir% /Y > nul
) else (
	xcopy Program.cs %rootDir% /Y > nul
	xcopy %dllPath% %rootDir% /Y > nul
)
if exist *.dll (
	xcopy *.dll %rootDir% /Y > nul
)
cd %rootDir%

echo.
call:echo2 "���ڱ���.NET Framework��Ŀ%rootDir%..." "Compiling .NET Framework project %rootDir%..."
echo.
::https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-options/
set rd=
if not "%dllPath%"=="" set rd=/r:"%dllName%"
%FwDir%csc /t:exe /r:"%FwDir%System.Numerics.dll" %rd% /out:%rootDir%.exe *.cs
if errorlevel 1 (
	echo.
	call:echo2 "��Ŀ%rootDir%����ʧ��  " "Compilation failed for project %rootDir%"
	goto Pause
)

%rootDir%.exe -cmd=1 -zh=%isZh%

:Pause
pause
goto Run
:End