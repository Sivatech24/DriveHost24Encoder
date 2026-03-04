public class EncoderProfile
{
    public string Name { get; set; }
    public int Width { get; set; } = 2160;
    public int Height { get; set; } = 3840;
    public int FPS { get; set; } = 60;
    public string PixelFormat { get; set; } = "yuv444p10le";
    public string Profile { get; set; } = "rext";
    public string Preset { get; set; } = "p7";
    public string Bitrate { get; set; } = "250M";
}