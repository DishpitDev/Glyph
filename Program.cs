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
            Console.Title = "Glyph Shell";
            ShowWelcomeMessage();
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
                    Console.WriteLine("Goodbye! Thanks for using Glyph.");
                    Console.ResetColor();
                    break;
                }

                await ExecuteCommand(input);
            }
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
                case "help":
                    ShowHelp();
                    break;
                case "update":
                    await UpdateCommand();
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
                var process = new Process()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = command.Split(' ')[0],
                        Arguments = string.Join(" ", command.Split(' ').Skip(1)),
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
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
            Console.WriteLine("  exit           - Exit the shell.");
            Console.WriteLine("  help           - Display this help message.");
            Console.WriteLine("  update         - Update the shell to the latest version.");
        }

        static async Task UpdateCommand()
        {
            Console.WriteLine("Checking for updates...");

            try
            {
                string tempUpdateDirectory = Path.Combine(
                    Path.GetTempPath(),
                    UpdateDirectoryName
                );

                if (!Directory.Exists(tempUpdateDirectory))
                {
                    Directory.CreateDirectory(tempUpdateDirectory);
                }

                string latestReleaseInfo = await GetLatestReleaseInfo();
                if (string.IsNullOrEmpty(latestReleaseInfo))
                {
                    Console.WriteLine("Could not retrieve latest release information.");
                    return;
                }

                string assetUrl = ParseAssetUrl(latestReleaseInfo);
                if (string.IsNullOrEmpty(assetUrl))
                {
                    Console.WriteLine("Could not determine the asset URL for this platform.");
                    return;
                }

                string downloadedZipPath = Path.Combine(
                    tempUpdateDirectory,
                    "update.zip"
                );
                await DownloadFile(assetUrl, downloadedZipPath);

                string extractionPath = Path.Combine(
                    tempUpdateDirectory,
                    "extracted"
                );
                if (Directory.Exists(extractionPath))
                {
                    Directory.Delete(extractionPath, true);
                }

                ZipFile.ExtractToDirectory(downloadedZipPath, extractionPath);

                string executableName = Path.GetFileName(Assembly.GetExecutingAssembly().Location);
                string extractedExecutablePath = Path.Combine(
                    extractionPath,
                    executableName
                );

                if (!File.Exists(extractedExecutablePath))
                {
                    Console.WriteLine(
                        "The executable was not found in the extracted files."
                    );
                    return;
                }

                string currentExecutablePath = Assembly.GetExecutingAssembly().Location;
                string backupExecutablePath = Path.Combine(
                    tempUpdateDirectory,
                    "Glyph.old"
                );

                try
                {
                    File.Move(currentExecutablePath, backupExecutablePath, true);
                    File.Copy(extractedExecutablePath, currentExecutablePath, true);
                    Console.WriteLine("Successfully updated Glyph. Restarting...");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Update failed: {ex.Message}");
                    if (File.Exists(backupExecutablePath))
                    {
                        File.Move(backupExecutablePath, currentExecutablePath, true);
                    }

                    return;
                }
                finally
                {
                    try
                    {
                        Directory.Delete(tempUpdateDirectory, true);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Error cleaning up temp directory: {e.Message}");
                    }
                }

                Process.Start(currentExecutablePath);
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
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

        static async Task DownloadFile(string url, string destinationPath)
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
                        await contentStream.CopyToAsync(fileStream);
                    }
                }
            }
        }
    }
}
