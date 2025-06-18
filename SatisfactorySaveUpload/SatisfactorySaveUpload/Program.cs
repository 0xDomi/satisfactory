using System;
using System.Diagnostics;
using System.IO;

class Program
{
    // Lokaler Pfad zu deinem GitHub Repo
    static string repoPath = @"D:\satisfactory\satisfactory";

    // Lokaler Pfad zur Savegame-Datei, die du hochladen willst
    static string saveFilePath = @"D:\satisfactory\SnapshotDomi.sav";

    // Pfad, wo im Repo die Datei liegen soll (relativ zu repoPath)
    static string repoSaveFilePath = Path.Combine(repoPath, @"CurrentSaveFile\SnapshotDomi.sav");

    static void Main(string[] args)
    {
        try
        {
            Console.WriteLine("Starte Upload Prozess...");

            // 1. Datei kopieren (überschreiben)
            File.Copy(saveFilePath, repoSaveFilePath, true);
            Console.WriteLine("Datei kopiert.");

            // 2. Git add
            RunGitCommand("add CurrentSaveFile/SnapshotDomi.sav");


            // 3. Git commit
            string commitMessage = $"Update save {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            RunGitCommand($"commit -m \"{commitMessage}\"");

            // 4. Git push
            RunGitCommand("push");

            Console.WriteLine("Upload abgeschlossen.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Fehler: " + ex.Message);
        }
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
                throw new Exception($"Git Befehl '{arguments}' schlug fehl:\n{error}");
            }

            Console.WriteLine(output);
        }
    }
}
