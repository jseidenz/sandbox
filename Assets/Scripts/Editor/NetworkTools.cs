using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Diagnostics;

class NetworkTools : EditorWindow
{
    [MenuItem("Tools/Network Tools")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(NetworkTools));
    }

    void OnGUI()
    {
        var client_path = Path.GetFullPath("../SandboxClient");
        if (GUILayout.Button("Create Client"))
        {
            CreateClient(client_path);
        }      
    }

    void Update()
    {
        Repaint();
    }

    void CreateClient(string client_path)
    {
        var start_time = DateTime.Now;

        DeleteFolder(client_path);

        Directory.CreateDirectory(client_path);

        var directories_to_copy = new string[]
        {
            "Library",
            "Logs",
            "obj"
        };

        var directories_to_ignore = new string[]
        {
            ".vs",
            ".vscode",
            ".svn",
            "Logs",
            "Temp"
        };

        foreach(var file in Directory.GetFiles("."))
        {
            File.Copy(file, Path.Combine(client_path, file));
        }

        foreach(var directory in Directory.GetDirectories("."))
        {
            var source_dir_path = Path.GetFullPath(directory);

            bool ignore_directory = false;
            foreach (var ignored_directory in directories_to_ignore)
            {
                var ignored_directory_path = Path.GetFullPath(ignored_directory);
                if (source_dir_path == ignored_directory_path)
                {
                    ignore_directory = true;
                    break;
                }
            }

            if(ignore_directory)
            {
                continue;
            }

            bool should_copy = false;
            foreach(var copied_directory in directories_to_copy)
            {
                var copied_directory_path = Path.GetFullPath(copied_directory);
                if(source_dir_path == copied_directory_path)
                {
                    should_copy = true;
                    break;
                }
            }

            if(should_copy)
            {
                CopyFilesRecursively(directory, Path.Combine(client_path, directory));
            }
            else
            {
                CreateSymbolicLink(Path.Combine(client_path, directory), directory);
            }
        }

        System.Diagnostics.Process.Start(client_path);

        UnityEngine.Debug.Log($"Created client in {((DateTime.Now - start_time).TotalMilliseconds / 1000f).ToString("0.00")} seconds.");
    }

    public static void CopyFilesRecursively(string source_path, string target_path)
    {
        if(!Directory.Exists(target_path))
        {
            Directory.CreateDirectory(target_path);
        }

        CopyFilesRecursively(new DirectoryInfo(source_path), new DirectoryInfo(target_path));
    }

    static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target)
    {
        foreach (DirectoryInfo dir in source.GetDirectories())
            CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));
        foreach (FileInfo file in source.GetFiles())
            file.CopyTo(Path.Combine(target.FullName, file.Name));
    }

    static void CreateSymbolicLink(string link_path, string source_path)
    {
        var psi = new ProcessStartInfo("cmd.exe", " /C mklink /J \"" + link_path + "\" \"" + source_path + "\"");
        psi.CreateNoWindow = true;
        psi.UseShellExecute = false;
        Process.Start(psi).WaitForExit();
    }

    static void DeleteFolder(string path)
    {
        var psi = new ProcessStartInfo("cmd.exe", " /C rmdir /Q/S \"" + path + "\"");
        psi.CreateNoWindow = true;
        psi.UseShellExecute = false;
        Process.Start(psi).WaitForExit();
    }
}