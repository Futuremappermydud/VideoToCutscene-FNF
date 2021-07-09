using System;
using System.Data;
using System.Diagnostics;
using System.IO;

namespace VideoToCutscene
{
    [Serializable]
    public class FFProbe_Stream
    {

    }
    [Serializable]
    public class FFProbe_Format
    {

    }
    [Serializable]
    public class FFProbe
    {

    }
    class Program
    {
        public string ffxPath;

        public static bool ExistsOnPath(string fileName, out string path)
        {
            string Out = GetFullPath(fileName);
            path = Out;
            return Out != null;
        }

        public static string GetFullPath(string fileName)
        {
            if (File.Exists(fileName))
                return Path.GetFullPath(fileName);

            var values = Environment.GetEnvironmentVariable("PATH");
            foreach (var path in values.Split(Path.PathSeparator))
            {
                var fullPath = Path.Combine(path, fileName);
                if (File.Exists(fullPath))
                    return fullPath;
            }
            return null;
        }

        public static string ffmpeg;
        public static string ffprobe;

        static void Main(string[] args)
        {
            if (ExistsOnPath("ffmpeg.exe", out ffmpeg) && ExistsOnPath("ffprobe.exe", out ffprobe))
            {
                Console.WriteLine("ffmpeg found!");
            }
            else
            {
                Console.WriteLine("Input ffmpeg path");
                ffmpeg  = Console.ReadLine();

                Console.WriteLine("Input ffprobe path");
                ffprobe = Console.ReadLine();
            }

            Console.WriteLine("Video Path");
            string videoPath = Console.ReadLine();

            if(!File.Exists(videoPath))
            {
                Console.WriteLine("ERROR: Video File DOES NOT Exist");
                return;
            }
            string basePath = Path.GetDirectoryName(videoPath);
            Console.WriteLine(basePath);

            int ffprobeFrameCount = 0;
            double ffprobeFrameRate = 0;

            var frameCountProc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ffprobe,
                    Arguments = "-v error -count_frames -select_streams v:0 -show_entries stream=nb_read_frames -of default=nokey=1:noprint_wrappers=1 " + videoPath,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            frameCountProc.Start();
            while (!frameCountProc.StandardOutput.EndOfStream)
            {
                string line = frameCountProc.StandardOutput.ReadLine();
                Console.WriteLine(line);
                if (!int.TryParse(line, out ffprobeFrameCount))
                {
                    Console.WriteLine("Failed To Read Frame Count");
                    return;
                }
                frameCountProc.Kill();
            }

            File.WriteAllText(Path.Combine(basePath, Path.GetFileNameWithoutExtension(videoPath) + "_FrameCount.txt"), ffprobeFrameCount.ToString());

            var frameRateProc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ffprobe,
                    Arguments = "-v error -select_streams v -of default=noprint_wrappers=1:nokey=1 -show_entries stream=r_frame_rate " + videoPath,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            frameRateProc.Start();
            while (!frameRateProc.StandardOutput.EndOfStream)
            {
                string frameRateLine = frameRateProc.StandardOutput.ReadLine();
                DataTable dt = new DataTable();
                ffprobeFrameRate = (double)dt.Compute(frameRateLine, "");
                frameRateProc.Kill();
            }

            File.WriteAllText(Path.Combine(basePath, Path.GetFileNameWithoutExtension(videoPath) + "_FrameRate.txt"), ffprobeFrameRate.ToString());

            string baseVideoName = Path.GetFileNameWithoutExtension(videoPath);

            var extractAudioProc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ffmpeg,
                    Arguments = "-i " + videoPath + " -q:a 0 -map a " + Path.Combine(basePath, baseVideoName + ".mp3"),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            extractAudioProc.Start();
            while (!extractAudioProc.StandardOutput.EndOfStream)
            {
                string line = extractAudioProc.StandardOutput.ReadLine();
                Console.WriteLine(line);
            }
            extractAudioProc.Kill();

            var convertAudioProc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ffmpeg,
                    Arguments = "-i " + Path.Combine(basePath, baseVideoName + ".mp3") + " -c:a libvorbis -q:a 4 " + Path.Combine(basePath, baseVideoName + ".ogg"),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            convertAudioProc.Start();
            while (!convertAudioProc.StandardOutput.EndOfStream)
            {
                string line = convertAudioProc.StandardOutput.ReadLine();
                Console.WriteLine(line);
            }
            convertAudioProc.Kill();

            var convertVideoProc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ffmpeg,
                    Arguments = "-i " + videoPath + " -c:v libvpx -b:v 1M -crf 18 -c:a libvorbis " + Path.Combine(basePath, baseVideoName + ".webm"),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            convertVideoProc.Start();
            while (!convertVideoProc.StandardOutput.EndOfStream)
            {
                string line = convertVideoProc.StandardOutput.ReadLine();
                Console.WriteLine(line);
            }
            convertVideoProc.Kill();
        }
    }
}

//D:\!Cinema\vsoaty\assets\preload\cutscenes\Test\transformation.mp4