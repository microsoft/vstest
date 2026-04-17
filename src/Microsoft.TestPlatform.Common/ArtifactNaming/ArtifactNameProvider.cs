// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

using Microsoft.VisualStudio.TestPlatform.ObjectModel.ArtifactNaming;

namespace Microsoft.VisualStudio.TestPlatform.Common.ArtifactNaming;

/// <summary>
/// Default implementation of <see cref="IArtifactNameProvider"/>.
/// Resolves artifact name templates to concrete, sanitized file paths.
/// </summary>
internal sealed class ArtifactNameProvider : IArtifactNameProvider
{
    /// <summary>
    /// Name of the marker file created in per-run directories to claim ownership.
    /// </summary>
    internal const string MarkerFileName = ".vstest-run";

    /// <summary>
    /// Characters that are invalid in file names on Windows (superset of Linux restrictions).
    /// Manually listed to produce cross-platform-safe file names even when running on Linux.
    /// </summary>
    private static readonly HashSet<char> InvalidFileNameChars = new()
    {
        '\"', '<', '>', '|', '\0',
        (char)1, (char)2, (char)3, (char)4, (char)5, (char)6, (char)7, (char)8, (char)9, (char)10,
        (char)11, (char)12, (char)13, (char)14, (char)15, (char)16, (char)17, (char)18, (char)19, (char)20,
        (char)21, (char)22, (char)23, (char)24, (char)25, (char)26, (char)27, (char)28, (char)29, (char)30,
        (char)31, ':', '*', '?', '\\', '/',
    };

    private static readonly Regex ReservedFileNamesRegex = new(@"(?i:^(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9]|CLOCK\$)(\..*)?)$");

    private readonly Func<DateTime> _timeProvider;
    private readonly Func<string, bool> _fileExists;
    private readonly Func<string, bool> _directoryExists;
    private readonly Action<string> _createDirectory;
    private readonly Func<string, string, bool> _tryCreateMarkerFile;

    /// <summary>
    /// Initializes a new instance using the system clock and file system.
    /// </summary>
    public ArtifactNameProvider()
        : this(() => DateTime.UtcNow, File.Exists)
    {
    }

    /// <summary>
    /// Initializes a new instance with injectable dependencies (for testing).
    /// </summary>
    internal ArtifactNameProvider(
        Func<DateTime> timeProvider,
        Func<string, bool> fileExists,
        Func<string, string, bool>? tryCreateMarkerFile = null,
        Func<string, bool>? directoryExists = null,
        Action<string>? createDirectory = null)
    {
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _fileExists = fileExists ?? throw new ArgumentNullException(nameof(fileExists));
        _tryCreateMarkerFile = tryCreateMarkerFile ?? TryCreateMarkerFileOnDisk;
        _directoryExists = directoryExists ?? Directory.Exists;
        _createDirectory = createDirectory ?? (path => Directory.CreateDirectory(path));
    }

    /// <inheritdoc />
    public ArtifactNameResult Resolve(ArtifactNameRequest request)
    {
        _ = request ?? throw new ArgumentNullException(nameof(request));

        // 1. Build the context with auto-populated tokens.
        IReadOnlyDictionary<string, string> context = EnsureAutoTokens(request.Context);

        // 2. Resolve directory.
        string directoryTemplate = request.DirectoryTemplate
            ?? ("{" + ArtifactNameTokens.TestResultsDirectory + "}");
        string directory = ExpandTemplate(directoryTemplate, context);
        directory = SanitizePathSegments(directory);

        // 3. Resolve file name.
        string fileName = ExpandTemplate(request.FileTemplate, context);
        fileName = SanitizeFileName(fileName);

        // 4. Combine and apply collision behavior.
        string extension = request.Extension ?? string.Empty;
        if (extension.Length > 0 && extension[0] != '.')
        {
            extension = "." + extension;
        }

        string basePath = Path.Combine(directory, fileName + extension);

        // 5. Ensure directory exists and determine ownership.
        bool isDirectoryOwner = EnsureDirectoryWithOwnership(directory);

        // 6. Resolve collision and detect overwrite.
        return request.Collision switch
        {
            CollisionBehavior.Overwrite => ResolveOverwrite(basePath, isDirectoryOwner),
            CollisionBehavior.Fail => ResolveFail(basePath, isDirectoryOwner),
            CollisionBehavior.AppendCounter => ResolveWithCounter(directory, fileName, extension, isDirectoryOwner),
            CollisionBehavior.AppendTimestamp => ResolveWithTimestamp(directory, fileName, extension, isDirectoryOwner),
            _ => ResolveOverwrite(basePath, isDirectoryOwner),
        };
    }

