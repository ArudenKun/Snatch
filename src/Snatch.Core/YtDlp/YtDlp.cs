using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using CliWrap.Builders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ZLinq;

namespace Snatch.Core.YtDlp;

public sealed class YtDlp
{
    private readonly string _ytDlpPath;
    private ArgumentsBuilder _commandBuilder;
    private readonly ProgressParser _progressParser;
    private readonly ILogger _logger;
    private string _format = "best";
    private string _outputFolder = ".";
    private string? _outputTemplate;

    // Events for progress and status updates
    public event Action<string>? OnProgress;
    public event Action<string>? OnError;
    public event Action<bool, string>? OnCommandCompleted;
    public event EventHandler<string>? OnOutputMessage;
    public event EventHandler<DownloadProgressEventArgs>? OnProgressDownload;
    public event EventHandler<string>? OnCompleteDownload;
    public event EventHandler<string>? OnProgressMessage;
    public event EventHandler<string>? OnErrorMessage;
    public event Action<object, string>? OnPostProcessingComplete;

    #region Options

    private static readonly HashSet<string> GeneralOptions =
    [
        "--format",
        "--output",
        "-o",
        "--no-overwrites",
        "--continue",
        "--no-continue",
        "--ignore-errors",
        "--no-part",
        "--no-mtime",
        "--write-description",
        "--write-info-json",
        "--write-annotations",
        "--write-thumbnail",
        "--write-all-thumbnails",
        "--write-sub",
        "--write-auto-sub",
        "--sub-format",
        "--sub-langs",
        "--skip-download",
        "--no-playlist",
        "--yes-playlist",
        "--playlist-items",
        "--playlist-start",
        "--playlist-end",
        "--match-title",
        "--reject-title",
        "--no-check-certificate",
        "--user-agent",
        "--referer",
        "--cookies",
        "--add-header",
        "--limit-rate",
        "--retries",
        "--fragment-retries",
        "--timeout",
        "--source-address",
        "--force-ipv4",
        "--force-ipv6",
    ];

    private static readonly HashSet<string> AuthenticationOptions =
    [
        "--username",
        "--password",
        "--twofactor",
        "--netrc",
        "--netrc-location",
        "--video-password",
    ];

    private static readonly HashSet<string> NetworkOptions =
    [
        "--proxy",
        "--geo-bypass",
        "--geo-bypass-country",
        "--geo-bypass-ip-block",
        "--no-geo-bypass",
    ];

    private static readonly HashSet<string> DownloadArchiveOptions =
    [
        "--download-archive",
        "--max-downloads",
        "--min-filesize",
        "--max-filesize",
        "--date",
        "--datebefore",
        "--dateafter",
        "--match-filter",
    ];

    private static readonly HashSet<string> PostProcessingOptions =
    [
        "--extract-audio",
        "--audio-format",
        "--audio-quality",
        "--recode-video",
        "--postprocessor-args",
        "--embed-subs",
        "--embed-thumbnail",
        "--embed-metadata",
        "--embed-chapters",
        "--embed-info-json",
        "--convert-subs",
        "--merge-output-format",
    ];

    private static readonly HashSet<string> SubtitleThumbnailOptions =
    [
        "--write-sub",
        "--write-auto-sub",
        "--sub-lang",
        "--sub-format",
        "--write-thumbnail",
        "--write-all-thumbnails",
        "--convert-subs",
        "--embed-subs",
        "--embed-thumbnail",
    ];

    private static readonly HashSet<string> DebugOptions =
    [
        "--simulate",
        "--skip-download",
        "--print",
        "--quiet",
        "--no-warnings",
        "--verbose",
        "--dump-json",
        "--force-write-archive",
        "--no-progress",
        "--newline",
        "--write-log",
    ];

    private static readonly HashSet<string> AdvancedOptions =
    [
        "--download-sections",
        "--concat-playlist",
        "--replace-in-metadata",
        "--call-home",
        "--write-pages",
        "--sleep-interval",
        "--max-sleep-interval",
        "--min-sleep-interval",
        "--sleep-subtitles",
        "--write-link",
        "--live-from-start",
        "--no-live-from-start",
        "--no-ads",
        "--force-keyframes-at-cuts",
        "--remux-video",
        "--no-color",
        "--paths",
        "--output-na-placeholder",
        "--playlist-random",
        "--sponsorblock-mark",
        "--sponsorblock-remove",
        "--sponsorblock-chapter-title",
    ];

