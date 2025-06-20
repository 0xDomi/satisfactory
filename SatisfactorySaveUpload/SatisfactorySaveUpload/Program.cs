using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

class Program
{
    static string saveFolderPath;
    static string repoPath;
    static string repoSaveRelativePath = @"SnapshotLatest.sav";
    static string repoSaveFullPath;

    static void Main(string[] args)
    {
        Console.WriteLine("=== Savegame Auto-Uploader ===\n");

        Console.Write("Pfad zum Savegame-Ordner eingeben: ");
        saveFolderPath = Console.ReadLine().Trim('"');

        Console.Write("Pfad zum Git-Repository eingeben: ");
        repoPath = Console.ReadLine().Trim('"');

        repoSaveFullPath = Path.Combine(repoPath, repoSaveRelativePath);

        Console.WriteLine("\nStarte Savegame-Uploader. Alle 5 Minuten wird geprüft...");

        while (true)
        {
            try
            {
                ProcessLatestSave();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Verarbeiten: {ex.Message}");
            }

            Thread.Sleep(TimeSpan.FromMinutes(5));
        }
    }


    static void ProcessLatestSave()
    {
        if (!Directory.Exists(saveFolderPath))
        {
            Console.WriteLine($"Savegame-Pfad existiert nicht: {saveFolderPath}");
            return;
        }

        var latestSave = Directory.GetFiles(saveFolderPath, "*.sav")
                                  .Select(f => new FileInfo(f))
                                  .OrderByDescending(f => f.LastWriteTime)
                                  .FirstOrDefault();

        if (latestSave == null)
        {
            Console.WriteLine("Keine .sav-Dateien gefunden.");
            return;
        }

        DateTime? existingTimestamp = File.Exists(repoSaveFullPath)
            ? File.GetLastWriteTime(repoSaveFullPath)
            : null;

        if (existingTimestamp.HasValue && latestSave.LastWriteTime <= existingTimestamp.Value)
        {
            Console.WriteLine("Keine neuere Save-Datei gefunden.");
            return;
        }

        Console.WriteLine($"Neue Save-Datei gefunden: {latestSave.Name} ({latestSave.LastWriteTime})");

        // Datei kopieren und überschreiben
        Directory.CreateDirectory(Path.GetDirectoryName(repoSaveFullPath));
        File.Copy(latestSave.FullName, repoSaveFullPath, true);
        Console.WriteLine("Datei ins Repo kopiert.");

        // Git-Vorgänge
        RunGitCommand($"add \"{repoSaveRelativePath}\"");

        string commitMessage = $"Automatisches Save-Update: {latestSave.Name} ({latestSave.LastWriteTime:yyyy-MM-dd HH:mm:ss})";
        RunGitCommand($"commit -m \"{commitMessage}\"");

        RunGitCommand("push");

        Console.WriteLine("Savegame erfolgreich gepusht.");
    }

    static void RunGitCommand(string arguments)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo("git", arguments)
        {
            WorkingDirectory = repoPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (Process process = Process.Start(startInfo))
        {
            process.WaitForExit();

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            if (process.ExitCode != 0)
            {
                throw new Exception($"Git-Befehl '{arguments}' fehlgeschlagen:\n{error}");
            }

            Console.WriteLine(output.Trim());
        }
    }
}
