using ScannerAgent.Errors;
using ScannerAgent.Models;
using ScannerAgent.Scanning;
using NTwain;
using NTwain.Data;
using System.IO;
using System.Runtime.InteropServices;

namespace ScannerAgent.Providers;

public sealed class TwainScannerProvider : IScannerProvider, IDisposable
{
    private static readonly ScannerCapabilities DefaultCapabilities = new(
        Resolutions: [200, 300],
        ColorModes: ["color", "grayscale", "black-white"],
        Sources: ["flatbed"],
        Formats: ["pdf", "png", "jpeg"],
        Duplex: false
    );

    private readonly SemaphoreSlim _twainLock = new(1, 1);
    private readonly TwainThread _twainThread = new();

    public bool IsAvailable => IsTwainRuntimeAvailable();

    public void Dispose()
    {
        _twainThread.Dispose();
        _twainLock.Dispose();
    }

    public async Task<IReadOnlyList<ScannerDevice>> GetDevicesAsync(
        CancellationToken cancellationToken = default
    )
    {
        if (!IsAvailable)
        {
            return [];
        }

        await _twainLock.WaitAsync(cancellationToken);

        try
        {
            return await _twainThread.RunAsync(() =>
                WithOpenSession(session =>
                    session
                        .GetSources()
                        .Select(ToScannerDevice)
                        .ToList()
                )
            );
        }
        finally
        {
            _twainLock.Release();
        }
    }

    public async Task<ScannerCapabilities?> GetCapabilitiesAsync(
        string deviceId,
        CancellationToken cancellationToken = default
    )
    {
        if (!IsAvailable)
        {
            return null;
        }

        await _twainLock.WaitAsync(cancellationToken);

        try
        {
            return await _twainThread.RunAsync(() =>
                WithOpenSession(session =>
                {
                    var source = FindSource(session, deviceId);

                    if (source is null)
                    {
                        return null;
                    }

                    var openResult = source.Open();

                    if (openResult != ReturnCode.Success)
                    {
                        throw new ScannerOperationException(
                            "TWAIN_SOURCE_OPEN_FAILED",
                            $"TWAIN source '{source.Name}' could not be opened."
                        );
                    }

                    try
                    {
                        return ReadCapabilities(source);
                    }
                    finally
                    {
                        source.Close();
                    }
                })
            );
        }
        finally
        {
            _twainLock.Release();
        }
    }

    public async Task<ScanResult> ScanAsync(
        ScanOptions options,
        CancellationToken cancellationToken = default
    )
    {
        if (!IsAvailable)
        {
            throw new ScannerDeviceNotFoundException(
                options.DeviceId
            );
        }

        await _twainLock.WaitAsync(cancellationToken);

        try
        {
            return await _twainThread.RunAsync(() =>
                WithOpenSessionAsync(async session =>
                {
                    var source = FindSource(session, options.DeviceId);

                    if (source is null)
                    {
                        throw new ScannerDeviceNotFoundException(
                            options.DeviceId
                        );
                    }

                    var openResult = source.Open();

                    if (openResult != ReturnCode.Success)
                    {
                        throw new ScannerOperationException(
                            "TWAIN_SOURCE_OPEN_FAILED",
                            $"TWAIN source '{source.Name}' could not be opened."
                        );
                    }

                    try
                    {
                        ConfigureSource(source, options);

                        return await ScanWithFileTransferAsync(
                            session,
                            source,
                            options,
                            _twainThread.WindowHandle,
                            cancellationToken
                        );
                    }
                    finally
                    {
                        source.Close();
                    }
                })
            );
        }
        finally
        {
            _twainLock.Release();
        }
    }