    private static readonly HashSet<string> OthersOptions =
    [
        "--config-location",
        "--write-video",
        "--write-audio",
        "--no-post-overwrites",
        "--break-on-existing",
        "--break-per-input",
        "--windows-filenames",
        "--restrict-filenames",
        "--ffmpeg-location",
        // JS Runtime Support
        "--js-runtimes",
        // EJS script dependencies
        "--remote-components",
    ];

    // Valid yt-dlp options for validation
    private static readonly HashSet<string> ValidOptions = GeneralOptions
        .AsValueEnumerable()
        .Concat(AuthenticationOptions)
        .Concat(NetworkOptions)
        .Concat(DownloadArchiveOptions)
        .Concat(PostProcessingOptions)
        .Concat(SubtitleThumbnailOptions)
        .Concat(DebugOptions)
        .Concat(AdvancedOptions)
        .Concat(OthersOptions)
        .ToHashSet();

    #endregion

    public YtDlp(string ytDlpPath = "yt-dlp", ILogger? logger = null)
    {
        _ytDlpPath = ValidatePath(ytDlpPath);
        if (!File.Exists(_ytDlpPath) && !IsInPath(_ytDlpPath))
            throw new YtDlpException(
                $"yt-dlp executable not found at {_ytDlpPath}. Install yt-dlp or specify a valid path."
            );
        _commandBuilder = new ArgumentsBuilder();
        _progressParser = new ProgressParser(logger);
        _logger = logger ?? NullLogger.Instance;

        // Subscribe to progress parser events
        _progressParser.OnOutputMessage += (_, e) => OnOutputMessage?.Invoke(this, e);
        _progressParser.OnProgressDownload += (_, e) => OnProgressDownload?.Invoke(this, e);
        _progressParser.OnCompleteDownload += (_, e) => OnCompleteDownload?.Invoke(this, e);
        _progressParser.OnProgressMessage += (_, e) => OnProgressMessage?.Invoke(this, e);
        _progressParser.OnErrorMessage += (_, e) => OnErrorMessage?.Invoke(this, e);
        _progressParser.OnPostProcessingComplete += (_, e) =>
            OnPostProcessingComplete?.Invoke(this, e);
    }

    #region Command Building Methods

    public YtDlp Version()
    {
        _commandBuilder.Add("--version");
        return this;
    }

    public YtDlp Update()
    {
        _commandBuilder.Add("--update");
        return this;
    }

    public YtDlp ExtractAudio(string audioFormat)
    {
        if (string.IsNullOrWhiteSpace(audioFormat))
            throw new ArgumentException("Audio format cannot be empty.", nameof(audioFormat));
        _commandBuilder.Add($"--extract-audio --audio-format {SanitizeInput(audioFormat)} ");
        return this;
    }

    public YtDlp EmbedMetadata()
    {
        _commandBuilder.Add("--embed-metadata");
        return this;
    }

    public YtDlp EmbedThumbnail()
    {
        _commandBuilder.Add("--embed-thumbnail");
        return this;
    }

    public YtDlp SetOutputTemplate(string template)
    {
        if (string.IsNullOrWhiteSpace(template))
            throw new ArgumentException("Output template cannot be empty.", nameof(template));
        _outputTemplate = template.Replace("\\", "/").Trim();
        return this;
    }

    public YtDlp SelectPlaylistItems(string items)
    {
        if (string.IsNullOrWhiteSpace(items))
            throw new ArgumentException("Playlist items cannot be empty.", nameof(items));
        _commandBuilder.Add(["--playlist-items", $"{SanitizeInput(items)}"]);
        return this;
    }

    public YtDlp SetDownloadRate(string rate)
    {
        if (string.IsNullOrWhiteSpace(rate))
            throw new ArgumentException("Download rate cannot be empty.", nameof(rate));
        _commandBuilder.Add(["--limit-rate", $"{SanitizeInput(rate)}"]);
        return this;
    }

