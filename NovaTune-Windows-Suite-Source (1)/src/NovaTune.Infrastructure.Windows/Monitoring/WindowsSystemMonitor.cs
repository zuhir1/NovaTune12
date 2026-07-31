using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using NovaTune.Core.Abstractions;
using NovaTune.Core.Models;

namespace NovaTune.Infrastructure.Windows.Monitoring;

public sealed class WindowsSystemMonitor : ISystemMonitor
{
    private readonly object _gate = new();
    private CpuTimes? _previousCpu;
    private NetworkSample? _previousNetwork;

    public Task<SystemSnapshot> CaptureAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var memory = new MemoryStatus { Length = (uint)Marshal.SizeOf<MemoryStatus>() };
        if (!GlobalMemoryStatusEx(ref memory)) throw new InvalidOperationException("Unable to read Windows memory status.");

        var systemRoot = Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\";
        var drive = new DriveInfo(systemRoot);
        var now = DateTimeOffset.UtcNow;
        var cpu = ReadCpu();
        var network = ReadNetwork(now);

        var snapshot = new SystemSnapshot(
            now,
            cpu,
            memory.MemoryLoad,
            memory.TotalPhysical - memory.AvailablePhysical,
            memory.TotalPhysical,
            drive.TotalSize == 0 ? 0 : (drive.TotalSize - drive.AvailableFreeSpace) * 100d / drive.TotalSize,
            drive.AvailableFreeSpace,
            drive.TotalSize,
            network.ReceivePerSecond,
            network.SendPerSecond,
            TimeSpan.FromMilliseconds(Environment.TickCount64));
        return Task.FromResult(snapshot);
    }

    private double ReadCpu()
    {
        if (!GetSystemTimes(out var idle, out var kernel, out var user)) return 0;
        var current = new CpuTimes(ToUInt64(idle), ToUInt64(kernel), ToUInt64(user));
        lock (_gate)
        {
            var previous = _previousCpu;
            _previousCpu = current;
            if (previous is null) return 0;
            var idleDelta = current.Idle - previous.Value.Idle;
            var totalDelta = current.Kernel - previous.Value.Kernel + current.User - previous.Value.User;
            return totalDelta == 0 ? 0 : Math.Clamp((totalDelta - idleDelta) * 100d / totalDelta, 0, 100);
        }
    }

    private (long ReceivePerSecond, long SendPerSecond) ReadNetwork(DateTimeOffset now)
    {
        long receive = 0, send = 0;
        foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces().Where(n => n.OperationalStatus == OperationalStatus.Up))
        {
            var stats = adapter.GetIPv4Statistics();
            receive += stats.BytesReceived;
            send += stats.BytesSent;
        }
        lock (_gate)
        {
            var current = new NetworkSample(now, receive, send);
            var previous = _previousNetwork;
            _previousNetwork = current;
            if (previous is null) return (0, 0);
            var seconds = Math.Max((now - previous.Value.At).TotalSeconds, .001);
            return ((long)Math.Max(0, (receive - previous.Value.Receive) / seconds), (long)Math.Max(0, (send - previous.Value.Send) / seconds));
        }
    }

    private static ulong ToUInt64(FileTime value) => ((ulong)value.High << 32) | value.Low;

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatus status);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemTimes(out FileTime idleTime, out FileTime kernelTime, out FileTime userTime);

    [StructLayout(LayoutKind.Sequential)] private struct FileTime { public uint Low; public uint High; }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MemoryStatus
    {
        public uint Length; public uint MemoryLoad; public ulong TotalPhysical; public ulong AvailablePhysical;
        public ulong TotalPageFile; public ulong AvailablePageFile; public ulong TotalVirtual; public ulong AvailableVirtual; public ulong AvailableExtendedVirtual;
    }
    private readonly record struct CpuTimes(ulong Idle, ulong Kernel, ulong User);
    private readonly record struct NetworkSample(DateTimeOffset At, long Receive, long Send);
}
