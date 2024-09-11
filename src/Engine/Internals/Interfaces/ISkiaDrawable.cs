namespace DrawnUi.Maui.Draw;

public interface ISkiaDrawable : ISkiaSharpView, IDisposable
{
    /// <summary>
    /// Return true if need force invalidation on next frame
    /// </summary>
    public Func<SKSurface, SKRect, bool> OnDraw { get; set; }

    public SKSurface Surface { get; }

    public bool IsHardwareAccelerated { get; }

    public double FPS { get; }
	public int MinMS { get; }
	public int MaxMS { get; }
	public float AvgDrawMS { get; }
	public int AvgMiscMS { get; }

	public bool IsDrawing { get; }

    long FrameTime { get; }
}