    public YtDlp UseProxy(string proxy)
    {
        if (string.IsNullOrWhiteSpace(proxy))
            throw new ArgumentException("Proxy URL cannot be empty.", nameof(proxy));
        _commandBuilder.Add($"--proxy {SanitizeInput(proxy)}");
        return this;
    }

    public YtDlp Simulate()
    {
        _commandBuilder.Add("--simulate");
        return this;
    }

    public YtDlp WriteMetadataToJson()
    {
        _commandBuilder.Add("--write-info-json");
        return this;
    }

    public YtDlp DownloadSubtitles(string languages = "all")
    {
        if (string.IsNullOrWhiteSpace(languages))
            throw new ArgumentException("Languages cannot be empty.", nameof(languages));
        _commandBuilder.Add(["--write-subs", "--sub-langs", $"{SanitizeInput(languages)}"]);
        return this;
    }

    public YtDlp SetFormat(string format)
    {
        _format = format;
        return this;
    }

    public YtDlp DownloadThumbnails()
    {
        _commandBuilder.Add("--write-thumbnail");
        return this;
    }

    public YtDlp DownloadLivestream(bool fromStart = true)
    {
        _commandBuilder.Add(fromStart ? "--live-from-start " : "--no-live-from-start");
        return this;
    }

    public YtDlp SetRetries(string retries)
    {
        if (string.IsNullOrWhiteSpace(retries))
            throw new ArgumentException("Retries cannot be empty.", nameof(retries));
        _commandBuilder.Add(["--retries", $"{SanitizeInput(retries)}"]);
        return this;
    }

    public YtDlp DownloadSections(string timeRanges)
    {
        if (string.IsNullOrWhiteSpace(timeRanges))
            throw new ArgumentException("Time ranges cannot be empty.", nameof(timeRanges));
        _commandBuilder.Add(["--download-sections", $"{SanitizeInput(timeRanges)}"]);
        return this;
    }

    public YtDlp ConcatenateVideos()
    {
        _commandBuilder.Add("--concat-playlist always");
        return this;
    }

    public YtDlp ReplaceMetadata(string field, string regex, string replacement)
    {
        if (
            string.IsNullOrWhiteSpace(field)
            || string.IsNullOrWhiteSpace(regex)
            || replacement == null
        )
            throw new ArgumentException("Metadata field, regex, and replacement cannot be empty.");
        _commandBuilder.Add([
            "--replace-in-metadata",
            $"{SanitizeInput(field)} {SanitizeInput(regex)} {SanitizeInput(replacement)}",
        ]);
        return this;
    }

    public YtDlp SkipDownloaded()
    {
        _commandBuilder.Add("--download-archive downloaded.txt");
        return this;
    }

    public YtDlp SetUserAgent(string userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            throw new ArgumentException("User agent cannot be empty.", nameof(userAgent));
        _commandBuilder.Add(["--user-agent", $"{SanitizeInput(userAgent)}"]);
        return this;
    }

    public YtDlp LogToFile(string logFile)
    {
        if (string.IsNullOrWhiteSpace(logFile))
            throw new ArgumentException("Log file path cannot be empty.", nameof(logFile));
        _commandBuilder.Add(["--write-log", $"{SanitizeInput(logFile)}"]);
        return this;
    }

    public YtDlp UseCookies(string cookieFile)
    {
        if (string.IsNullOrWhiteSpace(cookieFile))
            throw new ArgumentException("Cookie file path cannot be empty.", nameof(cookieFile));
        _commandBuilder.Add($"--cookies {SanitizeInput(cookieFile)} ");
        return this;
    }

    public YtDlp SetReferer(string referer)
    {
        if (string.IsNullOrWhiteSpace(referer))
            throw new ArgumentException("Referer URL cannot be empty.", nameof(referer));
        _commandBuilder.Add($"--referer {SanitizeInput(referer)} ");
        return this;
    }

    public YtDlp MergePlaylistIntoSingleVideo(string format)
    {
        if (string.IsNullOrWhiteSpace(format))
            throw new ArgumentException("Format cannot be empty.", nameof(format));
        _commandBuilder.Add($"--merge-output-format {SanitizeInput(format)} ");
        return this;
    }

