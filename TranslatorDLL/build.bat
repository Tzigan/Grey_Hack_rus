@echo off
chcp 1251 >nul
echo ������ Grey Hack Translator DLL...

REM ������� ����� ������� (����� �� ��� � ������)
echo ^<Project Sdk="Microsoft.NET.Sdk"^> > GreyHackTranslator.csproj
echo   ^<PropertyGroup^> >> GreyHackTranslator.csproj
echo     ^<TargetFramework^>net46^</TargetFramework^> >> GreyHackTranslator.csproj
echo     ^<OutputType^>Library^</OutputType^> >> GreyHackTranslator.csproj
echo   ^</PropertyGroup^> >> GreyHackTranslator.csproj
echo   ^<ItemGroup^> >> GreyHackTranslator.csproj
echo     ^<PackageReference Include="HarmonyX" Version="2.5.5" /^> >> GreyHackTranslator.csproj
echo   ^</ItemGroup^> >> GreyHackTranslator.csproj
echo   ^<ItemGroup^> >> GreyHackTranslator.csproj
echo     ^<Reference Include="UnityEngine"^> >> GreyHackTranslator.csproj
echo       ^<HintPath^>$(ProjectDir)lib\UnityEngine.dll^</HintPath^> >> GreyHackTranslator.csproj
echo     ^</Reference^> >> GreyHackTranslator.csproj
echo     ^<Reference Include="UnityEngine.UI"^> >> GreyHackTranslator.csproj
echo       ^<HintPath^>$(ProjectDir)lib\UnityEngine.UI.dll^</HintPath^> >> GreyHackTranslator.csproj
echo     ^</Reference^> >> GreyHackTranslator.csproj
echo     ^<Reference Include="Assembly-CSharp"^> >> GreyHackTranslator.csproj
echo       ^<HintPath^>$(ProjectDir)lib\Assembly-CSharp.dll^</HintPath^> >> GreyHackTranslator.csproj
echo     ^</Reference^> >> GreyHackTranslator.csproj
echo     ^<Reference Include="TextMeshPro-1.0.55.56.0b12"^> >> GreyHackTranslator.csproj
echo       ^<HintPath^>$(ProjectDir)lib\TextMeshPro-1.0.55.56.0b12.dll^</HintPath^> >> GreyHackTranslator.csproj
echo     ^</Reference^> >> GreyHackTranslator.csproj
echo   ^</ItemGroup^> >> GreyHackTranslator.csproj
echo ^</Project^> >> GreyHackTranslator.csproj

REM ������� ���� ������� ��� ���������
echo ^<Project Sdk="Microsoft.NET.Sdk"^> > GreyHackInjector.csproj
echo   ^<PropertyGroup^> >> GreyHackInjector.csproj
echo     ^<TargetFramework^>net46^</TargetFramework^> >> GreyHackInjector.csproj
echo     ^<OutputType^>Exe^</OutputType^> >> GreyHackInjector.csproj
echo   ^</PropertyGroup^> >> GreyHackInjector.csproj
echo ^</Project^> >> GreyHackInjector.csproj

REM �������� ������� ��������� Unity
if not exist lib\UnityEngine.dll (
    echo ��������������: ���������� Unity �� �������!
    echo ���������� ��������� ����� �� ����� ���� Grey Hack:
    echo - UnityEngine.dll
    echo - UnityEngine.UI.dll
    echo - Assembly-CSharp.dll
    echo - TextMeshPro-1.0.55.56.0b12.dll (���� ����)
    echo �� ����� Grey Hack\Grey Hack_Data\Managed � ����� lib �������
    mkdir lib 2>nul
    pause
    exit /b 1
)

REM ������ DLL � ������� dotnet CLI
echo ������ DLL...
dotnet build GreyHackTranslator.csproj -c Release

REM ������ ���������
echo ������ ���������...
dotnet build GreyHackInjector.csproj -c Release

REM ����������� ������ � �������� �����
echo ����������� ������...
mkdir bin 2>nul
copy bin\Release\net46\GreyHackTranslator.dll bin\ /y
copy bin\Release\net46\GreyHackInjector.exe bin\ /y
copy bin\Release\net46\*.dll bin\ /y

echo ������!
pause