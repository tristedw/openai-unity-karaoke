using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using static UnityEngine.EventSystems.EventTrigger;

public class KaraokeGameManager : MonoBehaviour
{
    private readonly string lyricsPath = Application.streamingAssetsPath + "/Separated/lyrics.txt";
    private readonly string sentencesPath = Application.streamingAssetsPath + "/Separated/sentences.txt";
    private readonly string audioPath = Application.streamingAssetsPath + "/Separated/";

    public AudioSource instrumentSource;
    public AudioSource vocalSource;

    public TMP_Text lyricsText;

    private AudioClip vocals;
    private AudioClip instruments;

    private Dictionary<string, List<Tuple<float, float>>> lyricsDictionary = new Dictionary<string, List<Tuple<float, float>>>();
    private Dictionary<string, List<Tuple<float, float>>> sentencesDictionary = new Dictionary<string, List<Tuple<float, float>>>();

    private string lastSentence;

    private string currentLyricsShown;

    private float startTimer;
    private bool isInit = false;
    private Tuple<float, float> lastPairValue;
    private string lastLyric;

    private IEnumerator Start()
    {
        if (!File.Exists(lyricsPath))
        {
            print("NO LYRICS FOUND!!!");
            SceneManager.LoadScene("LoadScene");
            yield break;
        }

        if (!File.Exists(sentencesPath))
        {
            print("NO SENTENCES FOUND!!!");
            SceneManager.LoadScene("LoadScene");
            yield break;
        }

        LoadLyrics();
        LoadSentences();

        //load clips
        foreach (var file in Directory.GetFiles(audioPath))
        {
            if (file.EndsWith(".meta"))
            { continue; }

            print(file);
            if (file.Contains("_Vocals"))
            {
                yield return LoadAudioClip(file, clip => vocals = clip);
                vocalSource.clip = vocals;
            }
            else if (file.Contains("_Instruments"))
            {
                yield return LoadAudioClip(file, clip => instruments = clip);
                instrumentSource.clip = instruments;
            }
        }

        StartKaraoke();
    }

    private void StartKaraoke()
    {
        vocalSource.Play();
        instrumentSource.Play();
        isInit = true;
    }

    private void LoadSentences()
    {
        var lines = File.ReadAllLines(sentencesPath);

        foreach (string line in lines)
        {
            // Find the first space to extract the start time
            int firstSpaceIndex = line.IndexOf(' ');
            if (firstSpaceIndex > 0 && float.TryParse(line.Substring(0, firstSpaceIndex), out float startTime))
            {
                // Find the last space to extract the end time
                int lastSpaceIndex = line.LastIndexOf(' ');
                if (lastSpaceIndex > firstSpaceIndex)
                {
                    if (float.TryParse(line.Substring(lastSpaceIndex), out float endTime))
                    {
                        // Extract the sentence between start and end times
                        string sentence = line.Substring(firstSpaceIndex + 1, lastSpaceIndex - firstSpaceIndex - 1);

                        // Process the lyric by adding it to the dictionary
                        if (!sentencesDictionary.ContainsKey(sentence))
                        {
                            // Create a new list for the lyric if it doesn't exist
                            sentencesDictionary.Add(sentence, new List<Tuple<float, float>>());
                        }

                        // Add the time range to the dictionary
                        List<Tuple<float, float>> timeRanges = sentencesDictionary[sentence];
                        timeRanges.Add(Tuple.Create(startTime, endTime));
                    }
                }
            }
        }
    }