    private static bool IsTwainRuntimeAvailable()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            && PlatformInfo.Current.IsSupported
            && PlatformInfo.Current.DsmExists;
    }

    private static T WithOpenSession<T>(
        Func<TwainSession, T> action
    )
    {
        var session = CreateSession();
        var openResult = session.Open();

        if (openResult != ReturnCode.Success)
        {
            throw new ScannerOperationException(
                "TWAIN_DSM_OPEN_FAILED",
                "TWAIN Data Source Manager could not be opened."
            );
        }

        try
        {
            return action(session);
        }
        finally
        {
            session.Close();
        }
    }

    private static async Task<T> WithOpenSessionAsync<T>(
        Func<TwainSession, Task<T>> action
    )
    {
        var session = CreateSession();
        var openResult = session.Open();

        if (openResult != ReturnCode.Success)
        {
            throw new ScannerOperationException(
                "TWAIN_DSM_OPEN_FAILED",
                "TWAIN Data Source Manager could not be opened."
            );
        }

        try
        {
            return await action(session);
        }
        finally
        {
            session.Close();
        }
    }

    private static TwainSession CreateSession()
    {
        var appId = TWIdentity.Create(
            DataGroups.Image,
            new Version(0, 1, 0),
            "Scanner SDK",
            "Scanner SDK",
            "Scanner Agent",
            "Local Scanner SDK Agent"
        );

        return new TwainSession(appId);
    }

    private static async Task<ScanResult> ScanWithFileTransferAsync(
        TwainSession session,
        DataSource source,
        ScanOptions options,
        IntPtr windowHandle,
        CancellationToken cancellationToken
    )
    {
        if (ToTwainFileFormat(options.Format) is null)
        {
            throw new UnsupportedCapabilityException(
                "format",
                options.Format,
                DefaultCapabilities.Formats
            );
        }

        // This driver (and others like it) accepts DAT_SETUPFILEXFER for PDF
        // without error but never actually writes the output file. JPEG file
        // transfer is reliable, so PDF output is produced by scanning to
        // JPEG and wrapping it into a single-page PDF ourselves.
        var driverFormat = options.Format.Equals("pdf", StringComparison.OrdinalIgnoreCase)
            ? "jpeg"
            : options.Format;
        var fileFormat = ToTwainFileFormat(driverFormat)!;

        var supportedTransferMechanisms = ReadValues(source.Capabilities.ICapXferMech);

        if (supportedTransferMechanisms.Count > 0
            && !supportedTransferMechanisms.Contains(XferMech.File))
        {
            throw new ScannerOperationException(
                "TWAIN_FILE_TRANSFER_NOT_SUPPORTED",
                $"TWAIN source '{source.Name}' does not support file transfer."
            );
        }

        EnsureSuccess(
            source.Capabilities.ICapXferMech.SetValue(XferMech.File),
            "TWAIN_SET_TRANSFER_FAILED",
            "TWAIN file transfer mode could not be configured."
        );
        EnsureSuccess(
            source.Capabilities.ICapImageFileFormat.SetValue(fileFormat.Value),
            "TWAIN_SET_FORMAT_FAILED",
            "TWAIN output format could not be configured."
        );

        var filePath = CreateScanFilePath(driverFormat);
        var setupFileXfer = new TWSetupFileXfer
        {
            FileName = filePath,
            Format = fileFormat.Value
        };

        EnsureSuccess(
            source.DGControl.SetupFileXfer.Set(setupFileXfer),
            "TWAIN_SET_OUTPUT_FILE_FAILED",
            "TWAIN output file could not be configured."
        );

        var transfer = new TaskCompletionSource<string>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );

        void OnDataTransferred(
            object? sender,
            DataTransferredEventArgs args
        )
        {
            if (args.DataSource != source)
            {
                return;
            }

            if (args.TransferType == XferMech.File
                && !string.IsNullOrWhiteSpace(args.FileDataPath))
            {
                transfer.TrySetResult(args.FileDataPath);
                return;
            }

            if (File.Exists(filePath))
            {
                transfer.TrySetResult(filePath);
                return;
            }

            transfer.TrySetException(new ScannerOperationException(
                "TWAIN_TRANSFER_FAILED",
                "TWAIN transfer completed without producing an output file."
            ));
        }

        void OnTransferError(
            object? sender,
            TransferErrorEventArgs args
        )
        {
            transfer.TrySetException(new ScannerOperationException(
                "TWAIN_TRANSFER_FAILED",
                $"TWAIN transfer failed with return code '{args.ReturnCode}'."
            ));
        }

        var sourceDisabled = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );

        void OnSourceDisabled(
            object? sender,
            EventArgs args
        )
        {
            if (!transfer.Task.IsCompleted)
            {
                transfer.TrySetException(new ScannerOperationException(
                    "TWAIN_TRANSFER_CANCELLED",
                    "TWAIN source was disabled before a page was transferred."
                ));
            }

            sourceDisabled.TrySetResult(true);
        }

        session.DataTransferred += OnDataTransferred;
        session.TransferError += OnTransferError;
        session.SourceDisabled += OnSourceDisabled;

        try
        {
            EnsureSuccess(
                source.Enable(SourceEnableMode.NoUI, false, windowHandle),
                "TWAIN_ENABLE_SOURCE_FAILED",
                $"TWAIN source '{source.Name}' could not be enabled."
            );

            var completedTask = await Task.WhenAny(
                transfer.Task,
                Task.Delay(TimeSpan.FromSeconds(60), cancellationToken)
            );

            if (completedTask != transfer.Task)
            {
                throw new ScannerOperationException(
                    "TWAIN_TRANSFER_TIMEOUT",
                    "TWAIN source did not transfer a page before the timeout."
                );
            }

            var completedFilePath = await transfer.Task;

            // The driver still owns the source at this point (TWAIN state
            // Enabled/Transferring). Closing it before the driver disables it
            // itself is an invalid state transition that has been observed to
            // crash the Epson driver natively. Give it a grace period to
            // disable on its own before handing control back to the caller,
            // whose `finally` block will call source.Close().
            await Task.WhenAny(
                sourceDisabled.Task,
                Task.Delay(TimeSpan.FromSeconds(5))
            );

            await WaitForFileReadyAsync(
                completedFilePath,
                cancellationToken
            );

            var fileBytes = await File.ReadAllBytesAsync(
                completedFilePath,
                cancellationToken
            );

            TryDeleteFile(completedFilePath);

            var outputBytes = driverFormat == options.Format
                ? fileBytes
                : TwainPdfDocument.WrapJpeg(fileBytes, options.Dpi);
            var outputFileName =
                $"{Path.GetFileNameWithoutExtension(completedFilePath)}.{options.Format.ToLowerInvariant()}";

            return new ScanResult(
                Id: $"scan_{options.DeviceId}_{DateTimeOffset.UtcNow:yyyyMMddHHmmss}",
                DeviceId: options.DeviceId,
                Status: "completed",
                Format: options.Format,
                MimeType: ToMimeType(options.Format),
                FileName: outputFileName,
                Message: "TWAIN scan completed.",
                DataBase64: Convert.ToBase64String(outputBytes)
            );
        }
        finally
        {
            session.DataTransferred -= OnDataTransferred;
            session.TransferError -= OnTransferError;
            session.SourceDisabled -= OnSourceDisabled;
        }
    }

    private static void ConfigureSource(
        DataSource source,
        ScanOptions options
    )
    {
        var pixelType = ToTwainPixelType(options.ColorMode);

        if (pixelType is not null)
        {
            TrySet(
                source.Capabilities.ICapPixelType,
                pixelType.Value
            );
        }

        TrySet(
            source.Capabilities.ICapXResolution,
            (TWFix32)options.Dpi
        );
        TrySet(
            source.Capabilities.ICapYResolution,
            (TWFix32)options.Dpi
        );

        if (options.Source == "feeder")
        {
            TrySet(
                source.Capabilities.CapFeederEnabled,
                BoolType.True
            );
        }
        else
        {
            TrySet(
                source.Capabilities.CapFeederEnabled,
                BoolType.False
            );
        }

        TrySet(
            source.Capabilities.CapDuplexEnabled,
            options.Duplex ? BoolType.True : BoolType.False
        );
        TrySet(
            source.Capabilities.CapXferCount,
            1
        );
    }

    private static DataSource? FindSource(
        TwainSession session,
        string deviceId
    )
    {
        return session
            .GetSources()
            .FirstOrDefault(source => ToDeviceId(source) == deviceId);
    }

    private static ScannerDevice ToScannerDevice(
        DataSource source
    )
    {
        var status = ScannerStatus.Ready;
        var capabilities = DefaultCapabilities;

        try
        {
            var openResult = source.Open();

            if (openResult == ReturnCode.Success)
            {
                try
                {
                    capabilities = ReadCapabilities(source);
                }
                finally
                {
                    source.Close();
                }
            }
            else
            {
                status = ScannerStatus.Unknown;
            }
        }
        catch
        {
            status = ScannerStatus.Unknown;
        }

        return new ScannerDevice(
            Id: ToDeviceId(source),
            Name: source.Name,
            Provider: "twain",
            Status: status,
            Capabilities: capabilities
        );
    }

    private static ScannerCapabilities ReadCapabilities(
        DataSource source
    )
    {
        var capabilities = source.Capabilities;

        return new ScannerCapabilities(
            Resolutions: ReadResolutions(capabilities),
            ColorModes: ReadColorModes(capabilities),
            Sources: ReadSources(capabilities),
            Formats: ReadFormats(capabilities),
            Duplex: ReadDuplex(capabilities)
        );
    }

    private static IReadOnlyList<int> ReadResolutions(
        ICapabilities capabilities
    )
    {
        var resolutions = ReadValues(capabilities.ICapXResolution)
            .Select(value => (int)Math.Round((double)value))
            .Where(value => value > 0)
            .Distinct()
            .Order()
            .ToList();

        return resolutions.Count > 0
            ? resolutions
            : DefaultCapabilities.Resolutions;
    }

    private static IReadOnlyList<string> ReadColorModes(
        ICapabilities capabilities
    )
    {
        var colorModes = ReadValues(capabilities.ICapPixelType)
            .Select(ToColorMode)
            .Where(value => value is not null)
            .Select(value => value!)
            .Distinct()
            .ToList();

        return colorModes.Count > 0
            ? colorModes
            : DefaultCapabilities.ColorModes;
    }

    private static IReadOnlyList<string> ReadSources(
        ICapabilities capabilities
    )
    {
        var sources = new List<string> { "flatbed" };

        if (ReadValues(capabilities.CapFeederEnabled).Count > 0
            || ReadValues(capabilities.CapFeederLoaded).Count > 0
            || ReadValues(capabilities.ICapFeederType).Count > 0)
        {
            sources.Add("feeder");
        }

        return sources;
    }

    private static IReadOnlyList<string> ReadFormats(
        ICapabilities capabilities
    )
    {
        var formats = ReadValues(capabilities.ICapImageFileFormat)
            .Select(ToScanFormat)
            .Where(value => value is not null)
            .Select(value => value!)
            .Distinct()
            .ToList();

        return formats.Count > 0
            ? formats
            : DefaultCapabilities.Formats;
    }

    private static bool ReadDuplex(
        ICapabilities capabilities
    )
    {
        return ReadValues(capabilities.CapDuplex)
            .Any(value => value is Duplex.OnePass or Duplex.TwoPass);
    }

    private static IReadOnlyList<T> ReadValues<T>(
        IReadOnlyCapWrapper<T> capability
    )
    {
        if (!capability.IsSupported || !capability.CanGet)
        {
            return [];
        }

        try
        {
            return capability
                .GetValues()
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    private static string ToDeviceId(
        DataSource source
    )
    {
        return $"twain:{source.Id}";
    }

    private static string? ToColorMode(
        PixelType pixelType
    )
    {
        return pixelType switch
        {
            PixelType.BlackWhite => "black-white",
            PixelType.Gray => "grayscale",
            PixelType.RGB or PixelType.SRGB => "color",
            _ => null
        };
    }

    private static string? ToScanFormat(
        FileFormat fileFormat
    )
    {
        return fileFormat switch
        {
            FileFormat.Pdf or FileFormat.PdfA or FileFormat.PdfA2 => "pdf",
            FileFormat.Png => "png",
            FileFormat.Jfif => "jpeg",
            _ => null
        };
    }

    private static PixelType? ToTwainPixelType(
        string colorMode
    )
    {
        return colorMode switch
        {
            "black-white" => PixelType.BlackWhite,
            "grayscale" => PixelType.Gray,
            "color" => PixelType.RGB,
            _ => null
        };
    }

    private static FileFormat? ToTwainFileFormat(
        string format
    )
    {
        return format switch
        {
            "pdf" => FileFormat.Pdf,
            "png" => FileFormat.Png,
            "jpeg" => FileFormat.Jfif,
            _ => null
        };
    }

    private static string ToMimeType(
        string format
    )
    {
        return format switch
        {
            "png" => "image/png",
            "jpeg" => "image/jpeg",
            _ => "application/pdf"
        };
    }

    private static string CreateScanFilePath(
        string format
    )
    {
        var scansDirectory = Path.Combine(
            Path.GetTempPath(),
            "scanner-sdk"
        );

        Directory.CreateDirectory(scansDirectory);

        return Path.Combine(
            scansDirectory,
            $"twain-scan-{Guid.NewGuid():N}.{format}"
        );
    }

    private static void TrySet<T>(
        ICapWrapper<T> capability,
        T value
    )
    {
        if (!capability.IsSupported || !capability.CanSet)
        {
            return;
        }

        try
        {
            capability.SetValue(value);
        }
        catch
        {
        }
    }

    // Some TWAIN drivers (notably when assembling multi-page PDFs) raise
    // DataTransferred before the output file is fully flushed to disk, so the
    // file may not exist yet - or may still be growing - at this point. Poll
    // until it appears and its size stops changing between checks.
    private static async Task WaitForFileReadyAsync(
        string filePath,
        CancellationToken cancellationToken
    )
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(10);
        long previousLength = -1;

        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (File.Exists(filePath))
            {
                var currentLength = new FileInfo(filePath).Length;

                if (currentLength > 0 && currentLength == previousLength)
                {
                    return;
                }

                previousLength = currentLength;
            }

            await Task.Delay(150, cancellationToken);
        }

        throw new ScannerOperationException(
            "TWAIN_TRANSFER_FAILED",
            $"TWAIN transfer reported completion but the output file '{filePath}' was never written."
        );
    }

    private static void TryDeleteFile(
        string filePath
    )
    {
        try
        {
            File.Delete(filePath);
        }
        catch
        {
        }
    }

    private static void EnsureSuccess(
        ReturnCode returnCode,
        string code,
        string message
    )
    {
        if (returnCode != ReturnCode.Success)
        {
            throw new ScannerOperationException(
                code,
                message
            );
        }
    }
}
