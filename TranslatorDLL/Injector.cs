using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace GreyHackInjector
{
    class Injector
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll")]
        static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll")]
        static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll")]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out UIntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

        [DllImport("kernel32.dll")]
        static extern bool CloseHandle(IntPtr hObject);

        const int PROCESS_ALL_ACCESS = 0x1F0FFF;
        const uint MEM_COMMIT = 0x1000;
        const uint PAGE_READWRITE = 0x04;

        static string logPath = "injector_log.txt";
        static string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        static void Main()
        {
            try
            {
                Console.WriteLine("Grey Hack Translator Injector");
                Console.WriteLine("=============================");
                Log("Injector запущен");
                EmergencyLog("Injector запущен");

                // Проверяем наличие DLL
                string dllPath = Path.GetFullPath("GreyHackTranslator.dll");
                if (!File.Exists(dllPath))
                {
                    string message = $"DLL не найдена по пути: {dllPath}";
                    Console.WriteLine(message);
                    Log(message);
                    EmergencyLog(message);
                    Console.ReadKey();
                    return;
                }

                Log($"Найдена DLL: {dllPath}");
                EmergencyLog($"Найдена DLL: {dllPath}");

                // Ищем процесс игры (может быть по разному названию в зависимости от платформы)
                Process gameProcess = null;

                // Стандартное название в Steam
                Process[] processes = Process.GetProcessesByName("GreyHack");
                if (processes.Length > 0)
                {
                    gameProcess = processes[0];
                }
                else
                {
                    // Альтернативное название без пробела
                    processes = Process.GetProcessesByName("Grey_Hack");
                    if (processes.Length > 0)
                    {
                        gameProcess = processes[0];
                    }
                    else
                    {
                        // Название с расширением (Windows 11)
                        processes = Process.GetProcessesByName("Grey Hack.exe");
                        if (processes.Length > 0)
                        {
                            gameProcess = processes[0];
                        }
                        else
                        {
                            // Название с пробелом (Windows 11)
                            processes = Process.GetProcessesByName("Grey Hack");
                            if (processes.Length > 0)
                            {
                                gameProcess = processes[0];
                            }
                            else
                            {
                                // Название в Unity Editor
                                processes = Process.GetProcessesByName("Unity");
                                foreach (var proc in processes)
                                {
                                    if (proc.MainWindowTitle.Contains("Grey Hack"))
                                    {
                                        gameProcess = proc;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

                if (gameProcess == null)
                {
                    Console.WriteLine("Grey Hack не запущен! Ожидание запуска игры...");
                    Log("Игра не запущена, ожидание...");
                    EmergencyLog("Игра не запущена, ожидание...");

                    // Ожидание запуска игры
                    bool gameFound = false;
                    for (int i = 0; i < 30; i++) // 30 секунд ожидания
                    {
                        Thread.Sleep(1000);

                        processes = Process.GetProcessesByName("GreyHack");
                        if (processes.Length > 0)
                        {
                            gameProcess = processes[0];
                            gameFound = true;
                            break;
                        }

                        processes = Process.GetProcessesByName("Grey_Hack");
                        if (processes.Length > 0)
                        {
                            gameProcess = processes[0];
                            gameFound = true;
                            break;
                        }

                        // Проверка на название в Windows 11
                        processes = Process.GetProcessesByName("Grey Hack.exe");
                        if (processes.Length > 0)
                        {
                            gameProcess = processes[0];
                            gameFound = true;
                            break;
                        }

                        // Проверка на название с пробелом
                        processes = Process.GetProcessesByName("Grey Hack");
                        if (processes.Length > 0)
                        {
                            gameProcess = processes[0];
                            gameFound = true;
                            break;
                        }

                        Console.Write(".");
                    }

                    if (!gameFound)
                    {
                        Console.WriteLine("\nИгра не запущена после 30 секунд ожидания. Выход.");
                        Log("Тайм-аут ожидания запуска игры");
                        EmergencyLog("Тайм-аут ожидания запуска игры");
                        Console.ReadKey();
                        return;
                    }
                }

                Console.WriteLine($"Найден процесс Grey Hack (PID: {gameProcess.Id})");
                Log($"Найден процесс игры с PID: {gameProcess.Id}");
                EmergencyLog($"Найден процесс игры с PID: {gameProcess.Id}, имя: {gameProcess.ProcessName}, путь: {gameProcess.MainModule?.FileName}");

                // Внедрение DLL
                IntPtr hProcess = OpenProcess(PROCESS_ALL_ACCESS, false, gameProcess.Id);
                if (hProcess == IntPtr.Zero)
                {
                    string message = "Не удалось получить доступ к процессу";
                    Console.WriteLine(message);
                    Log(message);
                    EmergencyLog(message);
                    Console.ReadKey();
                    return;
                }

                IntPtr loadLibraryAddr = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");
                if (loadLibraryAddr == IntPtr.Zero)
                {
                    string message = "Не удалось найти адрес LoadLibraryA";
                    Console.WriteLine(message);
                    Log(message);
                    EmergencyLog(message);
                    CloseHandle(hProcess);
                    Console.ReadKey();
                    return;
                }

                Log("Выделение памяти в процессе...");
                EmergencyLog("Выделение памяти в процессе...");
                IntPtr allocMemAddress = VirtualAllocEx(hProcess, IntPtr.Zero, (uint)dllPath.Length + 1, MEM_COMMIT, PAGE_READWRITE);
                if (allocMemAddress == IntPtr.Zero)
                {
                    string message = "Не удалось выделить память в процессе";
                    Console.WriteLine(message);
                    Log(message);
                    EmergencyLog(message);
                    CloseHandle(hProcess);
                    Console.ReadKey();
                    return;
                }

                Log("Запись пути к DLL в память процесса...");
                EmergencyLog("Запись пути к DLL в память процесса...");
                byte[] bytes = Encoding.ASCII.GetBytes(dllPath);
                UIntPtr bytesWritten;
                if (!WriteProcessMemory(hProcess, allocMemAddress, bytes, (uint)bytes.Length, out bytesWritten))
                {
                    string message = "Не удалось записать в память процесса";
                    Console.WriteLine(message);
                    Log(message);
                    EmergencyLog(message);
                    CloseHandle(hProcess);
                    Console.ReadKey();
                    return;
                }

                Log("Создание удаленного потока для загрузки DLL...");
                EmergencyLog("Создание удаленного потока для загрузки DLL...");
                IntPtr hThread = CreateRemoteThread(hProcess, IntPtr.Zero, 0, loadLibraryAddr, allocMemAddress, 0, IntPtr.Zero);
                if (hThread == IntPtr.Zero)
                {
                    string message = "Не удалось создать удаленный поток";
                    Console.WriteLine(message);
                    Log(message);
                    EmergencyLog(message);
                    CloseHandle(hProcess);
                    Console.ReadKey();
                    return;
                }

                string successMessage = "DLL успешно внедрена в игру!";
                Console.WriteLine(successMessage);
                Log(successMessage);
                EmergencyLog(successMessage);

                // Даём время на загрузку DLL
                Log("Ожидание загрузки DLL (2 секунды)...");
                EmergencyLog("Ожидание загрузки DLL (2 секунды)...");
                Thread.Sleep(2000);

                // Пытаемся вызвать метод Init из DLL
                try
                {
                    Log("Попытка вызвать метод Init из DLL...");
                    EmergencyLog("Попытка вызвать метод Init из DLL...");

                    // Можно попробовать получить указатель на загруженную DLL
                    // Но это требует использования специальных API, обычно проще просто создать отдельную DLL с экспортом

                    // Альтернативный подход: создать файл с флагом для DLL
                    string flagFilePath = Path.Combine(Path.GetDirectoryName(dllPath), "init_translator.flag");
                    File.WriteAllText(flagFilePath, DateTime.Now.ToString());
                    Log($"Создан файл флага: {flagFilePath}");
                    EmergencyLog($"Создан файл флага: {flagFilePath}");

                    // Для дополнительной диагностики записываем список модулей процесса
                    try
                    {
                        EmergencyLog("Список модулей процесса:");
                        foreach (ProcessModule module in gameProcess.Modules)
                        {
                            EmergencyLog($"  - {module.ModuleName} ({module.FileName})");
                        }
                    }
                    catch (Exception ex)
                    {
                        EmergencyLog($"Ошибка при получении списка модулей: {ex.Message}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при вызове Init: {ex.Message}");
                    Log($"Ошибка при вызове Init: {ex.Message}");
                    EmergencyLog($"Ошибка при вызове Init: {ex.Message}\n{ex.StackTrace}");
                }

                CloseHandle(hThread);
                CloseHandle(hProcess);

                Console.WriteLine("Нажмите любую клавишу для выхода...");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
                Log($"Критическая ошибка: {ex.Message}\n{ex.StackTrace}");
                EmergencyLog($"Критическая ошибка: {ex.Message}\n{ex.StackTrace}");
                Console.ReadKey();
            }
        }

        static void Log(string message)
        {
            try
            {
                File.AppendAllText(logPath, $"[{DateTime.Now}] {message}\n");
            }
            catch { }
        }

        // Добавляем метод для экстренного логирования на рабочий стол
        static void EmergencyLog(string message)
        {
            try
            {
                File.AppendAllText(Path.Combine(desktopPath, "greyhack_injector_debug.log"),
                    $"[{DateTime.Now}] {message}\n");
            }
            catch
            {
                // Если даже этот лог не работает, попробуем записать в текущую директорию
                try
                {
                    File.AppendAllText("emergency_debug.log",
                        $"[{DateTime.Now}] {message}\n");
                }
                catch { }
            }
        }
    }
}