using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

public class FfmpegService
{
    private string ffmpegPath = "ffmpeg.exe";
    private string segmentFolder = "tmp_segments";
    private string logFile = $"Logs/log_{DateTime.Now:yyyyMMdd_HHmmss}.txt";

    public async Task StartEncoding(string input, string clipName, EncoderProfile profile)
    {
        Directory.CreateDirectory(segmentFolder);
        Directory.CreateDirectory("Logs");

        await Log("Starting split process...");

        // STEP 1: SPLIT
        await RunFFmpeg(
            $"-y -i \"{input}\" -c copy -map 0 -f segment -segment_time 30 " +
            $"-reset_timestamps 1 {segmentFolder}\\clip_%03d.mov");

        await Log("Splitting complete.");

        // STEP 2: ENCODE EACH SEGMENT
        foreach (var file in Directory.GetFiles(segmentFolder, "clip_*.mov"))
        {
            if (IsSegmentCompleted(file))
                continue;

            string baseName = Path.GetFileNameWithoutExtension(file);
            string outputFile = $"Output\\{baseName}_{clipName}_4K60_vertical.mov";

            await Log($"Encoding: {baseName}");

            string args =
                $"-y -hwaccel cuda -i \"{file}\" " +
                $"-vf \"scale={profile.Width}:{profile.Height}:flags=lanczos,fps={profile.FPS},format={profile.PixelFormat}\" " +
                "-c:v hevc_nvenc " +
                $"-pix_fmt {profile.PixelFormat} " +
                $"-profile:v {profile.Profile} " +
                $"-preset {profile.Preset} " +
                "-tune hq -rc cbr_hq " +
                $"-b:v {profile.Bitrate} -maxrate {profile.Bitrate} -bufsize 2000M " +
                "-spatial-aq 1 -temporal-aq 1 -aq-strength 15 " +
                "-c:a copy " +
                $"\"{outputFile}\"";

            await RunFFmpeg(args);

            MarkSegmentCompleted(file);
        }

        await Log("All segments completed.");
    }

    private async Task RunFFmpeg(string arguments)
    {
        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = ffmpegPath,
            Arguments = arguments,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        Process process = new Process { StartInfo = psi };
        process.Start();

        string output = await process.StandardError.ReadToEndAsync();
        await Log(output);

        process.WaitForExit();
    }

    private bool IsSegmentCompleted(string file)
    {
        return File.Exists(file + ".done");
    }

    private void MarkSegmentCompleted(string file)
    {
        File.Create(file + ".done").Dispose();
    }

    private async Task Log(string message)
    {
        await File.AppendAllTextAsync(logFile,
            $"[{DateTime.Now}] {message}\n");
    }
}