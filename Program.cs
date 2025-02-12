using System;
using System.Reflection;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;

class Program
{
    static string currentDirectory = Directory.GetCurrentDirectory();
    static string[] directoryParts = currentDirectory.Split(Path.DirectorySeparatorChar);
    static string currentLocation = directoryParts[directoryParts.Length - 1];
    
    static async Task Main()
    {
        Console.Title = "Glyph Shell";
        ShowWelcomeMessage();
        
        while (true)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"{currentLocation} : glyph> ");
            Console.ResetColor();

            string? input = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(input)) continue;

            if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Goodbye! Thanks for using Glyph.");
                break;
            }

            await ExecuteCommand(input);
        }
    }

    static void ShowWelcomeMessage()
    {
        string version = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyFileVersionAttribute>().Version;
        
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
        string cmd = parts[0].ToLower();

        if (cmd == "cd")
        {
            if (parts.Length > 1)
            {
                string path = parts[1];
                try
                {
                    string newPath = Path.GetFullPath(Path.Combine(currentDirectory, path));
                    if (Directory.Exists(newPath))
                    {
                        currentDirectory = newPath;
                        Directory.SetCurrentDirectory(currentDirectory);
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
        else
        {
            try
            {
                bool isWindows = Environment.OSVersion.Platform == PlatformID.Win32NT;
                var process = new Process()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = isWindows ? "cmd.exe" : "/bin/sh",
                        Arguments = isWindows ? $"/C {command}" : $"-c \"{command}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WorkingDirectory = currentDirectory
                    }
                };

                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync();

                if (!string.IsNullOrWhiteSpace(output)) Console.WriteLine(output);
                if (!string.IsNullOrWhiteSpace(error))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(error);
                }
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: {ex.Message}");
                Console.ResetColor();
            }
        }
    }
    
}
