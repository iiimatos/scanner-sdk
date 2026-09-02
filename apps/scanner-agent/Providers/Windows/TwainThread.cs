using System.Windows.Interop;
using System.Windows.Threading;

namespace ScannerAgent.Providers;

// TWAIN's native DSM/source handles are thread-affine: every call touching
// an open session or source must run on the same thread that keeps pumping
// its Windows messages, or the native call can access-violate (0xC0000005).
// ASP.NET Core's thread pool can resume an await continuation on a different
// thread, so all TWAIN work is marshalled onto one dedicated STA thread.
internal sealed class TwainThread : IDisposable
{
    private readonly Dispatcher _dispatcher;
    private readonly HwndSource _hwndSource;

    public IntPtr WindowHandle { get; }

    public TwainThread()
    {
        using var ready = new ManualResetEventSlim();
        Dispatcher? dispatcher = null;
        HwndSource? hwndSource = null;

        var thread = new Thread(() =>
        {
            dispatcher = Dispatcher.CurrentDispatcher;
            SynchronizationContext.SetSynchronizationContext(
                new DispatcherSynchronizationContext(dispatcher)
            );

            hwndSource = new HwndSource(new HwndSourceParameters("ScannerSdkTwainHost")
            {
                Width = 0,
                Height = 0,
                WindowStyle = 0
            });

            ready.Set();
            Dispatcher.Run();
        })
        {
            IsBackground = true,
            Name = nameof(TwainThread)
        };

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        ready.Wait();
        _dispatcher = dispatcher!;
        _hwndSource = hwndSource!;
        WindowHandle = _hwndSource.Handle;
    }

    public Task<T> RunAsync<T>(Func<T> action) =>
        _dispatcher.InvokeAsync(action).Task;

    public async Task<T> RunAsync<T>(Func<Task<T>> action) =>
        await await _dispatcher.InvokeAsync(action);

    public void Dispose()
    {
        _dispatcher.Invoke(() => _hwndSource.Dispose());
        _dispatcher.InvokeShutdown();
    }
}
