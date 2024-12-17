using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length == 0 || args.Contains("--help"))
        {
            DisplayHelp();
            return;
        }

        string? operation = args.FirstOrDefault(arg => arg == "--create" || arg == "--validate");
        if (operation == null)
        {
            Console.WriteLine("Error: No valid operation specified. Use --create or --validate.");
            DisplayHelp();
            return;
        }

        bool recursive = args.Contains("--recursive");
        string directoryPath = args.FirstOrDefault(arg => !arg.StartsWith("--")) ?? Directory.GetCurrentDirectory();

        if (!Directory.Exists(directoryPath))
        {
            Console.WriteLine($"Error: Directory does not exist: {directoryPath}");
            return;
        }

        switch (operation)
        {
            case "--create":
                ProcessDirectories(directoryPath, CreateSHAFile, recursive);
                break;

            case "--validate":
                ProcessDirectories(directoryPath, ValidateDirectory, recursive);
                break;

            default:
                Console.WriteLine($"Error: Unknown operation '{operation}'");
                DisplayHelp();
                break;
        }
    }

    static void DisplayHelp()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  [PoolHash] --create [directoryPath] [--recursive]");
        Console.WriteLine("    Creates SHA-256 checksum files for .pol files in the specified directory.");
        Console.WriteLine("    If --recursive is used, processes all subdirectories.");
        Console.WriteLine();
        Console.WriteLine("  [PoolHash] --validate [directoryPath] [--recursive]");
        Console.WriteLine("    Validates .pol files in the specified directory using .sha files.");
        Console.WriteLine("    If --recursive is used, processes all subdirectories.");
        Console.WriteLine();
        Console.WriteLine("  [PoolHash] --help");
        Console.WriteLine("    Displays this help message.");
    }

    static void ProcessDirectories(string baseDirectory, Action<string> operation, bool recursive)
    {
        var directories = recursive
            ? Directory.GetDirectories(baseDirectory, "*", SearchOption.AllDirectories)
            : new[] { baseDirectory };

        foreach (var directory in directories)
        {
            Console.WriteLine($"Processing directory: {directory}");
            operation(directory);
        }
    }

    static void CreateSHAFile(string workingDirectory)
    {
        // Set the working directory for file operations
        Directory.SetCurrentDirectory(workingDirectory);

        // Get .pol files and sort them alphabetically
        string[] files = Directory.GetFiles(workingDirectory, "*.pol").OrderBy(Path.GetFileName).ToArray();
        if (files.Length == 0)
        {
            Console.WriteLine("No .pol files found in the directory.");
            return;
        }

        // Compute combined hash for the files
        string combinedHash = ComputeSHA256(files);

        // Determine the output file name and path
        string parentDirectory = Directory.GetParent(workingDirectory)?.Name ?? "root";
        string currentDirectory = new DirectoryInfo(workingDirectory).Name;
        string outputFileName = $"Pool_{parentDirectory}_{currentDirectory}.sha1";
        string outputFilePath = Path.Combine(workingDirectory, outputFileName);

        // Write the hash to the file
        File.WriteAllText(outputFilePath, combinedHash);
        Console.WriteLine($"SHA file created: {outputFilePath}");
    }

    static void ValidateDirectory(string workingDirectory)
    {
        // Set the working directory for file operations
        Directory.SetCurrentDirectory(workingDirectory);

        // Determine the expected SHA file name and path
        string parentDirectory = Directory.GetParent(workingDirectory)?.Name ?? "root";
        string currentDirectory = new DirectoryInfo(workingDirectory).Name;
        string shaFileName = $"Pool_{parentDirectory}_{currentDirectory}.sha1";
        string shaFilePath = Path.Combine(workingDirectory, shaFileName);

        if (!File.Exists(shaFilePath))
        {
            Console.WriteLine($"No .sha file found in the directory: {shaFilePath}");
            return;
        }

        // Get .pol files and sort them alphabetically
        string[] files = Directory.GetFiles(workingDirectory, "*.pol").OrderBy(Path.GetFileName).ToArray();
        if (files.Length == 0)
        {
            Console.WriteLine("No .pol files found in the directory.");
            return;
        }

        // Validate the computed hash against the saved hash
        string originalHash = File.ReadAllText(shaFilePath).Trim();
        string computedHash = ComputeSHA256(files);

        if (originalHash == computedHash)
        {
            Console.WriteLine("Directory is valid. No integrity issues found.");
        }
        else
        {
            Console.WriteLine("Integrity check failed! Directory contents have been altered.");
        }
    }

    static string ComputeSHA256(string[] filePaths)
    {
        using var sha256 = SHA256.Create();
        StringBuilder combinedHashBuilder = new();

        foreach (string file in filePaths)
        {
            byte[] fileBytes = File.ReadAllBytes(file);
            byte[] hashBytes = sha256.ComputeHash(fileBytes);
            combinedHashBuilder.Append(Convert.ToHexString(hashBytes));
        }

        byte[] finalHashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combinedHashBuilder.ToString()));
        return Convert.ToHexString(finalHashBytes);
    }
}