    /// <inheritdoc />
    public string ExpandTemplate(string template, IReadOnlyDictionary<string, string> context)
    {
        if (StringUtils.IsNullOrEmpty(template))
        {
            return string.Empty;
        }

        var result = new StringBuilder(template.Length * 2);
        int i = 0;

        while (i < template.Length)
        {
            char c = template[i];

            // Escaped braces.
            if (c == '{' && i + 1 < template.Length && template[i + 1] == '{')
            {
                result.Append('{');
                i += 2;
                continue;
            }

            if (c == '}' && i + 1 < template.Length && template[i + 1] == '}')
            {
                result.Append('}');
                i += 2;
                continue;
            }

            // Token start.
            if (c == '{')
            {
                int closeBrace = template.IndexOf('}', i + 1);
                if (closeBrace < 0)
                {
                    // No closing brace — keep as literal.
                    result.Append(c);
                    i++;
                    continue;
                }

                string tokenContent = template.Substring(i + 1, closeBrace - i - 1);
                string expanded = ExpandToken(tokenContent, context);
                result.Append(expanded);
                i = closeBrace + 1;
                continue;
            }

            result.Append(c);
            i++;
        }

        return result.ToString();
    }

    /// <summary>
    /// Expands a single token. Supports <c>TokenName</c> and <c>TokenName:format</c>.
    /// Unknown/unavailable tokens are kept literally as <c>{TokenName}</c>.
    /// </summary>
    private static string ExpandToken(string tokenContent, IReadOnlyDictionary<string, string> context)
    {
        // Parse format specifier: {Token:format}
        string tokenName;
        string? format = null;

        int formatIndex = tokenContent.IndexOf(':');

        if (formatIndex >= 0)
        {
            tokenName = tokenContent.Substring(0, formatIndex);
            format = tokenContent.Substring(formatIndex + 1);
        }
        else
        {
            tokenName = tokenContent;
        }

        if (context.TryGetValue(tokenName, out string? value) && value is not null)
        {
            // Apply format specifier to Timestamp/Date tokens if the value is a parsable DateTime.
            if (format is not null
                && DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime dt))
            {
                return dt.ToString(format, CultureInfo.InvariantCulture);
            }

            return value;
        }

        // Token not found — keep literal so misconfiguration is obvious.
        return "{" + tokenContent + "}";
    }

    /// <summary>
    /// Ensures that auto-populated tokens (Timestamp, Date, MachineName, UserName, Pid) are present.
    /// </summary>
    private IReadOnlyDictionary<string, string> EnsureAutoTokens(IReadOnlyDictionary<string, string> context)
    {
        var now = _timeProvider();

        var merged = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Auto tokens — only set if not already provided.
        merged[ArtifactNameTokens.Timestamp] = now.ToString("yyyyMMdd'T'HHmmss.fff", CultureInfo.InvariantCulture);
        merged[ArtifactNameTokens.Date] = now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        merged[ArtifactNameTokens.MachineName] = Environment.MachineName;
        merged[ArtifactNameTokens.UserName] = Environment.UserName;
#if NET
        merged[ArtifactNameTokens.Pid] = Environment.ProcessId.ToString(CultureInfo.InvariantCulture);
#else
        merged[ArtifactNameTokens.Pid] = System.Diagnostics.Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture);
