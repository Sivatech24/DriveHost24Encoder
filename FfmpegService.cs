using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

public class FfmpegService
{
    private string ffmpegPath = "ffmpeg.exe";
    private string segmentFolder = "tmp_segments";
    private string logsFolder = "Logs";
    private string outputFolder = "Output";
    private string logFile;

    public event Action<string> OnLog; // raised for UI to show live logs

    public FfmpegService()
    {
        Directory.CreateDirectory(segmentFolder);
        Directory.CreateDirectory(logsFolder);
        Directory.CreateDirectory(outputFolder);
        logFile = Path.Combine(logsFolder, $"log_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
    }

    public async Task StartEncoding(string input, string clipName, EncoderProfile profile,
        Metadata metadata = null)
    {
        await Log("Starting split process...");

        // STEP 1: SPLIT if no segments exist
        var existing = Directory.GetFiles(segmentFolder, "clip_*.mov");
        if (existing.Length == 0)
        {
            await RunFFmpeg($"-y -i \"{input}\" -c copy -map 0 -f segment -segment_time 30 -reset_timestamps 1 {segmentFolder}\\clip_%03d.mov");
            await Log("Splitting complete.");
        }

        // STEP 2: ENCODE EACH SEGMENT
        foreach (var file in Directory.GetFiles(segmentFolder, "clip_*.mov"))
        {
            if (IsSegmentCompleted(file))
                continue;

            string baseName = Path.GetFileNameWithoutExtension(file);
            string safeClipName = MakeSafeFileName(clipName);
            string outputFile = Path.Combine(outputFolder, $"{baseName}_{safeClipName}_4K60_vertical.mov");

            await Log($"Encoding: {baseName}");

            var sb = new StringBuilder();
            sb.Append($"-y -hwaccel cuda -i \"{file}\" ");
            sb.Append($"-vf \"scale={profile.Width}:{profile.Height}:flags=lanczos,fps={profile.FPS},format={profile.PixelFormat}\" ");
            sb.Append("-c:v hevc_nvenc ");
            sb.Append($"-pix_fmt {profile.PixelFormat} ");
            sb.Append($"-profile:v {profile.Profile} ");
            sb.Append($"-preset {profile.Preset} ");
            sb.Append("-tune hq -rc cbr_hq ");
            sb.Append($"-b:v {profile.Bitrate} -maxrate {profile.Bitrate} -bufsize 2000M ");
            sb.Append("-spatial-aq 1 -temporal-aq 1 -aq-strength 15 ");
            sb.Append("-c:a copy ");

            if (metadata != null)
            {
                if (!string.IsNullOrEmpty(metadata.Title)) sb.Append($"-metadata title=\"{metadata.Title}\" ");
                if (!string.IsNullOrEmpty(metadata.Artist)) sb.Append($"-metadata artist=\"{metadata.Artist}\" ");
                if (!string.IsNullOrEmpty(metadata.Director)) sb.Append($"-metadata director=\"{metadata.Director}\" ");
                if (!string.IsNullOrEmpty(metadata.Producer)) sb.Append($"-metadata producer=\"{metadata.Producer}\" ");
                if (!string.IsNullOrEmpty(metadata.Writer)) sb.Append($"-metadata writer=\"{metadata.Writer}\" ");
                if (!string.IsNullOrEmpty(metadata.Year)) sb.Append($"-metadata year=\"{metadata.Year}\" ");
                if (!string.IsNullOrEmpty(metadata.Genre)) sb.Append($"-metadata genre=\"{metadata.Genre}\" ");
                if (!string.IsNullOrEmpty(metadata.Publisher)) sb.Append($"-metadata publisher=\"{metadata.Publisher}\" ");
                if (!string.IsNullOrEmpty(metadata.ContentProvider)) sb.Append($"-metadata \"content_provider={metadata.ContentProvider}\" ");
                if (!string.IsNullOrEmpty(metadata.EncodedBy)) sb.Append($"-metadata \"encoded_by={metadata.EncodedBy}\" ");
                if (!string.IsNullOrEmpty(metadata.Author)) sb.Append($"-metadata author=\"{metadata.Author}\" ");
                if (!string.IsNullOrEmpty(metadata.Copyright)) sb.Append($"-metadata copyright=\"{metadata.Copyright}\" ");
                if (!string.IsNullOrEmpty(metadata.Comment)) sb.Append($"-metadata comment=\"{metadata.Comment}\" ");
            }

            sb.Append($"\"{outputFile}\"");

            await RunFFmpeg(sb.ToString());

            MarkSegmentCompleted(file);
        }

        await Log("All segments completed.");
    }

    private async Task RunFFmpeg(string arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = ffmpegPath,
            Arguments = arguments,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (var process = new Process { StartInfo = psi, EnableRaisingEvents = true })
        {
            var tcs = new TaskCompletionSource<int>();

            process.ErrorDataReceived += (s, e) =>
            {
                if (e.Data == null) return;
                _ = Log(e.Data);
            };

            process.Exited += (s, e) => tcs.TrySetResult(process.ExitCode);

            process.Start();
            process.BeginErrorReadLine();

            await tcs.Task; // wait for exit
        }
    }

    private bool IsSegmentCompleted(string file)
    {
        return File.Exists(file + ".done");
    }

    private void MarkSegmentCompleted(string file)
    {
        try
        {
            File.Create(file + ".done").Dispose();
        }
        catch { }
    }

    private Task Log(string message)
    {
        string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\n";
        try
        {
            File.AppendAllText(logFile, line);
        }
        catch { }

        try
        {
            OnLog?.Invoke(line);
        }
        catch { }

        return Task.CompletedTask;
    }

    private static string MakeSafeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name;
    }
}

public class Metadata
{
    public string Title { get; set; }
    public string Artist { get; set; }
    public string Director { get; set; }
    public string Producer { get; set; }
    public string Writer { get; set; }
    public string Year { get; set; }
    public string Genre { get; set; }
    public string Publisher { get; set; }
    public string ContentProvider { get; set; }
    public string EncodedBy { get; set; }
    public string Author { get; set; }
    public string Copyright { get; set; }
    public string Comment { get; set; }
}
