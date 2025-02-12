using System;
using System.Reflection;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

class Program
{
    static string currentDirectory = Directory.GetCurrentDirectory();
    static string[] directoryParts = currentDirectory.Split(Path.DirectorySeparatorChar);
    static string currentLocation = directoryParts[directoryParts.Length - 1];

    static readonly List<string> builtInCommands = new() { "cd", "exit", "help" };

    static async Task Main()
    {
        Console.Title = "Glyph Shell";
        ShowWelcomeMessage();

        while (true)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"{currentLocation} : glyph> ");
            Console.ResetColor();

            string? input = ReadUserInputWithTabCompletion()?.Trim();
            if (string.IsNullOrEmpty(input)) continue;

            if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Goodbye! Thanks for using Glyph.");
                break;
            }

            await ExecuteCommand(input);
        }
    }

    static string ReadUserInputWithTabCompletion()
    {
        List<string> suggestions = new();
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
                suggestions = GetSuggestions(prefix);

                if (suggestions.Count == 1)
                {
                    input = input.Substring(0, input.Length - prefix.Length) + suggestions[0];
                    cursorPosition = input.Length;
                    RedrawInput(input, cursorPosition);
                }
                else if (suggestions.Count > 1)
                {
                    Console.WriteLine();
                    Console.WriteLine(string.Join("  ", suggestions));
                    Console.Write($"{currentLocation} : glyph> {input}");
                }
            }
            else if (key.Key == ConsoleKey.LeftArrow)
            {
                if (cursorPosition > 0) cursorPosition--;
            }
            else if (key.Key == ConsoleKey.RightArrow)
            {
                if (cursorPosition < input.Length) cursorPosition++;
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
        
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write($"{currentLocation} : glyph> ");
        
        Console.ResetColor();
        Console.Write(input + " ");
    
        Console.SetCursorPosition($"{currentLocation} : glyph> ".Length + cursorPosition, Console.CursorTop);
    }

    static List<string> GetSuggestions(string prefix)
    {
        var commandMatches = builtInCommands.Where(cmd => cmd.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList();
        var fileMatches = Directory.GetFileSystemEntries(currentDirectory)
            .Select(Path.GetFileName)
            .Where(name => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return commandMatches.Concat(fileMatches).ToList();
    }
    
    static void ListDirectoryContents()
    {
        try
        {
            var entries = Directory.GetFileSystemEntries(currentDirectory);
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
        string version = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version ?? "Unknown";
        
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
        else if (cmd == "ls")
        {
            ListDirectoryContents();
        }
        else
        {
            try
            {
                var process = new Process()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = Environment.OSVersion.Platform == PlatformID.Win32NT ? "cmd.exe" : "/bin/sh",
                        Arguments = Environment.OSVersion.Platform == PlatformID.Win32NT ? $"/C {command}" : $"-c \"{command}\"",
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
