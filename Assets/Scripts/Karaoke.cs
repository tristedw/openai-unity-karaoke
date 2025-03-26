using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Lunatics;
using SFB;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Karaoke : MonoBehaviour
{
    public Button selectFileButton;
    public GameObject loadingCircle; // Reference to your loading circle game object

    private readonly string pythonScriptPath = Application.streamingAssetsPath + "/vocal-remover/inference.py";
    private readonly string savePath = Application.streamingAssetsPath + "/Separated/";

    private void Start()
    {
        selectFileButton.onClick.AddListener(SelectFileButtonClicked);
    }

    private void SelectFileButtonClicked()
    {
        var path = SelectFile();
        print(path);

        if (string.IsNullOrEmpty(path))
            return;

        // Show loading circle
        loadingCircle.SetActive(true);

        // Run the vocal separation and speech-to-text in separate threads
        Thread vocalSeparationThread = new Thread(() => DoVocalSeparation(path));
        vocalSeparationThread.Start();
    }

    private void DoVocalSeparation(string songPath)
    {
        ClearDirectory(savePath);

        var argsString = "--output_dir" + " " + savePath + " " + "--input" + " " + $"\"{songPath}\"";

        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = "python",
            Arguments = pythonScriptPath + " " + argsString,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = Application.streamingAssetsPath + "/vocal-remover/"
        };

        print(pythonScriptPath + " " + argsString);

        using (Process process = new Process { StartInfo = psi })
        {
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            print("Output: " + output);
            print("Error: " + error);
        }

        Thread speechToTextThread = new Thread(() => DoSpeechToText());
        speechToTextThread.Start();
    }

    private void DoSpeechToText()
    {
        string ffmpegPath = @"C:\ffmpeg\bin";
        string currentPath = Environment.GetEnvironmentVariable("PATH");
        Environment.SetEnvironmentVariable("PATH", currentPath + ";" + ffmpegPath);

        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = "python",
            Arguments = "speechtotext.py",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = Application.streamingAssetsPath
        };

        using (Process process = new Process { StartInfo = psi })
        {
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            print("Output: " + output);
            print("Error: " + error);
        }

        Environment.SetEnvironmentVariable("PATH", currentPath);

        MainThreadDispatcher.Enqueue(LoadingDone);
    }

    private void LoadingDone()
    {
        SceneManager.LoadScene("KaraokeTime");
    }

    private string SelectFile()
    {
        // Open file with filter
        var extensions = new[] {
            new ExtensionFilter("Sound Files", "mp3", "wav", "ogg" )
        };
        var paths = StandaloneFileBrowser.OpenFilePanel("Select Song", "", extensions, false);
        return paths.FirstOrDefault();
    }

    private void ClearDirectory(string directoryPath)
    {
        try
        {
            // Check if the directory exists
            if (Directory.Exists(directoryPath))
            {
                // Clear all files in the directory
                foreach (string file in Directory.GetFiles(directoryPath))
                {
                    File.Delete(file);
                }

                // Clear all subdirectories and their files
                foreach (string subdirectory in Directory.GetDirectories(directoryPath))
                {
                    Directory.Delete(subdirectory, true);
                }

                print("Directory cleared successfully: " + directoryPath);
            }
            else
            {
                print("Directory not found: " + directoryPath);
            }
        }
        catch (Exception ex)
        {
            print("Error clearing directory: " + ex.Message);
        }
    }
}