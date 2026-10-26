using Microsoft.AspNetCore.Http;
using PropertyAssessmentApp.Models;

namespace PropertyAssessmentApp.Services;

public class SavedFileResult
{
    public bool Success { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string SavedFileName { get; set; } = string.Empty;
    public string PhysicalPath { get; set; } = string.Empty;
    public string WebRelativePath { get; set; } = string.Empty;
    public string FileSizeFormatted { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
}

public interface IFileStorageService
{
    string GetPhysicalStorageDirectory(FileDefaultLocation locationKey, string? webRootPath = null);
    Task<string> GetPhysicalStorageDirectoryAsync(FileDefaultLocation locationKey, string? webRootPath = null);
    SavedFileResult SaveFile(IFormFile? file, FileDefaultLocation locationKey, string? webRootPath = null, string? filePrefix = null);
    Task<SavedFileResult> SaveFileAsync(IFormFile? file, FileDefaultLocation locationKey, string? webRootPath = null, string? filePrefix = null);
    Task<IEnumerable<FileLocation>> GetAllLocationsAsync();
    Task<FileLocation?> GetLocationByIdAsync(int id);
    Task<bool> UpdateLocationAsync(int id, string name, string location);
}