    public YtDlp SetCustomHeader(string header, string value)
    {
        if (string.IsNullOrWhiteSpace(header) || string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Header and value cannot be empty.");
        _commandBuilder.Add($"--add-header \"{SanitizeInput(header)}:{SanitizeInput(value)}\" ");
        return this;
    }

    public YtDlp SetResolution(string resolution)
    {
        if (string.IsNullOrWhiteSpace(resolution))
            throw new ArgumentException("Resolution cannot be empty.", nameof(resolution));
        _commandBuilder.Add($"--format \"bestvideo[height<={SanitizeInput(resolution)}]\" ");
        return this;
    }

    public YtDlp ExtractMetadataOnly()
    {
        _commandBuilder.Add("--dump-json ");
        return this;
    }

    public YtDlp DownloadAudioAndVideoSeparately()
    {
        _commandBuilder.Add("--write-video --write-audio ");
        return this;
    }

    public YtDlp PostProcessFiles(string operation)
    {
        if (string.IsNullOrWhiteSpace(operation))
            throw new ArgumentException("Operation cannot be empty.", nameof(operation));
        _commandBuilder.Add($"--postprocessor-args \"{SanitizeInput(operation)}\" ");
        return this;
    }

    public YtDlp SetKeepTempFiles(bool keep)
    {
        if (keep)
            _commandBuilder.Add(" -k");
        return this;
    }

    public YtDlp SetDownloadTimeout(string timeout)
    {
        if (string.IsNullOrWhiteSpace(timeout))
            throw new ArgumentException("Timeout cannot be empty.", nameof(timeout));
        _commandBuilder.Add($"--download-timeout {SanitizeInput(timeout)} ");
        return this;
    }

    public YtDlp SetAuthentication(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Username and password cannot be empty.");
        _commandBuilder.Add(
            $"--username {SanitizeInput(username)} --password {SanitizeInput(password)} "
        );
        return this;
    }

    public YtDlp SetOutputFolder([Required] string folderPath)
    {
        _outputFolder = folderPath;
        return this;
    }

    public YtDlp DisableAds()
    {
        _commandBuilder.Add("--no-ads ");
        return this;
    }

    public YtDlp DownloadLiveStreamRealTime()
    {
        _commandBuilder.Add("--live-from-start --recode-video mp4 ");
        return this;
    }

    public YtDlp AddCustomCommand(string customCommand)
    {
        if (string.IsNullOrWhiteSpace(customCommand))
            throw new ArgumentException("Custom command cannot be empty.", nameof(customCommand));

        var commandParts = customCommand.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (commandParts.Length == 0 || !ValidOptions.Contains(SanitizeInput(commandParts[0])))
        {
            var errorMessage = $"Invalid option: {customCommand}";
            OnError?.Invoke(errorMessage);
            _logger.LogError(errorMessage);
            return this;
        }

        _commandBuilder.Add($"{SanitizeInput(customCommand)} ");
        return this;
    }

    public YtDlp SetTimeout(TimeSpan timeout)
    {
        if (timeout.TotalSeconds <= 0)
            throw new ArgumentException("Timeout must be greater than zero.", nameof(timeout));
        _commandBuilder.Add($"--timeout {timeout.TotalSeconds} ");
        return this;
    }

    #endregion

    #region Execution Methods

    public string PreviewCommand()
    {
        return _commandBuilder.ToString().Trim();
    }

    public async Task<string> GetVersionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var process = CreateProcess($"--version");

            process.Start();

            // Read standard output asynchronously
            var readOutputTask = process.StandardOutput.ReadToEndAsync();

            // Wait for process to exit, observing cancellation
            using (
                cancellationToken.Register(() =>
                {
                    try
                    {
                        if (!process.HasExited)
                        {
                            process.Kill(true); // forcefully kill process
                        }
                    }
                    catch
                    {
                        /* ignore if already exited */
                    }
                })
            )
            {
                await process.WaitForExitAsync(cancellationToken);
            }

            var output = await readOutputTask;

            string version = output.Trim();
            _logger.Log(LogLevel.Information, $"yt-dlp version: {version}");
            return version;
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, $"Error getting yt-dlp version: {ex.Message}");
            return string.Empty;
        }
    }

    public async Task<string> UpdateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var process = CreateProcess("-U");

            process.Start();

            // Read output and error concurrently
            var readOutputTask = process.StandardOutput.ReadToEndAsync();
            var readErrorTask = process.StandardError.ReadToEndAsync();

            using (
                cancellationToken.Register(() =>
                {
                    try
                    {
                        if (!process.HasExited)
                            process.Kill(true);
                    }
                    catch
                    {
                        // ignored
                    }
                })
            )
            {
                await process.WaitForExitAsync(cancellationToken);
            }

            var output = await readOutputTask;
            var error = await readErrorTask;

            // Log both
            if (!string.IsNullOrWhiteSpace(output))
                _logger.Log(LogLevel.Information, output.Trim());
            if (!string.IsNullOrWhiteSpace(error))
                _logger.Log(LogLevel.Error, error.Trim());

            // Analyze output for professional messages
            if (output.Contains("Updated", StringComparison.OrdinalIgnoreCase))
                return "yt-dlp was successfully updated to the latest version.";

            if (output.Contains("up to date", StringComparison.OrdinalIgnoreCase))
                return "yt-dlp is already up to date.";

            return "yt-dlp update check completed (no changes detected).";
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, $"Error updating yt-dlp: {ex.Message}");
            return $"yt-dlp update failed: {ex.Message}";
        }
    }

    public async Task<Metadata?> GetVideoMetadataJsonAsync(
        string url,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL cannot be empty.", nameof(url));

        try
        {
            var process = CreateProcess($"--dump-json {SanitizeInput(url)}");

            process.Start();

            // Read standard output asynchronously
            var readOutputTask = process.StandardOutput.ReadToEndAsync();

            // Drain stderr so it never blocks (we don't await unless needed)
            //var readErrorTask = Task.Run(() => process.StandardError.ReadToEndAsync());

            // Wait for process to exit, observing cancellation
            using (
                cancellationToken.Register(() =>
                {
                    try
                    {
                        if (!process.HasExited)
                            process.Kill(true); // forcefully kill process
                    }
                    catch
                    {
                        /* ignore if already exited */
                    }
                })
            )
            {
                await process.WaitForExitAsync(cancellationToken);
            }

            // Get stdout result
            var output = await readOutputTask;

            // Optionally capture errors for logging
            //var errorOutput = await readErrorTask;
            //if (!string.IsNullOrWhiteSpace(errorOutput))
            //    _logger.Log(LogType.Debug, $"yt-dlp stderr: {errorOutput}");

            _logger.Log(LogLevel.Information, $"Get Format: {output}");

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<Metadata>(output, options);
        }
        catch (OperationCanceledException)
        {
            _logger.Log(LogLevel.Warning, "Format fetching cancelled by user.");
            throw;
        }
        catch (Exception ex)
        {
            var errorMessage = $"Failed to fetch available formats: {ex.Message}";
            _logger.Log(LogLevel.Error, errorMessage);
            throw new YtDlpException(errorMessage, ex);
        }
    }

    public async Task<List<VideoFormat>> GetAvailableFormatsAsync(
        string videoUrl,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(videoUrl))
            throw new ArgumentException("Video URL cannot be empty.", nameof(videoUrl));

        try
        {
            var process = CreateProcess($"-F {SanitizeInput(videoUrl)}");

            process.Start();

            // Read standard output asynchronously
            var readOutputTask = process.StandardOutput.ReadToEndAsync();

            // Wait for process to exit, observing cancellation
            using (
                cancellationToken.Register(() =>
                {
                    try
                    {
                        if (!process.HasExited)
                        {
                            process.Kill(true); // forcefully kill process
                        }
                    }
                    catch
                    {
                        /* ignore if already exited */
                    }
                })
            )
            {
                await process.WaitForExitAsync(cancellationToken);
            }

            var output = await readOutputTask;

            _logger.Log(LogLevel.Information, $"Get Format: {output}");
            return ParseFormats(output);
        }
        catch (OperationCanceledException)
        {
            _logger.Log(LogLevel.Warning, "Format fetching cancelled by user.");
            throw;
        }
        catch (Exception ex)
        {
            var errorMessage = $"Failed to fetch available formats: {ex.Message}";
            _logger.Log(LogLevel.Error, errorMessage);
            throw new YtDlpException(errorMessage, ex);
        }
    }

    public async Task ExecuteAsync(
        string url,
        CancellationToken cancellationToken = default,
        string? outputTemplate = null
    )
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL cannot be empty.", nameof(url));

        // Ensure output folder exists
        try
        {
            Directory.CreateDirectory(_outputFolder);
            _logger.Log(LogLevel.Information, $"Output folder: {Path.GetFullPath(_outputFolder)}");
        }
        catch (Exception ex)
        {
            _logger.Log(
                LogLevel.Error,
                $"Failed to create output folder {_outputFolder}: {ex.Message}"
            );
            throw new YtDlpException($"Failed to create output folder {_outputFolder}", ex);
        }

        // Reset ProgressParser for this download
        _progressParser.Reset();
        _logger.Log(LogLevel.Information, $"Starting download for URL: {url}");

        // Use provided template or default
        string template =
            Path.Combine(_outputFolder, _outputTemplate?.Replace("\\", "/")!)
            ?? Path.Combine(_outputFolder, "%(title)s.%(ext)s").Replace("\\", "/");

        // Build command with format and output template

        _commandBuilder.Add(["-f", $"{_format}", "-o", $"{template}", $"{SanitizeInput(url)}"]);
        string arguments = _commandBuilder.Build();
        _commandBuilder = new ArgumentsBuilder();

        await RunYtdlpAsync(arguments, cancellationToken);
    }

    public async Task ExecuteBatchAsync(
        IEnumerable<string> urls,
        CancellationToken cancellationToken = default
    )
    {
        if (urls == null || !urls.Any())
        {
            _logger.Log(LogLevel.Error, "No URLs provided for batch download");
            throw new YtDlpException("No URLs provided for batch download");
        }

        // Ensure output folder exists
        try
        {
            Directory.CreateDirectory(_outputFolder);
            _logger.Log(
                LogLevel.Information,
                $"Output folder for batch: {Path.GetFullPath(_outputFolder)}"
            );
        }
        catch (Exception ex)
        {
            _logger.Log(
                LogLevel.Error,
                $"Failed to create output folder {_outputFolder}: {ex.Message}"
            );
            throw new YtDlpException($"Failed to create output folder {_outputFolder}", ex);
        }

        foreach (var url in urls)
        {
            try
            {
                await ExecuteAsync(url, cancellationToken);
            }
            catch (YtDlpException ex)
            {
                _logger.Log(LogLevel.Error, $"Skipping URL {url} due to error: {ex.Message}");
                continue; // Continue with next URL
            }
        }
    }

    public async Task ExecuteBatchAsync(
        IEnumerable<string> urls,
        int maxConcurrency = 3,
        CancellationToken cancellationToken = default
    )
    {
        if (urls == null || !urls.Any())
        {
            _logger.Log(LogLevel.Error, "No URLs provided for batch download");
            throw new YtDlpException("No URLs provided for batch download");
        }

        try
        {
            Directory.CreateDirectory(_outputFolder);
            _logger.Log(
                LogLevel.Information,
                $"Output folder for batch: {Path.GetFullPath(_outputFolder)}"
            );
        }
        catch (Exception ex)
        {
            _logger.Log(
                LogLevel.Error,
                $"Failed to create output folder {_outputFolder}: {ex.Message}"
            );
            throw new YtDlpException($"Failed to create output folder {_outputFolder}", ex);
        }

        using SemaphoreSlim throttler = new(maxConcurrency);

        var tasks = urls.Select(async url =>
        {
            await throttler.WaitAsync();
            try
            {
                await ExecuteAsync(url, cancellationToken);
            }
            catch (YtDlpException ex)
            {
                _logger.Log(LogLevel.Error, $"Skipping URL {url} due to error: {ex.Message}");
            }
            finally
            {
                throttler.Release();
            }
        });

        await Task.WhenAll(tasks);
    }

    #endregion

    #region Private Helpers

    private async Task RunYtdlpAsync(
        string arguments,
        CancellationToken cancellationToken = default
    )
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _ytDlpPath,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = new Process { StartInfo = startInfo };

        try
        {
            if (!process.Start())
                throw new YtDlpException("Failed to start yt-dlp process.");

            // Register cancellation
            using var registration = cancellationToken.Register(() =>
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill(entireProcessTree: true);
                        _logger.Log(LogLevel.Warning, "yt-dlp process killed due to cancellation.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.Log(LogLevel.Error, $"Error killing process: {ex.Message}");
                }
            });

            // Read output and errors concurrently
            var outputTask = Task.Run(
                async () =>
                {
                    using var reader = process.StandardOutput;
                    string? output;
                    while ((output = await reader.ReadLineAsync()) != null)
                    {
                        cancellationToken.ThrowIfCancellationRequested(); // <- required
                        _progressParser.ParseProgress(output);
                        OnProgress?.Invoke(output);
                    }
                },
                cancellationToken
            );

            var errorTask = Task.Run(
                async () =>
                {
                    using var errorReader = process.StandardError;
                    string? errorOutput;
                    while ((errorOutput = await errorReader.ReadLineAsync()) != null)
                    {
                        cancellationToken.ThrowIfCancellationRequested(); // <- required
                        OnErrorMessage?.Invoke(this, errorOutput);
                        OnError?.Invoke(errorOutput);
                        _logger.Log(LogLevel.Error, errorOutput);
                    }
                },
                cancellationToken
            );

            await Task.WhenAll(outputTask, errorTask);
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync();
                throw new YtDlpException(
                    $"yt-dlp command failed with exit code {process.ExitCode}: {error}"
                );
            }

            var success = process.ExitCode == 0;
            var message = success
                ? "Process completed successfully."
                : $"Process failed with exit code {process.ExitCode}.";
            OnCommandCompleted?.Invoke(success, message);
            _logger.Log(success ? LogLevel.Information : LogLevel.Error, message);
        }
        catch (OperationCanceledException)
        {
            throw; // Let your caller handle this
        }
        catch (Exception ex)
        {
            var errorMessage = $"Error executing yt-dlp: {ex.Message}";
            OnError?.Invoke(errorMessage);
            _logger.Log(LogLevel.Error, errorMessage);
            throw new YtDlpException(errorMessage, ex);
        }
    }

    private Process CreateProcess(string arguments)
    {
        return new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = _ytDlpPath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
            EnableRaisingEvents = true,
        };
    }

    private List<VideoFormat> ParseFormats(string result)
    {
        var formats = new List<VideoFormat>();
        if (string.IsNullOrWhiteSpace(result))
        {
            _logger.Log(LogLevel.Warning, "Empty or null yt-dlp output");
            return formats;
        }

        var lines = result.Split(['\n'], StringSplitOptions.RemoveEmptyEntries);
        bool isFormatSection = false;

        foreach (var line in lines)
        {
            _logger.Log(LogLevel.Debug, $"Parsing line: {line}");

            // Detect format section start
            if (line.Contains("[info] Available formats"))
            {
                isFormatSection = true;
                continue;
            }

            // Skip header or separator lines
            if (!isFormatSection || line.Contains("RESOLUTION") || line.StartsWith("---"))
            {
                continue;
            }

            // Skip empty or invalid lines (basic check for format line structure)
            if (!Regex.IsMatch(line, @"^[^\s]+\s+[^\s]+"))
            {
                _logger.Log(LogLevel.Debug, $"Stopping format parsing at non-format line: {line}");
                break;
            }

            // Split line by whitespace, preserving structure
            var parts = line.Split([' '], StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                _logger.Log(LogLevel.Warning, $"Skipping line (too few parts): {line}");
                continue;
            }

            var format = new VideoFormat();
            int index = 0;

            try
            {
                // Parse ID
                format.ID = parts[index++];

                // Check for duplicate ID
                if (formats.Any(f => f.ID == format.ID))
                {
                    _logger.Log(LogLevel.Warning, $"Skipping duplicate format ID: {format.ID}");
                    continue;
                }

                // Parse Extension
                format.Extension = parts[index++];

                // Parse Resolution (may include "audio only")
                if (
                    index < parts.Length
                    && parts[index] == "audio"
                    && index + 1 < parts.Length
                    && parts[index + 1] == "only"
                )
                {
                    format.Resolution = "audio only";
                    index += 2;
                }
                else if (index < parts.Length)
                {
                    format.Resolution = parts[index++];
                }
                else
                {
                    _logger.Log(LogLevel.Warning, $"Skipping line (missing resolution): {line}");
                    continue;
                }

                // Parse FPS (empty for audio-only formats)
                if (
                    format.Resolution != "audio only"
                    && index < parts.Length
                    && Regex.IsMatch(parts[index], @"^\d+$")
                )
                {
                    format.FPS = parts[index++];
                }

                // Parse Channels (marked by '|' or number)
                if (
                    index < parts.Length
                    && (
                        Regex.IsMatch(parts[index], @"^\d+\|$")
                        || Regex.IsMatch(parts[index], @"^\d+$")
                    )
                )
                {
                    format.Channels = parts[index].TrimEnd('|');
                    index++;
                }

                // Skip first '|' if present
                if (index < parts.Length && parts[index] == "|")
                {
                    index++;
                }

                // Parse FileSize
                if (
                    index < parts.Length
                    && (Regex.IsMatch(parts[index], @"^~?\d+\.\d+MiB$") || parts[index] == "")
                )
                {
                    format.FileSize = parts[index] == "" ? null : parts[index];
                    index++;
                }

                // Parse TBR
                if (index < parts.Length && Regex.IsMatch(parts[index], @"^\d+k$"))
                {
                    format.TBR = parts[index];
                    index++;
                }

                // Parse Protocol
                if (
                    index < parts.Length
                    && (
                        parts[index] == "https" || parts[index] == "m3u8" || parts[index] == "mhtml"
                    )
                )
                {
                    format.Protocol = parts[index];
                    index++;
                }

                // Skip second '|' if present
                if (index < parts.Length && parts[index] == "|")
                {
                    index++;
                }

                // Parse VCodec
                if (index < parts.Length)
                {
                    if (
                        parts[index] == "audio"
                        && index + 1 < parts.Length
                        && parts[index + 1] == "only"
                    )
                    {
                        format.VCodec = "audio only";
                        index += 2;
                    }
                    else if (parts[index] == "images")
                    {
                        format.VCodec = "images";
                        index++;
                    }
                    else if (Regex.IsMatch(parts[index], @"^[a-zA-Z0-9\.]+$"))
                    {
                        format.VCodec = parts[index];
                        index++;
                    }
                }

                // Parse VBR
                if (index < parts.Length && Regex.IsMatch(parts[index], @"^\d+k$"))
                {
                    format.VBR = parts[index];
                    index++;
                }

                // Parse ACodec
                if (
                    index < parts.Length
                    && (
                        Regex.IsMatch(parts[index], @"^[a-zA-Z0-9\.]+$")
                        || parts[index] == "unknown"
                    )
                )
                {
                    format.ACodec = parts[index];
                    index++;
                }

                // Parse ABR
                if (index < parts.Length && Regex.IsMatch(parts[index], @"^\d+k$"))
                {
                    format.ABR = parts[index];
                    index++;
                }

                // Parse ASR
                if (index < parts.Length && Regex.IsMatch(parts[index], @"^\d+k$"))
                {
                    format.ASR = parts[index];
                    index++;
                }

                // Parse MoreInfo (remaining parts)
                if (index < parts.Length)
                {
                    format.MoreInfo = string.Join(" ", parts.Skip(index)).Trim();
                    // Clean up MoreInfo to remove redundant parts
                    if (format.MoreInfo.StartsWith("|"))
                    {
                        format.MoreInfo = format.MoreInfo.Substring(1).Trim();
                    }

                    // For storyboards, ensure MoreInfo is 'storyboard' and ACodec is null
                    if (format.VCodec == "images" && format.MoreInfo != "storyboard")
                    {
                        format.ACodec = null;
                        format.MoreInfo = "storyboard";
                    }
                }

                formats.Add(format);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Warning, $"Failed to parse line '{line}': {ex.Message}");
                continue;
            }
        }

        _logger.Log(LogLevel.Information, $"Parsed {formats.Count} formats");
        return formats;
    }

    private bool IsInPath(string executable)
    {
        var paths = Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator) ?? [];
        return paths.Any(path => File.Exists(Path.Combine(path, executable)));
    }

    private static string ValidatePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("yt-dlp path cannot be empty.", nameof(path));
        return path;
    }

    private static string SanitizeInput(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;
        // Escape quotes and other potentially dangerous characters
        return input.Replace("\"", "\\\"").Replace("`", "\\`");
    }

    #endregion
}
