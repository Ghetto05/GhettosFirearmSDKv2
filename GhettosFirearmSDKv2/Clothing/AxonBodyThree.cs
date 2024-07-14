using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Threading;
using ThunderRoad;

namespace GhettosFirearmSDKv2
{
    public class AxonBodyThree : MonoBehaviour
    {
        // Video Capture Variables
        public Camera captureCamera;
        public int frameRate = 32;
        public int width = 640;
        public int height = 480;
        public float interval = 15 * 60; // 15 minutes
        public RawImage cameraDisplay; // UI element to display the camera feed
        public Canvas overlayCanvas; // Canvas for the UI overlay

        private RenderTexture renderTexture;
        private List<byte[]> frameDataList;
        private List<string> framePaths;
        private float frameTime;
        private float videoTimer = 0f;
        private int videoFileCount = 0;

        // Audio Capture Variables
        private List<float> audioData;
        private int sampleRate = 44100;
        private int channels = 1; // Mono
        private float audioTimer = 0f;
        private int audioFileCount = 0;

        private string ffmpegPath;
        private string dataPath;
        private string videoPath;
        private Thread fileIOThread;
        
        private bool isApplicationQuitting = false;

        void Start()
        {
            frameTime = 1.0f / frameRate;
            frameDataList = new List<byte[]>();
            framePaths = new List<string>();
            audioData = new List<float>();

            string assemblyPath = FileManager.GetFullPath(FileManager.Type.JSONCatalog, FileManager.Source.Mods, "!GhettosFirearmSDKv2");
            ffmpegPath = Path.Combine(assemblyPath, "ffmpeg", "bin", "ffmpeg.exe");
            dataPath = Path.Combine(assemblyPath, "TEMP", "BodyCamData");
            videoPath = Path.Combine(assemblyPath, "BodyCamVideos");

            Directory.CreateDirectory(dataPath);
            Directory.CreateDirectory(videoPath);

            // Set up the RenderTexture and RawImage
            renderTexture = new RenderTexture(width, height, 24);
            captureCamera.targetTexture = renderTexture;
            if (cameraDisplay != null)
            {
                cameraDisplay.texture = renderTexture;
            }

            // Enable the overlay canvas
            if (overlayCanvas != null)
            {
                overlayCanvas.worldCamera = captureCamera;
                overlayCanvas.planeDistance = captureCamera.nearClipPlane + 0.01f;
            }

            fileIOThread = new Thread(FileIOProcess);
            fileIOThread.Start();

            StartCoroutine(CaptureVideo());
            StartCoroutine(CaptureAudio());
        }

        void OnApplicationQuit()
        {
            isApplicationQuitting = true;
            SaveAllData();
            fileIOThread.Join(); // Wait for the file IO thread to finish
        }

        void OnDestroy()
        {
            if (!isApplicationQuitting)
            {
                SaveAllData();
            }
        }

        IEnumerator CaptureVideo()
        {
            while (true)
            {
                yield return new WaitForEndOfFrame();

                RenderTexture rt = new RenderTexture(width, height, 24);
                captureCamera.targetTexture = rt;
                Texture2D screenShot = new Texture2D(width, height, TextureFormat.RGB24, false);
                captureCamera.Render();

                RenderTexture.active = rt;
                screenShot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                screenShot.Apply();
                captureCamera.targetTexture = renderTexture;
                RenderTexture.active = null;
                Destroy(rt);

                // byte[] bytes = screenShot.EncodeToJPG(); // Use JPG for compression
                // lock (frameDataList)
                // {
                //     frameDataList.Add(bytes);
                // }
                // Destroy(screenShot);
                //
                // videoTimer += frameTime;
                // if (videoTimer >= interval)
                // {
                //     SaveVideo();
                //     videoTimer = 0f;
                // }
            }
        }