    private void LoadLyrics()
    {
        var lines = File.ReadAllLines(lyricsPath);

        string currentLyric = null;

        float startTime = 0.0f;

        foreach (string line in lines)
        {
            var words = line.Split(" ");
            foreach (var word in words)
            {
                // Try parsing the word as a float to determine if it's a time value
                if (float.TryParse(word, out float timeValue))
                {
                    // If it's a time value, update the end time for the current lyric
                    if (!string.IsNullOrEmpty(currentLyric) && lyricsDictionary.ContainsKey(currentLyric))
                    {
                        List<Tuple<float, float>> timeRanges = lyricsDictionary[currentLyric];
                        timeRanges.Add(Tuple.Create(startTime, timeValue));
                    }

                    // Update the start time for the next lyric
                    startTime = timeValue;
                }
                else
                {
                    // If it's not a time value, it's a lyric
                    currentLyric = word;

                    // Check if the key already exists in the dictionary
                    if (!lyricsDictionary.ContainsKey(currentLyric))
                    {
                        // Create a new list for the lyric if it doesn't exist
                        lyricsDictionary.Add(currentLyric, new List<Tuple<float, float>>());
                    }
                }
            }
        }


        startTimer = Time.time + lyricsDictionary.First().Value.First().Item2;
    }

    private void Update()
    {
        if (!vocalSource.isPlaying)
            return;

        if (Time.time < startTimer)
            return;

        if (!isInit)
            return;

        var closestSentence = GetClosestSentence(vocalSource.time);

        if (closestSentence != lastSentence)
        {
            lyricsText.text = closestSentence.TrimStart();
            currentLyricsShown = string.Empty;
        }

        var closestLyric = GetClosestLyric(vocalSource.time);

        if (!string.IsNullOrEmpty(closestLyric) && closestSentence.Contains(closestLyric) && lastLyric != closestLyric)
        {
            StringBuilder sb = new StringBuilder(currentLyricsShown);
            sb.Append(closestLyric + " ");

            currentLyricsShown = sb.ToString();

            if (currentLyricsShown.Length > 1)
            {
                StringBuilder sb1 = new StringBuilder(lyricsText.text);

                sb1.Replace("<color=white>", "");
                sb1.Insert(0, "<color=white>");
                sb1.Replace("</color>", "");
                sb1.Insert(Math.Min(currentLyricsShown.Length + 12, sb1.Length), "</color>");
                lyricsText.text = sb1.ToString();
            }
        }

        lastSentence = closestSentence;
        lastLyric = closestLyric;
    }

    string GetClosestLyric(float targetTime)
    {
        string closestLyric = string.Empty;
        float minDifference = float.MaxValue;
        float threshold = 1.0f;

        foreach (var entry in lyricsDictionary)
        {
            foreach (var tuple in entry.Value)
            {
                float startTime = tuple.Item1;
                float endTime = tuple.Item2;

                if (targetTime >= startTime && targetTime <= endTime)
                {
                    float difference = Math.Min(Math.Abs(startTime - targetTime), Math.Abs(endTime - targetTime));

                    if (difference < minDifference)
                    {
                        minDifference = difference;
                        closestLyric = entry.Key;
                    }
                }
            }
        }

        // Check if the closest lyric is within the threshold
        return minDifference <= threshold ? closestLyric : string.Empty;
    }

    string GetClosestSentence(float targetTime)
    {
        string closestLyric = string.Empty;
        float minDifference = float.MaxValue;
        float threshold = 20.0f;

        foreach (var entry in sentencesDictionary)
        {
            foreach (var tuple in entry.Value)
            {
                float startTime = tuple.Item1;
                float endTime = tuple.Item2;

                if (targetTime >= startTime && targetTime <= endTime)
                {
                    float difference = Math.Min(Math.Abs(startTime - targetTime), Math.Abs(endTime - targetTime));

                    if (difference < minDifference)
                    {
                        minDifference = difference;
                        closestLyric = entry.Key;
                    }
                }
            }
        }

        // Check if the closest lyric is within the threshold
        return minDifference <= threshold ? closestLyric : string.Empty;
    }

    private IEnumerator LoadAudioClip(string filePath, System.Action<AudioClip> callback)
    {
        using UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + filePath, AudioType.WAV);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError("Error loading audio clip: " + www.error);
        }
        else
        {
            callback(DownloadHandlerAudioClip.GetContent(www));
        }
    }
}