#endif

        // User-provided context overrides auto tokens.
        foreach (KeyValuePair<string, string> kvp in context)
        {
            merged[kvp.Key] = kvp.Value;
        }

        return merged;
    }

    /// <summary>
    /// Sanitizes a file name segment by replacing invalid characters with underscores.
    /// </summary>
    internal static string SanitizeFileName(string fileName)
    {
        if (StringUtils.IsNullOrEmpty(fileName))
        {
            return fileName;
        }

        var result = new StringBuilder(fileName.Length);
        foreach (char c in fileName)
        {
            result.Append(InvalidFileNameChars.Contains(c) ? '_' : c);
        }

        string sanitized = result.ToString().TrimEnd(' ', '.');
        if (sanitized.Length == 0)
        {
            return "_";
        }

        if (ReservedFileNamesRegex.IsMatch(sanitized))
        {
            sanitized = "_" + sanitized;
        }

        return sanitized;
    }

    /// <summary>
    /// Sanitizes path segments in a directory path. Handles both forward and back slashes.
    /// Each segment is individually sanitized while preserving directory separators.
    /// </summary>
    internal static string SanitizePathSegments(string path)
    {
        if (StringUtils.IsNullOrEmpty(path))
        {
            return path;
        }

        // Normalize to forward slashes for splitting, then restore platform separator.
        string normalized = path.Replace('\\', '/');
        string[] segments = normalized.Split('/');

        var result = new StringBuilder(path.Length);
        bool first = true;

        foreach (string segment in segments)
        {
            if (!first)
            {
                result.Append(Path.DirectorySeparatorChar);
            }

            first = false;

            // Preserve drive letters (e.g., "C:") and empty segments at the start (UNC paths).
            if (segment.Length == 2 && segment[1] == ':' && char.IsLetter(segment[0]))
            {
                result.Append(segment);
                continue;
            }

            if (segment.Length == 0)
            {
                continue;
            }

            // Reject ".." traversal.
            if (segment == "..")
            {
                continue;
            }

            // Keep "." as-is (current directory).
            if (segment == ".")
            {
                result.Append(segment);
                continue;
            }

            result.Append(SanitizeFileName(segment));
        }

        return result.ToString();
    }

    private ArtifactNameResult ResolveWithCounter(string directory, string fileName, string extension, bool isDirectoryOwner)
    {
        string path = Path.Combine(directory, fileName + extension);

        if (!_fileExists(path))
        {
            return new ArtifactNameResult(path, isOverwrite: false, isDirectoryOwner);
        }

        for (int i = 2; i < 10000; i++)
        {
            string candidate = Path.Combine(directory, fileName + "_" + i.ToString(CultureInfo.InvariantCulture) + extension);
            if (!_fileExists(candidate))
            {
                return new ArtifactNameResult(candidate, isOverwrite: false, isDirectoryOwner);
            }
        }

        // Extremely unlikely: fall back to GUID suffix.
        string fallback = Path.Combine(directory, fileName + "_" + Guid.NewGuid().ToString("N").Substring(0, 8) + extension);

        return new ArtifactNameResult(fallback, isOverwrite: false, isDirectoryOwner);
    }

    private ArtifactNameResult ResolveWithTimestamp(string directory, string fileName, string extension, bool isDirectoryOwner)
    {
        string timestamp = _timeProvider().ToString("yyyyMMdd'T'HHmmss.fff", CultureInfo.InvariantCulture);
        string path = Path.Combine(directory, fileName + "_" + timestamp + extension);

        if (!_fileExists(path))
        {
            return new ArtifactNameResult(path, isOverwrite: false, isDirectoryOwner);
        }

        // If timestamp collision (parallel runs), fall back to counter.
        return ResolveWithCounter(directory, fileName + "_" + timestamp, extension, isDirectoryOwner);
    }

    private ArtifactNameResult ResolveOverwrite(string path, bool isDirectoryOwner)
    {
        bool isOverwrite = _fileExists(path);

        return new ArtifactNameResult(path, isOverwrite, isDirectoryOwner);
    }

    private ArtifactNameResult ResolveFail(string path, bool isDirectoryOwner)
    {
        if (_fileExists(path))
        {
            throw new InvalidOperationException(
                string.Format(CultureInfo.InvariantCulture,
                    "Artifact path '{0}' already exists and collision behavior is set to Fail.", path));
        }

        return new ArtifactNameResult(path, isOverwrite: false, isDirectoryOwner);
    }

    /// <summary>
    /// Ensures the directory exists and attempts to claim ownership via a marker file.
    /// Returns <see langword="true"/> if this process created the directory (owns it).
    /// </summary>
    private bool EnsureDirectoryWithOwnership(string directory)
    {
        if (StringUtils.IsNullOrEmpty(directory))
        {
            return false;
        }

        bool directoryExisted = _directoryExists(directory);
        _createDirectory(directory);

        if (directoryExisted)
        {
            // Directory already existed — we're not the owner.
            return false;
        }

        // New directory — claim ownership with an atomic marker file.
        string markerPath = Path.Combine(directory, MarkerFileName);
#if NET
        string markerContent = Environment.ProcessId.ToString(CultureInfo.InvariantCulture);
#else
        string markerContent = System.Diagnostics.Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture);
#endif

        return _tryCreateMarkerFile(markerPath, markerContent);
    }

    /// <summary>
    /// Atomically creates a marker file using <see cref="FileMode.CreateNew"/>.
    /// Returns <see langword="true"/> if this call created the file, <see langword="false"/> if it already existed.
    /// </summary>
    private static bool TryCreateMarkerFileOnDisk(string path, string content)
    {
        try
        {
            // FileMode.CreateNew fails atomically if the file already exists.
            using FileStream fs = new(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            byte[] bytes = Encoding.UTF8.GetBytes(content);
            fs.Write(bytes, 0, bytes.Length);

            return true;
        }
        catch (IOException)
        {
            // File already exists — another process claimed ownership.
            return false;
        }
    }
}
