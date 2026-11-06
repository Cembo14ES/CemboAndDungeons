using Godot;
using System;
using System.Diagnostics;

public partial class SystemPerformance: Label
{
    [Export] public double refreshSeconds { get; set; } = 1.0;

    private const double gb = 1024.0 * 1024.0 * 1024.0;

    private Process process;
    private TimeSpan lastCpuTime;
    private ulong lastTicks;
    private double timer;
    private Rid viewportRid;
    private string gpuName;

    public override void _Ready()
    {
        process = Process.GetCurrentProcess();
        lastCpuTime = process.TotalProcessorTime;
        lastTicks = Time.GetTicksMsec();

        // Activa la medición del tiempo de render de la GPU
        viewportRid = GetViewport().GetViewportRid();
        RenderingServer.ViewportSetMeasureRenderTime(viewportRid, true);

        gpuName = RenderingServer.GetVideoAdapterName();
    }

    public override void _Process(double delta)
    {
        timer += delta;
        if (timer < refreshSeconds) return;
        timer = 0;

        Text = BuildText();
    }

    private string BuildText()
    {
        // CPU del proceso (0-100 % sobre el total de la máquina)
        process.Refresh();
        TimeSpan cpuNow = process.TotalProcessorTime;
        ulong ticksNow = Time.GetTicksMsec();

        double cpuMs = (cpuNow - lastCpuTime).TotalMilliseconds;
        double elapsedMs = ticksNow - lastTicks;
		double cpuPercent = elapsedMs > 0
			? cpuMs / (elapsedMs * System.Environment.ProcessorCount) * 100.0
			: 0;

        lastCpuTime = cpuNow;
        lastTicks = ticksNow;

        // RAM y VRAM
        double ramGb = process.WorkingSet64 / gb;
        double vramGb = Performance.GetMonitor(Performance.Monitor.RenderVideoMemUsed) / gb;

        // Tiempos por frame
        double fps = Engine.GetFramesPerSecond();
        double frameMs = fps > 0 ? 1000.0 / fps : 0;
        double gpuMs = RenderingServer.ViewportGetMeasuredRenderTimeGpu(viewportRid);
        double gpuPercent = frameMs > 0 ? Math.Min(gpuMs / frameMs * 100.0, 100.0) : 0;

        return
            $"FPS: {fps:F0} ({frameMs:F1} ms)\n" +
            $"CPU: {cpuPercent:F1} %\n" +
            $"GPU: {gpuMs:F2} ms (~{gpuPercent:F0} % del frame)\n" +
            $"RAM: {ramGb:F2} GB\n" +
            $"VRAM: {vramGb:F2} GB\n" +
            $"{gpuName}";
    }
}