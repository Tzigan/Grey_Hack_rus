# Grey Hack Russian - Русификатор для игры Grey Hack

## Требования
- Игра Grey Hack
- BepInEx 6.0 pre-2

## Установка
1. Установите [BepInEx 6.0 pre-2](https://github.com/BepInEx/BepInEx/releases/tag/v6.0.0-pre.2)
   - Скачайте файл `BepInEx_Unity.Mono_x64_6.0.0-pre.2.zip`
   - Распакуйте его в корневую папку игры Grey Hack
2. Распакуйте содержимое этого архива в папку с игрой
3. Запустите игру и наслаждайтесь переводом!
## Cтруктура для установки
Grey Hack/
   ├── BepInEx/
   │   ├── core/
   │   │   ├── BepInEx.Core.dll
   │   │   ├── BepInEx.Unity.Common.dll
   │   │   ├── BepInEx.Unity.Mono.dll
   │   │   ├── 0Harmony.dll
   │   │   └── ... (другие библиотеки)
   │   └── plugins/
   │       ├── GreyHackRussianPlugin.dll
   │       └── russian_translation.txt
   ├── Grey Hack.exe
   └── ... (остальные файлы игры)
## Настройка переводов
Переводы находятся в файле `BepInEx\plugins\GreyHackRussian\russian_translation.txt` и имеют формат:
оригинальная строка=переведенная строка
### Список отсутствующих переводов
Строки, которые требуют перевода, автоматически сохраняются в файл `BepInEx\plugins\GreyHackRussian\Translation\untranslated.txt`.
## Разработка
Для настройки среды разработки:

1. Установите BepInEx 6.0 pre-2 в игру Grey Hack
2. Скопируйте следующие библиотеки:

2.1 Создайте папку libs, если её нет
	mkdir -p libs
2.2 Скопируйте BepInEx библиотеки
	cp "путь_к_игре/BepInEx/core/BepInEx.Core.dll" libs/ 
	cp "путь_к_игре/BepInEx/core/BepInEx.Unity.Common.dll" libs/ 
	cp "путь_к_игре/BepInEx/core/BepInEx.Unity.Mono.dll" libs/ 
	cp "путь_к_игре/BepInEx/core/0Harmony.dll" libs/
2.3 Скопируйте библиотеки Unity/игры
	cp "путь_к_игре/Grey Hack_Data/Managed/UnityEngine.dll" libs/ 
	cp "путь_к_игре/Grey Hack_Data/Managed/UnityEngine.CoreModule.dll" libs/ 
	cp "путь_к_игре/Grey Hack_Data/Managed/UnityEngine.UI.dll" libs/ 
	cp "путь_к_игре/Grey Hack_Data/Managed/Assembly-CSharp.dll" libs/
3. Соберите проект командой: `dotnet build`
