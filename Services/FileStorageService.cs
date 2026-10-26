using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PropertyAssessmentApp.Data;
using PropertyAssessmentApp.Models;

namespace PropertyAssessmentApp.Services;

public class FileStorageService : IFileStorageService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<FileStorageService> _logger;

    // Default fallbacks matching CAST system default file locations
    private static readonly Dictionary<int, (string Name, string Location)> FallbackLocations = new()
    {
        { 1, ("Profile", @"\\192.168.0.182\mapping_system\Attachments\UserProfile") },
        { 2, ("Attachment", @"\\192.168.0.182\mapping_system\Attachments\Attachment _for_Testing") },
        { 3, ("Sketch", @"\\192.168.0.182\mapping_system\Attachments\Skech") },
        { 4, ("InspectionAttachment", @"\\192.168.0.182\mapping_system\Attachments\Inspection") },
        { 5, ("SketchLand", @"\\192.168.0.182\mapping_system\Attachments\Inspection") },
        { 6, ("PropertyAssessment", @"\\192.168.0.182\mapping_system\Attachments\PropertyAssessment") },
        { 11, ("Profile", "~/UploadFile/UserProfile") },
        { 12, ("Attachment", "~/UploadFile/Attachment") },
        { 13, ("Sketch", "~/UploadFile/Sketch") },
        { 14, ("InspectionAttachmentNetwork", "~/UploadFile/Inspection") },
        { 15, ("SketchLandBase", "~/UploadFile/Land") },
        { 16, ("PropertyAssessmentBase", "~/UploadFile/PropertyAssessment") }
    };

    public FileStorageService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<FileStorageService> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public string GetPhysicalStorageDirectory(FileDefaultLocation locationKey, string? webRootPath = null)
    {
        var targetId = ResolveLocationId(locationKey);
        var configuredPath = GetLocationStringFromDb(targetId) ?? GetFallbackPath(targetId);
        return ResolveToPhysicalPath(configuredPath, webRootPath);
    }

    public async Task<string> GetPhysicalStorageDirectoryAsync(FileDefaultLocation locationKey, string? webRootPath = null)
    {
        var targetId = ResolveLocationId(locationKey);
        var configuredPath = await GetLocationStringFromDbAsync(targetId) ?? GetFallbackPath(targetId);
        return ResolveToPhysicalPath(configuredPath, webRootPath);
    }

    public SavedFileResult SaveFile(IFormFile? file, FileDefaultLocation locationKey, string? webRootPath = null, string? filePrefix = null)
    {
        if (file == null || file.Length == 0)
        {
            return new SavedFileResult { Success = false, ErrorMessage = "No file provided or file is empty." };
        }

        var safeRoot = GetSafeWebRoot(webRootPath);
        var isNetwork = IsNetworkEnabled();
        var primaryId = ResolveLocationId(locationKey, isNetwork);
        var primaryConfigured = GetLocationStringFromDb(primaryId) ?? GetFallbackPath(primaryId);
        var primaryDir = ResolveToPhysicalPath(primaryConfigured, safeRoot);

        // Attempt 1: Try configured primary storage directory (e.g. Network UNC if enabled, or local path)
        try
        {
            return PerformFileSave(file, primaryDir, primaryConfigured, filePrefix);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save file to primary directory '{Dir}'. Attempting fallback...", primaryDir);

            // Attempt 2: Local base storage (e.g. ~/UploadFile/Attachment)
            try
            {
                var fallbackId = GetBaseLocationId(locationKey);
                var fallbackConfigured = GetLocationStringFromDb(fallbackId) ?? GetFallbackPath(fallbackId);
                var fallbackDir = ResolveToPhysicalPath(fallbackConfigured, safeRoot);
                return PerformFileSave(file, fallbackDir, fallbackConfigured, filePrefix);
            }
            catch (Exception fallbackEx)
            {
                _logger.LogWarning(fallbackEx, "Failed to save file to fallback directory. Attempting emergency local upload directory...");

                // Attempt 3: Ultimate emergency fallback to safe wwwroot/UploadFile/Attachment
                try
                {
                    var emergencyDir = Path.Combine(safeRoot, "UploadFile", "Attachment");
                    return PerformFileSave(file, emergencyDir, "~/UploadFile/Attachment", filePrefix);
                }
                catch (Exception emergencyEx)
                {
                    _logger.LogError(emergencyEx, "All file save attempts failed for location key {LocationKey}.", locationKey);
                    return new SavedFileResult
                    {
                        Success = false,
                        ErrorMessage = $"Failed to save attachment: {emergencyEx.Message}",
                        OriginalFileName = file.FileName
                    };
                }
            }
        }
    }

    public async Task<SavedFileResult> SaveFileAsync(IFormFile? file, FileDefaultLocation locationKey, string? webRootPath = null, string? filePrefix = null)
    {
        if (file == null || file.Length == 0)
        {
            return new SavedFileResult { Success = false, ErrorMessage = "No file provided or file is empty." };
        }

        var safeRoot = GetSafeWebRoot(webRootPath);
        var isNetwork = IsNetworkEnabled();
        var primaryId = ResolveLocationId(locationKey, isNetwork);
        var primaryConfigured = await GetLocationStringFromDbAsync(primaryId) ?? GetFallbackPath(primaryId);
        var primaryDir = ResolveToPhysicalPath(primaryConfigured, safeRoot);

        // Attempt 1: Try configured primary storage directory
        try
        {
            return await PerformFileSaveAsync(file, primaryDir, primaryConfigured, filePrefix);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save file async to primary directory '{Dir}'. Attempting fallback...", primaryDir);

            // Attempt 2: Local base storage
            try
            {
                var fallbackId = GetBaseLocationId(locationKey);
                var fallbackConfigured = await GetLocationStringFromDbAsync(fallbackId) ?? GetFallbackPath(fallbackId);
                var fallbackDir = ResolveToPhysicalPath(fallbackConfigured, safeRoot);
                return await PerformFileSaveAsync(file, fallbackDir, fallbackConfigured, filePrefix);
            }
            catch (Exception fallbackEx)
            {
                _logger.LogWarning(fallbackEx, "Failed to save file async to fallback directory. Attempting emergency local upload directory...");

                // Attempt 3: Ultimate emergency fallback to safe wwwroot/UploadFile/Attachment
                try
                {
                    var emergencyDir = Path.Combine(safeRoot, "UploadFile", "Attachment");
                    return await PerformFileSaveAsync(file, emergencyDir, "~/UploadFile/Attachment", filePrefix);
                }
                catch (Exception emergencyEx)
                {
                    _logger.LogError(emergencyEx, "All async file save attempts failed for location key {LocationKey}.", locationKey);
                    return new SavedFileResult
                    {
                        Success = false,
                        ErrorMessage = $"Failed to save attachment: {emergencyEx.Message}",
                        OriginalFileName = file.FileName
                    };
                }
            }
        }
    }

    public async Task<IEnumerable<FileLocation>> GetAllLocationsAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var list = await context.FileLocations.AsNoTracking().OrderBy(f => f.FileLocationId).ToListAsync();
            if (list.Any())
            {
                return list;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching FileLocations from database.");
        }

        return FallbackLocations.Select(kv => new FileLocation
        {
            FileLocationId = kv.Key,
            Name = kv.Value.Name,
            Location = kv.Value.Location
        });
    }

    public async Task<FileLocation?> GetLocationByIdAsync(int id)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var item = await context.FileLocations.FindAsync(id);
            if (item != null) return item;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding FileLocation {Id} in database.", id);
        }

        if (FallbackLocations.TryGetValue(id, out var fallback))
        {
            return new FileLocation { FileLocationId = id, Name = fallback.Name, Location = fallback.Location };
        }
        return null;
    }

    public async Task<bool> UpdateLocationAsync(int id, string name, string location)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var item = await context.FileLocations.FindAsync(id);
            if (item == null)
            {
                item = new FileLocation { FileLocationId = id, Name = name, Location = location };
                context.FileLocations.Add(item);
            }
            else
            {
                item.Name = name;
                item.Location = location;
            }
            await context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update FileLocation {Id}.", id);
            return false;
        }
    }

    #region Helper Methods

    private bool IsNetworkEnabled()
    {
        var val = _configuration["IsNetworkConnected"] ?? _configuration["IsNewtworkConnected"];
        return bool.TryParse(val, out var enabled) && enabled;
    }

    private int ResolveLocationId(FileDefaultLocation locationKey, bool? overrideNetwork = null)
    {
        var network = overrideNetwork ?? IsNetworkEnabled();
        var keyVal = (int)locationKey;

        // If key is already a base location (>= 10)
        if (keyVal >= 10)
        {
            return keyVal;
        }

        // If network enabled, use the network ID (1..6)
        if (network)
        {
            return keyVal;
        }

        // Otherwise return local base ID (11..16)
        return GetBaseLocationId(locationKey);
    }

    private static int GetBaseLocationId(FileDefaultLocation locationKey)
    {
        return locationKey switch
        {
            FileDefaultLocation.Profile => (int)FileDefaultLocation.ProfileBase,
            FileDefaultLocation.Attachment => (int)FileDefaultLocation.AttachmentBase,
            FileDefaultLocation.Sketch => (int)FileDefaultLocation.SketchBase,
            FileDefaultLocation.Inspection => (int)FileDefaultLocation.InspectionBase,
            FileDefaultLocation.SketchLand => (int)FileDefaultLocation.SketchLandBase,
            FileDefaultLocation.PropertyAssessment => (int)FileDefaultLocation.PropertyAssessmentBase,
            _ => (int)locationKey >= 10 ? (int)locationKey : (int)locationKey + 10
        };
    }

    private string? GetLocationStringFromDb(int id)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return context.FileLocations.AsNoTracking().FirstOrDefault(f => f.FileLocationId == id)?.Location;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not read FileLocation {Id} from database. Using fallback.", id);
            return null;
        }
    }

    private async Task<string?> GetLocationStringFromDbAsync(int id)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var item = await context.FileLocations.AsNoTracking().FirstOrDefaultAsync(f => f.FileLocationId == id);
            return item?.Location;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not read FileLocation {Id} from database. Using fallback.", id);
            return null;
        }
    }

    private static string GetFallbackPath(int id)
    {
        if (FallbackLocations.TryGetValue(id, out var entry))
        {
            return entry.Location;
        }
        return "~/UploadFile/Attachment";
    }

    private static string GetSafeWebRoot(string? webRootPath)
    {
        if (!string.IsNullOrWhiteSpace(webRootPath) && Directory.Exists(webRootPath))
        {
            return webRootPath;
        }

        var cur = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        if (Directory.Exists(cur)) return cur;

        var baseDir = Path.Combine(AppContext.BaseDirectory, "wwwroot");
        if (Directory.Exists(baseDir)) return baseDir;

        try
        {
            Directory.CreateDirectory(cur);
            return cur;
        }
        catch
        {
            return Directory.GetCurrentDirectory();
        }
    }

    private static string SanitizePrefix(string? prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix)) return "DOC";
        var invalidChars = Path.GetInvalidFileNameChars();
        var sb = new System.Text.StringBuilder();
        foreach (var ch in prefix)
        {
            sb.Append(invalidChars.Contains(ch) ? '_' : ch);
        }
        var cleaned = sb.ToString().Trim().Replace(' ', '_');
        return string.IsNullOrWhiteSpace(cleaned) ? "DOC" : cleaned;
    }

    private static string ResolveToPhysicalPath(string configuredPath, string? webRootPath)
    {
        var safeWebRoot = GetSafeWebRoot(webRootPath);

        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            configuredPath = "~/UploadFile/Attachment";
        }

        if (configuredPath.StartsWith("~") || configuredPath.StartsWith("/") || (configuredPath.StartsWith("\\") && !configuredPath.StartsWith(@"\\")))
        {
            var relative = configuredPath.TrimStart('~', '/', '\\').Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
            return Path.Combine(safeWebRoot, relative);
        }

        // UNC Path (\\server\share\...) or Drive Letter (C:\...)
        return configuredPath;
    }

    private static bool IsUncPathAccessible(string uncPath, int timeoutMs = 400)
    {
        if (string.IsNullOrWhiteSpace(uncPath) || !uncPath.StartsWith(@"\\"))
        {
            return true;
        }

        try
        {
            var parts = uncPath.TrimStart('\\').Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return false;
            var host = parts[0];

            using var client = new System.Net.Sockets.TcpClient();
            var ar = client.BeginConnect(host, 445, null, null);
            var success = ar.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(timeoutMs));
            if (!success) return false;
            client.EndConnect(ar);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private SavedFileResult PerformFileSave(IFormFile file, string storageDirectory, string configuredPath, string? filePrefix)
    {
        if (storageDirectory.StartsWith(@"\\") && !IsUncPathAccessible(storageDirectory))
        {
            throw new IOException($"UNC network storage path '{storageDirectory}' is unreachable.");
        }

        if (!Directory.Exists(storageDirectory))
        {
            Directory.CreateDirectory(storageDirectory);
        }

        var cleanPrefix = SanitizePrefix(filePrefix);
        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(ext)) ext = ".dat";
        var uniqueFileName = $"{Guid.NewGuid()}_{cleanPrefix}{ext}";
        var physicalPath = Path.Combine(storageDirectory, uniqueFileName);

        using (var stream = new FileStream(physicalPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            file.CopyTo(stream);
        }

        var webRelativePath = ResolveWebPath(configuredPath, uniqueFileName);

        return new SavedFileResult
        {
            Success = true,
            OriginalFileName = file.FileName,
            SavedFileName = uniqueFileName,
            PhysicalPath = physicalPath,
            WebRelativePath = webRelativePath,
            FileSizeFormatted = FormatFileSize(file.Length)
        };
    }

    private async Task<SavedFileResult> PerformFileSaveAsync(IFormFile file, string storageDirectory, string configuredPath, string? filePrefix)
    {
        if (storageDirectory.StartsWith(@"\\") && !IsUncPathAccessible(storageDirectory))
        {
            throw new IOException($"UNC network storage path '{storageDirectory}' is unreachable.");
        }

        if (!Directory.Exists(storageDirectory))
        {
            Directory.CreateDirectory(storageDirectory);
        }

        var cleanPrefix = SanitizePrefix(filePrefix);
        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(ext)) ext = ".dat";
        var uniqueFileName = $"{Guid.NewGuid()}_{cleanPrefix}{ext}";
        var physicalPath = Path.Combine(storageDirectory, uniqueFileName);

        using (var stream = new FileStream(physicalPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await file.CopyToAsync(stream);
        }

        var webRelativePath = ResolveWebPath(configuredPath, uniqueFileName);

        return new SavedFileResult
        {
            Success = true,
            OriginalFileName = file.FileName,
            SavedFileName = uniqueFileName,
            PhysicalPath = physicalPath,
            WebRelativePath = webRelativePath,
            FileSizeFormatted = FormatFileSize(file.Length)
        };
    }

    private static string ResolveWebPath(string configuredPath, string uniqueFileName)
    {
        if (configuredPath.StartsWith("~"))
        {
            var relative = configuredPath.TrimStart('~').TrimStart('/').Replace('\\', '/');
            return $"/{relative}/{uniqueFileName}";
        }
        if (configuredPath.StartsWith("/"))
        {
            var relative = configuredPath.TrimStart('/').Replace('\\', '/');
            return $"/{relative}/{uniqueFileName}";
        }
        // If external or UNC, provide a standard download/preview endpoint reference
        return $"/UploadFile/Attachment/{uniqueFileName}";
    }

    private static string FormatFileSize(long bytes)
    {
        if (bytes >= 1024 * 1024)
            return $"{((double)bytes / (1024 * 1024)):N1} MB";
        if (bytes >= 1024)
            return $"{((double)bytes / 1024):N1} KB";
        return $"{bytes} Bytes";
    }

    #endregion
}
