using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyAssessmentApp.Models;
using PropertyAssessmentApp.Services;

namespace PropertyAssessmentApp.Controllers;

[Authorize]
public class FileLocationController : Controller
{
    private readonly IFileStorageService _fileStorageService;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public FileLocationController(
        IFileStorageService fileStorageService,
        IWebHostEnvironment webHostEnvironment)
    {
        _fileStorageService = fileStorageService;
        _webHostEnvironment = webHostEnvironment;
    }

    // GET: /FileLocation
    public async Task<IActionResult> Index()
    {
        var locations = await _fileStorageService.GetAllLocationsAsync();
        ViewBag.WebRootPath = _webHostEnvironment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        return View(locations);
    }

    // POST: /FileLocation/Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int fileLocationId, string name, string location)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(location))
        {
            TempData["ErrorMessage"] = "Location Name and Path cannot be empty.";
            return RedirectToAction(nameof(Index));
        }

        var success = await _fileStorageService.UpdateLocationAsync(fileLocationId, name.Trim(), location.Trim());
        if (success)
        {
            TempData["SuccessMessage"] = $"File location '{name}' (ID: {fileLocationId}) updated successfully.";
        }
        else
        {
            TempData["ErrorMessage"] = $"Failed to update file location ID {fileLocationId}.";
        }

        return RedirectToAction(nameof(Index));
    }
}