        IEnumerator CaptureAudio()
        {
            while (true)
            {
                yield return new WaitForSeconds(1.0f / sampleRate);
                float[] samples = new float[256];
                AudioListener.GetOutputData(samples, 0);
                lock (audioData)
                {
                    audioData.AddRange(samples);
                }

                audioTimer += 1.0f / sampleRate;
                if (audioTimer >= interval)
                {
                    SaveAudio();
                    audioTimer = 0f;
                }
            }
        }

        void SaveVideo()
        {
            lock (framePaths)
            {
                for (int i = 0; i < frameDataList.Count; i++)
                {
                    string framePath = Path.Combine(dataPath, $"frame_{videoFileCount}_{i}.jpg");
                    framePaths.Add(framePath);
                    File.WriteAllBytes(framePath, frameDataList[i]);
                }
                frameDataList.Clear();

                string videoOutputPath = Path.Combine(videoPath, $"video_{videoFileCount}.mp4");
                string inputPattern = Path.Combine(dataPath, $"frame_{videoFileCount}_%d.jpg");

                string arguments = $"-framerate {frameRate} -i {inputPattern} -c:v libx264 -pix_fmt yuv420p {videoOutputPath}";

                ProcessStartInfo startInfo = new ProcessStartInfo()
                {
                    FileName = ffmpegPath,
                    Arguments = arguments,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                Process process = Process.Start(startInfo);
                process.WaitForExit();
                videoFileCount++;
            }
        }

        void SaveAudio()
        {
            lock (audioData)
            {
                string audioOutputPath = Path.Combine(dataPath, $"audio_{audioFileCount}.wav");
                using (var file = File.Create(audioOutputPath))
                {
                    byte[] wavData = ConvertToWav(audioData.ToArray(), channels, sampleRate);
                    file.Write(wavData, 0, wavData.Length);
                }
                audioData.Clear();
                audioFileCount++;
            }
        }

        byte[] ConvertToWav(float[] samples, int channels, int sampleRate)
        {
            int sampleCount = samples.Length;
            int byteRate = sampleRate * channels * 2; // 2 bytes per sample (16 bits)
            int fileSize = 44 + sampleCount * 2; // Header size (44 bytes) + data size

            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(memoryStream))
                {
                    // RIFF header
                    writer.Write("RIFF".ToCharArray());
                    writer.Write(fileSize - 8);
                    writer.Write("WAVE".ToCharArray());

                    // fmt sub-chunk
                    writer.Write("fmt ".ToCharArray());
                    writer.Write(16); // Sub-chunk size (16 for PCM)
                    writer.Write((short)1); // Audio format (1 for PCM)
                    writer.Write((short)channels);
                    writer.Write(sampleRate);
                    writer.Write(byteRate);
                    writer.Write((short)(channels * 2)); // Block align
                    writer.Write((short)16); // Bits per sample

                    // data sub-chunk
                    writer.Write("data".ToCharArray());
                    writer.Write(sampleCount * 2);

                    // Write audio data
                    foreach (float sample in samples)
                    {
                        writer.Write((short)(sample * short.MaxValue));
                    }
                }

                return memoryStream.ToArray();
            }
        }

        void FileIOProcess()
        {
            while (!isApplicationQuitting)
            {
                lock (framePaths)
                {
                    if (framePaths.Count > 0)
                    {
                        for (int i = 0; i < framePaths.Count; i++)
                        {
                            string framePath = framePaths[i];
                            if (File.Exists(framePath))
                            {
                                File.Delete(framePath);
                            }
                        }
                        framePaths.Clear();
                    }
                }
                Thread.Sleep(100); // Check for new frames to save every 100 ms
            }
        }

        void SaveAllData()
        {
            lock (framePaths)
            {
                if (frameDataList.Count > 0)
                {
                    SaveVideo();
                    frameDataList.Clear();
                }

                if (audioData.Count > 0)
                {
                    SaveAudio();
                    audioData.Clear();
                }

                foreach (var framePath in framePaths)
                {
                    if (File.Exists(framePath))
                    {
                        File.Delete(framePath);
                    }
                }
            }
        }
    }
}
