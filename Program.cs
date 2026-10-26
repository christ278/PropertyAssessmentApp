using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using PropertyAssessmentApp.Data;
using PropertyAssessmentApp.Hubs;
using PropertyAssessmentApp.Services;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddSingleton<IOrderPaymentService, OrderPaymentService>();
builder.Services.AddScoped<IOwnerService, OwnerService>();
builder.Services.AddSingleton<IGeographicService, GeographicService>();
builder.Services.AddScoped<IUserAccountService, UserAccountService>();
builder.Services.AddScoped<ITaxMappingService, TaxMappingService>();
builder.Services.AddScoped<ITaxDeclarationRequestService, TaxDeclarationRequestService>();
builder.Services.AddScoped<ILandHoldingRequestService, LandHoldingRequestService>();
builder.Services.AddScoped<IRequestForInspectionService, RequestForInspectionService>();
builder.Services.AddScoped<IPropertyAssessmentRequestService, PropertyAssessmentRequestService>();
builder.Services.AddScoped<IDocumentWorkflowService, DocumentWorkflowService>();
builder.Services.AddScoped<IUserRoleService, UserRoleService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IDataTransferService, DataTransferService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Assessment}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapHub<WorkflowHub>("/hubs/workflow");

app.Run();
