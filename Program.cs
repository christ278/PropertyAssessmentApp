using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using PropertyAssessmentApp.Data;
using PropertyAssessmentApp.Hubs;
using PropertyAssessmentApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Add Data Protection with workspace-contained key storage
var keysFolder = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtectionKeys");
if (!Directory.Exists(keysFolder)) Directory.CreateDirectory(keysFolder);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysFolder))
    .SetApplicationName("PropertyAssessmentApp");

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<PropertyAssessmentApp.Filters.ModuleAccessFilter>();
});
builder.Services.AddHttpClient();
builder.Services.AddSignalR();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection") 
                         ?? builder.Configuration.GetConnectionString("CAST_Entities"));
    options.ConfigureWarnings(w => 
        w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
});

// Add Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddSingleton<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<IAssessmentService, AssessmentService>();
builder.Services.AddScoped<IExcelAssessmentImportService, ExcelAssessmentImportService>();
builder.Services.AddScoped<IOrderPaymentService, OrderPaymentService>();
builder.Services.AddScoped<IOwnerService, OwnerService>();
builder.Services.AddSingleton<IGeographicService, GeographicService>();
builder.Services.AddScoped<IUserAccountService, UserAccountService>();
builder.Services.AddScoped<ITaxMappingService, TaxMappingService>();
builder.Services.AddScoped<ITaxDeclarationRequestService, TaxDeclarationRequestService>();
builder.Services.AddScoped<ILandHoldingRequestService, LandHoldingRequestService>();
builder.Services.AddScoped<IRequestForInspectionService, RequestForInspectionService>();
builder.Services.AddScoped<IPropertyAssessmentRequestService, PropertyAssessmentRequestService>();
builder.Services.AddScoped<IDocumentWorkflowService, DocumentWorkflowService>();
builder.Services.AddScoped<ICharterSlaService, CharterSlaService>();
builder.Services.AddScoped<IUserRoleService, UserRoleService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IDataTransferService, DataTransferService>();
builder.Services.AddScoped<ITaxDeclarationPdfService, TaxDeclarationPdfService>();
builder.Services.AddScoped<IRPTCertificatePdfService, RPTCertificatePdfService>();
builder.Services.AddScoped<IWorkflowNotifier, WorkflowNotifier>();
builder.Services.AddScoped<IPhilSmsService, PhilSmsService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMobileClients", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var app = builder.Build();

// Seed all 27 official Koronadal City barangays into the Barangays table if missing
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var geo = scope.ServiceProvider.GetRequiredService<IGeographicService>();
        var koronadalCity = db.CityMunicipalities.FirstOrDefault(c => c.Name.Contains("Koronadal"));
        if (koronadalCity != null)
        {
            var geoKoronadal = geo.GetCitiesByProvince(Guid.Parse("10000000-0000-0000-0000-000000000001"))
                .FirstOrDefault(c => c.Name.Contains("Koronadal", StringComparison.OrdinalIgnoreCase));
            if (geoKoronadal != null)
            {
                var existing = db.Barangays.Where(b => b.CityMunicipalityId == koronadalCity.Id).ToList();
                var existingNames = existing.Select(b => b.Name.Trim().ToLowerInvariant()).ToHashSet();
                var existingCodes = existing.Select(b => b.Code.Trim().ToLowerInvariant()).ToHashSet();
                bool added = false;

                foreach (var gb in geo.GetBarangaysByCity(geoKoronadal.Id))
                {
                    var normName = gb.Name.Trim().ToLowerInvariant();
                    var altZone1 = normName.Replace("zone i ", "zone 1 ");
                    if (existingNames.Contains(normName) || existingNames.Contains(altZone1) || existingCodes.Contains(gb.Code.Trim().ToLowerInvariant()))
                        continue;

                    db.Barangays.Add(new PropertyAssessmentApp.Models.Barangay
                    {
                        Id = Guid.NewGuid(),
                        CityMunicipalityId = koronadalCity.Id,
                        Code = gb.Code,
                        Name = gb.Name,
                        District = gb.District,
                        IsActive = true
                    });
                    added = true;
                }

                if (added) db.SaveChanges();
            }
        }
    }
    catch { }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

// Enable CORS for mobile apps, Expo web, and external clients
app.UseCors("AllowMobileClients");

// Configure Static Files with explicit font MIME types and CORS headers for mobile clients / uploads
var contentTypeProvider = new FileExtensionContentTypeProvider();
contentTypeProvider.Mappings[".woff2"] = "font/woff2";
contentTypeProvider.Mappings[".woff"] = "font/woff";
contentTypeProvider.Mappings[".ttf"] = "font/ttf";
app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = contentTypeProvider,
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
        ctx.Context.Response.Headers.Append("Access-Control-Allow-Headers", "*");
        ctx.Context.Response.Headers.Append("Access-Control-Allow-Methods", "GET, HEAD, OPTIONS");
    }
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}")
    .WithStaticAssets();

app.MapHub<WorkflowHub>("/hubs/workflow");

app.Run();
