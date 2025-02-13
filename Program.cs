using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Glyph
{
    public class Glyph
    {
        static string _currentDirectory = Directory.GetCurrentDirectory();
        static string _currentLocation = string.Empty;
        static string _currentLocationLast = string.Empty;

        private const string GitHubRepoOwner = "DishpitDev";
        private const string GitHubRepoName = "Glyph";
        private const string UpdateDirectoryName = "GlyphUpdates";

        static async Task Main()
        {
            Directory.SetCurrentDirectory(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
            _currentDirectory = Directory.GetCurrentDirectory();
            
            Console.Title = "Glyph Shell";
            
            ShowWelcomeMessage();
            bool updateAvailable = await CheckForUpdates();
            (_currentLocation, _currentLocationLast) = FormatPath(_currentDirectory);

            while (true)
            {
                DrawTerminalBar();
                string input = ReadUserInputWithTabCompletion().Trim();
                if (string.IsNullOrEmpty(input))
                    continue;

                if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine("Thanks for using Glyph! <3");
                    Console.ResetColor();
                    break;
                }

                await ExecuteCommand(input);
            }
        }

        static async Task<bool> CheckForUpdates()
        {
            try
            {
                string latestReleaseInfo = await GetLatestReleaseInfo();
                if (string.IsNullOrEmpty(latestReleaseInfo))
                {
                    return false;
                }

                string latestVersion = ParseLatestVersion(latestReleaseInfo);
                string currentVersion =
                    Assembly
                        .GetExecutingAssembly()
                        .GetCustomAttribute<AssemblyFileVersionAttribute>()
                        ?.Version ??
                    "0.0.0";

                if (string.IsNullOrEmpty(latestVersion))
                {
                    return false;
                }

                if (IsNewerVersionAvailable(currentVersion, latestVersion))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(
                        $"Glyph v{latestVersion} is now available! " +
                        "Type 'update' to update."
                    );
                    Console.ResetColor();
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }
        
        static string ParseLatestVersion(string releaseInfo)
        {
            try
            {
                dynamic release = Newtonsoft.Json.JsonConvert.DeserializeObject(releaseInfo);
                return release.tag_name.ToString().TrimStart('v');
            }
            catch
            {
                return string.Empty;
            }
        }

        static bool IsNewerVersionAvailable(string currentVersion, string latestVersion)
        {
            Version current = new Version(currentVersion);
            Version latest = new Version(latestVersion);
            return latest > current;
        }

        static void DrawTerminalBar()
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(_currentLocation);
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write(_currentLocationLast);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(" : glyph> ");
            Console.ResetColor();
        }

        static (string formattedPath, string lastFolder) FormatPath(string path)
        {
            string homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string formattedPathResult = path;
            string lastFolderResult = "";

            if (string.IsNullOrEmpty(homePath))
            {
                string[] directoryParts =
                    path.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
                if (directoryParts.Length > 0)
                {
                    lastFolderResult = directoryParts.Last();
                    formattedPathResult = string.Join(Path.DirectorySeparatorChar.ToString(),
                        directoryParts.Take(directoryParts.Length - 1));
                    if (formattedPathResult.Length > 0)
                        formattedPathResult += Path.DirectorySeparatorChar;
                }

                return (formattedPathResult, lastFolderResult);
            }

            string normalizedPath = path.Replace(Path.DirectorySeparatorChar, '/');
            string normalizedHomePath = homePath.Replace(Path.DirectorySeparatorChar, '/');

            string[] pathParts = normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (pathParts.Length > 0)
            {
                lastFolderResult = pathParts.Last();
                string pathToFormat = "/" + string.Join("/", pathParts.Take(pathParts.Length - 1));

                if (pathToFormat.Length > 0 && !pathToFormat.EndsWith("/"))
                    pathToFormat += "/";

                if (pathToFormat.StartsWith(normalizedHomePath, StringComparison.OrdinalIgnoreCase))
                {
                    pathToFormat = pathToFormat[normalizedHomePath.Length..];
                    formattedPathResult = "~" + pathToFormat;
                }
                else
                {
                    formattedPathResult = pathToFormat.Replace('/', Path.DirectorySeparatorChar);
                }
            }
            else
            {
                formattedPathResult = "";
                lastFolderResult = path;
            }

            if (string.IsNullOrEmpty(formattedPathResult) && pathParts.Length > 0)
            {
                formattedPathResult = "";
            }

            return (formattedPathResult, lastFolderResult);
        }

        static void UpdateCurrentLocation()
        {
            _currentDirectory = Directory.GetCurrentDirectory();
            (_currentLocation, _currentLocationLast) = FormatPath(_currentDirectory);
        }

        static string ReadUserInputWithTabCompletion()
        {
            string input = "";
            int cursorPosition = 0;

            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(intercept: true);

                if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    return input;
                }
                else if (key.Key == ConsoleKey.Backspace)
                {
                    if (cursorPosition > 0)
                    {
                        input = input.Remove(cursorPosition - 1, 1);
                        cursorPosition--;
                        RedrawInput(input, cursorPosition);
                    }
                }
                else if (key.Key == ConsoleKey.Tab)
                {
                    string prefix = input.Split(' ').LastOrDefault() ?? "";
                    List<string> suggestions = GetSuggestions(prefix);

                    if (suggestions.Count == 1)
                    {
                        input = input.Substring(0, input.Length - prefix.Length) +
                                suggestions[0];
                        cursorPosition = input.Length;
                        RedrawInput(input, cursorPosition);
                    }
                    else if (suggestions.Count > 1)
                    {
                        Console.WriteLine();
                        Console.WriteLine(string.Join("  ", suggestions));
                        DrawTerminalBar();
                    }
                }
                else if (key.Key == ConsoleKey.LeftArrow)
                {
                    if (cursorPosition > 0)
                        cursorPosition--;
                }
                else if (key.Key == ConsoleKey.RightArrow)
                {
                    if (cursorPosition < input.Length)
                        cursorPosition++;
                }
                else
                {
                    input = input.Insert(cursorPosition, key.KeyChar.ToString());
                    cursorPosition++;
                    RedrawInput(input, cursorPosition);
                }
            }
        }

        static void RedrawInput(string input, int cursorPosition)
        {
            Console.SetCursorPosition(0, Console.CursorTop);

            DrawTerminalBar();

            Console.ResetColor();
            Console.Write(input + " ");

            Console.SetCursorPosition($"{_currentLocation}{_currentLocationLast} : glyph> ".Length + cursorPosition,
                Console.CursorTop);
        }

        static List<string> GetSuggestions(string prefix)
        {
            List<string> fileMatches = Directory.GetFileSystemEntries(_currentDirectory)
                .Select(Path.GetFileName)
                .Where(name => name != null && name.StartsWith(prefix,
                    StringComparison.OrdinalIgnoreCase))
                .Select(name => name!)
                .ToList();

            List<string> localExecutables = Directory.GetFiles(_currentDirectory)
                .Where(file => IsExecutable(file))
                .Select(Path.GetFileNameWithoutExtension)
                .Where(name => name != null && name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .Select(name => name!)
                .ToList();

            return fileMatches
                .Concat(localExecutables)
                .Distinct()
                .ToList();
        }

        private static bool IsExecutable(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLower();
            return extension == ".exe" || extension == ".com" || extension == ".bat" ||
                   extension == ".ps1";
        }

        static void ListDirectoryContents()
        {
            try
            {
                var entries = Directory.GetFileSystemEntries(_currentDirectory);
                foreach (var entry in entries)
                {
                    bool isDirectory = Directory.Exists(entry);
                    string name = Path.GetFileName(entry);

                    if (isDirectory)
                    {
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.Write(name + "/  ");
                    }
                    else
                    {
                        Console.ResetColor();
                        Console.Write(name + "  ");
                    }
                }

                Console.WriteLine();
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: {ex.Message}");
                Console.ResetColor();
            }
        }

        static void ShowWelcomeMessage()
        {
            string version =
                Assembly.GetExecutingAssembly()
                    .GetCustomAttribute<AssemblyFileVersionAttribute>()
                    ?.Version ??
                "Unknown";

            string currentTime = DateTime.Now.ToString("MMMM dd, yyyy HH:mm:ss");

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(@$"
     ░▒▓██████▓▒░░▒▓█▓▒░   ░▒▓█▓▒░░▒▓█▓▒░▒▓███████▓▒░░▒▓█▓▒░░▒▓█▓▒░ 
    ░▒▓█▓▒░░▒▓█▓▒░▒▓█▓▒░   ░▒▓█▓▒░░▒▓█▓▒░▒▓█▓▒░░▒▓█▓▒░▒▓█▓▒░░▒▓█▓▒░ 
    ░▒▓█▓▒░      ░▒▓█▓▒░   ░▒▓█▓▒░░▒▓█▓▒░▒▓█▓▒░░▒▓█▓▒░▒▓█▓▒░░▒▓█▓▒░ 
    ░▒▓█▓▒▒▓███▓▒░▒▓█▓▒░    ░▒▓██████▓▒░░▒▓███████▓▒░░▒▓████████▓▒░ 
    ░▒▓█▓▒░░▒▓█▓▒░▒▓█▓▒░      ░▒▓█▓▒░   ░▒▓█▓▒░      ░▒▓█▓▒░░▒▓█▓▒░ 
    ░▒▓█▓▒░░▒▓█▓▒░▒▓█▓▒░      ░▒▓█▓▒░   ░▒▓█▓▒░      ░▒▓█▓▒░░▒▓█▓▒░ 
     ░▒▓██████▓▒░░▒▓████████▓▒░▒▓█▓▒░   ░▒▓█▓▒░      ░▒▓█▓▒░░▒▓█▓▒░ 

      Glyph Shell - v{version} - by DishpitDev  
      Current time: {currentTime}
      Open-source and built for power. Type 'exit' to quit.
      ");
            Console.ResetColor();
        }

        static async Task ExecuteCommand(string command)
        {
            string[] parts = command.Split(' ', 2);
            string cmd = parts[0];

            switch (cmd.ToLower())
            {
                case "cd":
                    HandleCdCommand(parts);
                    break;
                case "ls":
                    ListDirectoryContents();
                    break;
                case "cpu":
                    ShowCpuUsage();
                    break;
                case "mem":
                    ShowMemoryUsage();
                    break;
                case "disk":
                    ShowDiskUsage();
                    break;
                case "help":
                    ShowHelp();
                    break;
                case "glyph":
                    if (parts[1] == "update")
                    {
                        await UpdateCommand();
                    }
                    break;
                default:
                    await ExecuteExternalCommand(command);
                    break;
            }
        }

        static async Task ExecuteExternalCommand(string command)
        {
            try
            {
                string[] parts = command.Split(' ', 2);
                string executableName = parts[0];
                string arguments = parts.Length > 1 ? parts[1] : "";
                
                string? executablePath = FindExecutableInPath(executableName);

                if (executablePath == null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Command not found: {executableName}");
                    Console.ResetColor();
                    return;
                }

                var process = new Process()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = executablePath,
                        Arguments = arguments,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = false,
                        WorkingDirectory = _currentDirectory
                    }
                };

                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (!string.IsNullOrWhiteSpace(output))
                    Console.WriteLine(output);
                if (!string.IsNullOrWhiteSpace(error))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(error);
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error executing command: {ex.Message}");
                Console.ResetColor();
            }
        }
        
        static string? FindExecutableInPath(string executableName)
        {
            string? pathEnv = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrEmpty(pathEnv))
            {
                return null;
            }

            string[] paths = pathEnv.Split(Path.PathSeparator);

            foreach (string path in paths)
            {
                string fullPath = Path.Combine(path, executableName);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    string fullPathWithExe = fullPath + ".exe";
                    if (File.Exists(fullPathWithExe))
                    {
                        return fullPathWithExe;
                    }
                }
            }

            return null;
        }


        static void ShowCpuUsage()
        {
            try
            {
                var process = Process.GetCurrentProcess();
                var startTime = DateTime.UtcNow;
                var startCpuUsage = process.TotalProcessorTime;

                Thread.Sleep(500);

                var endTime = DateTime.UtcNow;
                var endCpuUsage = process.TotalProcessorTime;

                double cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
                double timePassedMs = (endTime - startTime).TotalMilliseconds;
                int cpuCores = Environment.ProcessorCount;
                double cpuUsagePercentage = (cpuUsedMs / (timePassedMs * cpuCores)) * 100;

                Console.WriteLine($"CPU Usage: {cpuUsagePercentage:F2}%");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error fetching CPU usage: {ex.Message}");
                Console.ResetColor();
            }
        }
        
        static void ShowMemoryUsage()
        {
            try
            {
                var memoryInfo = GC.GetGCMemoryInfo();
                long totalMemory = memoryInfo.TotalAvailableMemoryBytes;
                long usedMemory = GC.GetTotalMemory(false);
                long freeMemory = totalMemory - usedMemory;

                Console.WriteLine("Memory Usage:");
                Console.WriteLine($"  Total: {totalMemory / (1024 * 1024)} MB");
                Console.WriteLine($"  Used:  {usedMemory / (1024 * 1024)} MB");
                Console.WriteLine($"  Free:  {freeMemory / (1024 * 1024)} MB");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error fetching memory usage: {ex.Message}");
                Console.ResetColor();
            }
        }
        
        static void ShowDiskUsage()
        {
            try
            {
                DriveInfo drive = DriveInfo.GetDrives().FirstOrDefault(d => d.IsReady);
                if (drive != null)
                {
                    long totalSpace = drive.TotalSize;
                    long freeSpace = drive.TotalFreeSpace;
                    long usedSpace = totalSpace - freeSpace;

                    Console.WriteLine($"Disk Usage ({drive.Name}):");
                    Console.WriteLine($"  Total: {totalSpace / (1024 * 1024 * 1024)} GB");
                    Console.WriteLine($"  Used:  {usedSpace / (1024 * 1024 * 1024)} GB");
                    Console.WriteLine($"  Free:  {freeSpace / (1024 * 1024 * 1024)} GB");
                }
                else
                {
                    Console.WriteLine("No available drives detected.");
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error fetching disk usage: {ex.Message}");
                Console.ResetColor();
            }
        }

        static void HandleCdCommand(string[] parts)
        {
            if (parts.Length > 1)
            {
                string path = parts[1];
                try
                {
                    string newPath = Path.GetFullPath(Path.Combine(_currentDirectory, path));
                    if (Directory.Exists(newPath))
                    {
                        Directory.SetCurrentDirectory(newPath);
                        UpdateCurrentLocation();
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Directory not found: {path}");
                        Console.ResetColor();
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error: {ex.Message}");
                    Console.ResetColor();
                }
            }
            else
            {
                Console.WriteLine("Usage: cd <directory>");
            }
        }

        static void ShowHelp()
        {
            Console.WriteLine("Available commands:");
            Console.WriteLine("  cd <directory> - Change the current directory.");
            Console.WriteLine("  ls             - List the contents of the current directory.");
            Console.WriteLine("  cpu            - Show CPU usage.");
            Console.WriteLine("  mem            - Show available memory.");
            Console.WriteLine("  disk           - Show disk space usage.");
            Console.WriteLine("  exit           - Exit the shell.");
            Console.WriteLine("  help           - Display this help message.");
            Console.WriteLine("  update         - Update the shell to the latest version.");
        }

        static async Task UpdateCommand()
        {
            Console.WriteLine("Checking for updates...");
            string logFilePath = Path.Combine(
                Path.GetTempPath(),
                "GlyphUpdateLog.txt"
            );

            try
            {
                Log($"Starting update process...", logFilePath);

                string tempUpdateDirectory = Path.Combine(
                    Path.GetTempPath(),
                    UpdateDirectoryName
                );

                if (!Directory.Exists(tempUpdateDirectory))
                {
                    Log($"Creating directory: {tempUpdateDirectory}", logFilePath);
                    Directory.CreateDirectory(tempUpdateDirectory);
                }

                string latestReleaseInfo = await GetLatestReleaseInfo();
                if (string.IsNullOrEmpty(latestReleaseInfo))
                {
                    Log(
                        "Could not retrieve latest release information.",
                        logFilePath
                    );
                    Console.WriteLine("Could not retrieve latest release information.");
                    return;
                }

                string assetUrl = ParseAssetUrl(latestReleaseInfo);
                if (string.IsNullOrEmpty(assetUrl))
                {
                    Log(
                        "Could not determine the asset URL for this platform.",
                        logFilePath
                    );
                    Console.WriteLine("Could not determine the asset URL for this platform.");
                    return;
                }

                string downloadedZipPath = Path.Combine(
                    tempUpdateDirectory,
                    "update.zip"
                );
                await DownloadFile(assetUrl, downloadedZipPath, logFilePath);

                string extractionPath = Path.Combine(
                    tempUpdateDirectory,
                    "extracted"
                );
                if (Directory.Exists(extractionPath))
                {
                    Log($"Deleting existing extraction path: {extractionPath}", logFilePath);
                    Directory.Delete(extractionPath, true);
                }

                Log($"Extracting to: {extractionPath}", logFilePath);
                ZipFile.ExtractToDirectory(downloadedZipPath, extractionPath);

                string executableName = Process.GetCurrentProcess().ProcessName;

                string platformSubdirectory = "";
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    platformSubdirectory = "publish-win";
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    platformSubdirectory = "publish-linux";
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    platformSubdirectory = "publish-osx";
                }
                else
                {
                    Log("Unsupported platform.", logFilePath);
                    Console.WriteLine("Unsupported platform.");
                    return;
                }

                string executableExtension = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : "";
                string extractedExecutablePath = Path.Combine(
                    extractionPath,
                    platformSubdirectory,
                    executableName + executableExtension
                );

                if (!File.Exists(extractedExecutablePath))
                {
                    Log(
                        $"The executable was not found in the extracted files: {extractedExecutablePath}",
                        logFilePath
                    );
                    Console.WriteLine(
                        "The executable was not found in the extracted files."
                    );
                    return;
                }
                
                string currentExecutablePath = Process.GetCurrentProcess().MainModule.FileName;
                string currentExecutableExtension = Path.GetExtension(currentExecutablePath);
                string backupExecutablePath = Path.Combine(
                    tempUpdateDirectory,
                    "Glyph.old" + currentExecutableExtension
                );

                try
                {
                    Log(
                        $"Moving {currentExecutablePath} to {backupExecutablePath}",
                        logFilePath
                    );
                    File.Move(currentExecutablePath, backupExecutablePath, true);
                    Log(
                        $"Copying {extractedExecutablePath} to {currentExecutablePath}",
                        logFilePath
                    );
                    File.Copy(extractedExecutablePath, currentExecutablePath, true);
                    Console.WriteLine("Successfully updated Glyph. Restarting...");
                    Log("Successfully updated Glyph. Restarting...", logFilePath);
                    
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = currentExecutablePath,
                        UseShellExecute = false,
                        CreateNoWindow = false
                    };

                    Process.Start(startInfo);
                    Environment.Exit(0);
                }
                catch (Exception ex)
                {
                    Log($"Update failed: {ex.Message}", logFilePath);
                    Console.WriteLine($"Update failed: {ex.Message}");
                    if (File.Exists(backupExecutablePath))
                    {
                        Log(
                            $"Restoring backup {backupExecutablePath} to {currentExecutablePath}",
                            logFilePath
                        );
                        File.Move(backupExecutablePath, currentExecutablePath, true);
                    }

                    return;
                }
                finally
                {
                    try
                    {
                        Log($"Cleaning up temp directory: {tempUpdateDirectory}", logFilePath);
                        Directory.Delete(tempUpdateDirectory, true);
                    }
                    catch (Exception e)
                    {
                        Log(
                            $"Error cleaning up temp directory: {e.Message}",
                            logFilePath
                        );
                        Console.WriteLine($"Error cleaning up temp directory: {e.Message}");
                    }
                }

                Process.Start(currentExecutablePath);
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                Log($"Update failed: {ex.Message}", logFilePath);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Update failed: {ex.Message}");
                Console.ResetColor();
            }
        }


        static async Task<string> GetLatestReleaseInfo()
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.UserAgent.Add(
                    new System.Net.Http.Headers.ProductInfoHeaderValue(
                        "Glyph",
                        Assembly
                            .GetExecutingAssembly()
                            .GetCustomAttribute<AssemblyFileVersionAttribute>()
                            ?.Version ?? "1.0"
                    )
                );
                string url =
                    $"https://api.github.com/repos/{GitHubRepoOwner}/{GitHubRepoName}/releases/latest";
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
        }

        static string ParseAssetUrl(string releaseInfo)
        {
            try
            {
                dynamic release = Newtonsoft.Json.JsonConvert.DeserializeObject(releaseInfo);
                string platform;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    platform = "win";
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    platform = "linux";
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    platform = "osx";
                }
                else
                {
                    Console.WriteLine("Unsupported platform.");
                    return null;
                }

                foreach (var asset in release.assets)
                {
                    string assetName = asset.name.ToString().ToLower();
                    if (assetName.Contains(platform) && assetName.EndsWith(".zip"))
                    {
                        return asset.browser_download_url.ToString();
                    }
                }

                Console.WriteLine(
                    "No suitable asset found for the current platform."
                );
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing release info: {ex.Message}");
                return null;
            }
        }

        static async Task DownloadFile(
            string url,
            string destinationPath,
            string logFilePath
        )
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.UserAgent.Add(
                        new System.Net.Http.Headers.ProductInfoHeaderValue(
                            "Glyph",
                            Assembly
                                .GetExecutingAssembly()
                                .GetCustomAttribute<AssemblyFileVersionAttribute>()
                                ?.Version ?? "1.0"
                        )
                    );
                    using (
                        HttpResponseMessage response = await client.GetAsync(
                            url,
                            HttpCompletionOption.ResponseHeadersRead
                        )
                    )
                    {
                        response.EnsureSuccessStatusCode();
                        long? totalBytes = response.Content.Headers.ContentLength;

                        using (
                            Stream contentStream = await response.Content.ReadAsStreamAsync()
                        )
                        using (
                            FileStream fileStream = new FileStream(
                                destinationPath,
                                FileMode.Create,
                                FileAccess.Write,
                                FileShare.None,
                                8192,
                                true
                            )
                        )
                        {
                            var totalRead = 0L;
                            var buffer = new byte[8192];
                            var isMoreToRead = true;

                            do
                            {
                                var read = await contentStream.ReadAsync(
                                    buffer,
                                    0,
                                    buffer.Length
                                );
                                if (read == 0)
                                {
                                    isMoreToRead = false;
                                }
                                else
                                {
                                    await fileStream.WriteAsync(buffer, 0, read);

                                    totalRead += read;

                                    if (totalBytes.HasValue)
                                    {
                                        var percentage =
                                            (double)totalRead / totalBytes.Value * 100;
                                        DrawProgressBar(percentage);
                                    }
                                }
                            } while (isMoreToRead);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Download failed: {ex.Message}", logFilePath);
                throw;
            }
        }

        static void DrawProgressBar(double percentage)
        {
            Console.CursorVisible = false;
            int barSize = 50;
            int fillSize = (int)(barSize * percentage / 100);
            string fill = new string('=', fillSize);
            string space = new string(' ', barSize - fillSize);

            Console.Write($"\r[{fill}{space}] {percentage:F2}%");
            Console.CursorVisible = true;
        }

        static void Log(string message, string logFilePath)
        {
            try
            {
                string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}";
                File.AppendAllText(logFilePath, logMessage + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing to log file: {ex.Message}");
            }
        }
    }
}