using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class PostBuildProcessor : IPostprocessBuildWithReport
{
    public void OnPostprocessBuild(BuildReport report)
    {
        
        Debug.Log(report.summary.outputPath);
        
        // Define the source and destination directories
        string sourceDirectory = Path.GetDirectoryName(report.summary.outputPath);
        string destinationDirectory = "D:\\_Unity\\2023\\_Cargo TD on Rails\\SteamContentBuilder\\content";

        CopyFilesRecursively(sourceDirectory, destinationDirectory);
        
        // Log a message to indicate the process is complete
        Debug.Log("Build copied to: " + destinationDirectory);
        
        // open folder to steam upload thing
        System.Diagnostics.Process.Start(@"D:\\_Unity\\2023\\_Cargo TD on Rails\\SteamContentBuilder\\");
    }
    
    private static void CopyFilesRecursively(string sourcePath, string targetPath)
    {
        //Delete old stuff
        var dir = new DirectoryInfo(targetPath);
        dir.Delete(true);
        
        //Now Create all of the directories
        foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
        }

        //Copy all the files & Replaces any files with the same name
        foreach (string newPath in Directory.GetFiles(sourcePath, "*.*",SearchOption.AllDirectories))
        {
            File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
        }
    }

    public int callbackOrder { get; }
}
