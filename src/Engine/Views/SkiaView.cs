namespace DrawnUi.Maui.Views;



public partial class SkiaView : SKCanvasView, ISkiaDrawable
{



	public bool IsHardwareAccelerated => false;

	public void SignalFrame(long nanoseconds)
	{ }

	public SKSurface CreateStandaloneSurface(int width, int height)
	{
		return SKSurface.Create(new SKImageInfo(width, height));
	}

	public Func<SKSurface, SKRect, bool> OnDraw { get; set; }

	public DrawnView Superview { get; protected set; }

	public void Dispose()
	{
		_surface = null;
		PaintSurface -= OnPaintingSurface;
		Superview = null;
	}

	public SkiaView(DrawnView superview)
	{
		Superview = superview;
		EnableTouchEvents = false;
	}

	public void Disconnect()
	{
		PaintSurface -= OnPaintingSurface;
	}

	protected override void OnHandlerChanged()
	{
		base.OnHandlerChanged();

		if (Handler == null)
		{
			PaintSurface -= OnPaintingSurface;

			Superview?.DisconnectedHandler();
		}
		else
		{
			PaintSurface -= OnPaintingSurface;
			PaintSurface += OnPaintingSurface;

			Superview?.ConnectedHandler();
		}
	}

	SKSurface _surface;
	private DateTime _lastFrame;
	private double _fps;
	private double _reportFps;


	public SKSurface Surface
	{
		get
		{
			return _surface;
		}
	}

	public double FPS
	{
		get
		{
			return _reportFps;
		}
	}
	public int MinMS => _reportMinMS;
	public int MaxMS => _reportMaxMS;
	private int _reportMinMS;
	private int _reportMaxMS;


	private double _fpsAverage;
	private int _fpsCount;
	private double _sumSeconds;   // Accumulate over this time "window".
	private long _lastFrameTimestamp;
	private const double discardSeconds = 0.5;   // tms TODO: Adjust dynamically, once there is enough data. BETTER, be told when to ignore a frame.
	private double _slowestTimeNotDiscarded = 0;
	private double _fastestTime = double.MaxValue;
	public static long GestureTimestamp;   // tms TODO: remove static when know how to find the active instance.
	public static bool UserGestureSeen;

	/// <summary>
	/// Calculates the frames per second (FPS) and updates the rolling average FPS every 'averageAmount' frames.
	/// </summary>
	/// <param name="currentTimestamp">The current timestamp in nanoseconds.</param>
	/// <param name="averageAmount">The number of frames over which to average the FPS. Default is 10.</param>
	void CalculateFPS(long currentTimestamp, int averageAmount = 10, double maxSeconds = 1.0)
	{
		if (_lastFrameTimestamp == 0)
		{   // First time called.
			_lastFrameTimestamp = currentTimestamp;
			_reportFps = 0;
			_ClearFPSAccumulators();
			return;
		}

		long elapsedTicks = currentTimestamp - _lastFrameTimestamp;
		_lastFrameTimestamp = currentTimestamp;
		double elapsedSeconds = elapsedTicks / 1_000_000_000.0;
		bool gestureSeen = UserGestureSeen;
		UserGestureSeen = false;

		const bool byTime = true;
		if (byTime)
		{
			// P: "byTime" only makes sense when there is an animation loop forcing redraw continuously.
			// If waiting for user input, there will be some very long frames.
			// HACK: Throw out excessively long times.
			// TODO: BETTER, would be to "know" whether we had been waiting, throw out the first "frame time".
			if (gestureSeen || (elapsedSeconds > discardSeconds))
			{
				// Don't count this frame.
				return;
			}
			// Remember extremes seen.
			// FUTURE: Remember min/max each time window.
			if (elapsedSeconds > _slowestTimeNotDiscarded)
				_slowestTimeNotDiscarded = elapsedSeconds;
			if (elapsedSeconds < _fastestTime)
				_fastestTime = elapsedSeconds;

			_fpsCount++;
			_sumSeconds += elapsedSeconds;
			if ((_fpsCount > averageAmount) || (_sumSeconds > maxSeconds))
			{
				_reportFps = _fpsCount / _sumSeconds;   // frames over seconds: what could be simpler?
				_reportMinMS = (int)Math.Round(_fastestTime * 1000);
				_reportMaxMS = (int)Math.Round(_slowestTimeNotDiscarded * 1000);
				_ClearFPSAccumulators();
			}
		}
		else
		{   // Original code. Calcs so-called "currentFPS" each frame, averages those.
			// Problem is this minimizes the weight of slow frames. Misleading answer if any very slow frames.
			// An extreme example: Suppose one frame took 500ms, followed by 10 frames each taking 50 ms.
			// That would be 11 frames in 1 sec. Instead of reporting "11 fps", this says:
			// (1 * (2) + 10 * (20)) / 11 =  202 / 11 ~=  "18 fps". Big difference.
			// Or the other extreme: one frame takes 1 ms, 59 frames each take 16 ms. That's 60 frames in ~1 sec or "60 fps".
			// But this says: (1 * (1000) + 59 * (60)) / 60 ~= "76 fps". Not realistic.
			// I even saw a number over 1000 fps briefly!
			// Convert nanoseconds to seconds for elapsed time calculation.

			double currentFps = 1.0 / elapsedSeconds;

			_fpsAverage = ((_fpsAverage * _fpsCount) + currentFps) / (_fpsCount + 1);
			_fpsCount++;

			if (_fpsCount >= averageAmount)
			{
				_reportFps = _fpsAverage;
				_fpsCount = 0;
				_fpsAverage = 0.0;
			}
		}
	}

	private void _ClearFPSAccumulators()
	{
		_fpsCount = 0;
		_sumSeconds = 0;
		_slowestTimeNotDiscarded = 0;
		_fastestTime = double.MaxValue;
	}

	public long FrameTime { get; protected set; }
	public long EndDrawTime { get; protected set; }

	public bool IsDrawing { get; protected set; }

	private void OnPaintingSurface(object sender, SKPaintSurfaceEventArgs paintArgs)
	{
		IsDrawing = true;

		FrameTime = Super.GetCurrentTimeNanos();

#if ANDROID
        CalculateFPS(FrameTime);
#else
		CalculateFPS(FrameTime, 60);
#endif

		if (OnDraw != null && Super.EnableRendering)
		{
			_surface = paintArgs.Surface;
			bool isDirty = OnDraw.Invoke(paintArgs.Surface, new SKRect(0, 0, paintArgs.Info.Width, paintArgs.Info.Height));

#if ANDROID
            if (maybeLowEnd && FPS > 160)
            {
                maybeLowEnd = false;
            }

            if (maybeLowEnd && isDirty && _fps < 55) //kick refresh for low-end devices
            {
                InvalidateSurface();
                return;
            }
#endif

		}

		EndDrawTime = Super.GetCurrentTimeNanos();
		IsDrawing = false;
	}

	static bool maybeLowEnd = true;

	public void Update(long nanos)
	{
		if (
			Super.EnableRendering &&
			this.Handler != null && this.Handler.PlatformView != null && CanvasSize is { Width: > 0, Height: > 0 })
		{
			IsDrawing = true;
			InvalidateSurface();
		}
	}


}
