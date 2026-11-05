using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyAssessmentApp.Data;
using PropertyAssessmentApp.DTOs;
using PropertyAssessmentApp.Filters;
using PropertyAssessmentApp.Helpers;
using PropertyAssessmentApp.Models;
using PropertyAssessmentApp.Services;

namespace PropertyAssessmentApp.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class MobileAuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IUserAccountService _userService;

    public MobileAuthController(ApplicationDbContext context, IUserAccountService userService)
    {
        _context = context;
        _userService = userService;
    }

    // GET: /api/test or /api/ping or /api/auth/ping
    [HttpGet("/api/test")]
    [HttpGet("/api/ping")]
    [HttpGet("/api/auth/ping")]
    [AllowAnonymous]
    public async Task<IActionResult> Ping()
    {
        bool dbConnected = false;
        int userCount = 0;
        string? dbError = null;

        try
        {
            userCount = await _context.UserAccounts.CountAsync();
            dbConnected = true;
        }
        catch (Exception ex)
        {
            dbError = ex.Message;
        }

        return Ok(new
        {
            status = "OK",
            message = "PropertyAssessmentApp & FairTrack Mobile API is ONLINE",
            serverTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            serverMachine = Environment.MachineName,
            database = new
            {
                connected = dbConnected,
                registeredUsersCount = userCount,
                error = dbError
            },
            endpoints = new
            {
                test = "GET /api/test",
                ping = "GET /api/auth/ping",
                login = "POST /api/auth/login",
                profile = "GET /api/auth/profile",
                dashboard = "GET /api/mobile/dashboard",
                transactions = "GET /api/mobile/transactions",
                downloadDetails = "GET /api/mobile/transaction/download-details"
            }
        });
    }

    // POST: /api/auth/login
    [HttpPost("/api/auth/login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] MobileLoginRequest model)
    {
        if (model == null || string.IsNullOrWhiteSpace(model.Username) || string.IsNullOrWhiteSpace(model.Password))
        {
            return BadRequest(new { success = false, message = "Username and password are required." });
        }

        string cleanUsername = model.Username.Trim();
        string cleanPassword = model.Password.Trim();

        var user = await _context.UserAccounts
            .FirstOrDefaultAsync(u => u.Username.ToLower() == cleanUsername.ToLower());

        if (user == null || !_userService.VerifyPassword(user, cleanPassword))
        {
            return Unauthorized(new { success = false, message = "Invalid username or password." });
        }

        if (!user.IsActive)
        {
            return Unauthorized(new { success = false, message = "Account is disabled. Please contact your system administrator." });
        }

        // Update Last Login Date
        user.LastLoginDate = DateTime.Now;
        await _context.SaveChangesAsync();

        // Generate JWT Token (Valid for 7 Days)
        string token = JwtHelper.GenerateToken(user, expiryDays: 7);
        bool isAdmin = user.Role == UserRole.SystemAdmin || user.Role == UserRole.CityAssessor;

        string divisionName = user.DivisionId switch
        {
            1 => "Appraisal & Assessment Division",
            2 => "Tax Mapping Division",
            3 => "Records & Releasing Division",
            _ => "Office of the City Assessor"
        };

        return Ok(new
        {
            success = true,
            message = "Authentication successful.",
            tokenType = "Bearer",
            accessToken = token,
            expiresInDays = 7,
            user = new
            {
                uid = user.Id.ToString(),
                username = user.Username,
                name = user.FullName,
                roleId = (int)user.Role,
                divisionId = user.DivisionId ?? 1,
                isAdmin = isAdmin,
                divisionName = divisionName
            }
        });
    }

    // GET: /api/auth/profile
    [HttpGet("/api/auth/profile")]
    [JwtAuthorize]
    public async Task<IActionResult> Profile()
    {
        if (HttpContext.Items["JwtUser"] is not JwtPayload jwtUser)
        {
            return Unauthorized(new { success = false, message = "User context not found." });
        }

        if (!Guid.TryParse(jwtUser.Uid, out var userId))
        {
            return Unauthorized(new { success = false, message = "Invalid user identification." });
        }

        var dbUser = await _context.UserAccounts.FindAsync(userId);
        if (dbUser == null)
        {
            return NotFound(new { success = false, message = "User not found in database." });
        }

        bool isAdmin = dbUser.Role == UserRole.SystemAdmin || dbUser.Role == UserRole.CityAssessor;
        string divisionName = dbUser.DivisionId switch
        {
            1 => "Appraisal & Assessment Division",
            2 => "Tax Mapping Division",
            3 => "Records & Releasing Division",
            _ => "Office of the City Assessor"
        };

        return Ok(new
        {
            success = true,
            user = new
            {
                uid = dbUser.Id.ToString(),
                username = dbUser.Username,
                name = dbUser.FullName,
                roleId = (int)dbUser.Role,
                divisionId = dbUser.DivisionId ?? 1,
                isAdmin = isAdmin,
                divisionName = divisionName
            }
        });
    }

    // POST: /api/auth/refresh
    [HttpPost("/api/auth/refresh")]
    [JwtAuthorize]
    public async Task<IActionResult> Refresh()
    {
        if (HttpContext.Items["JwtUser"] is not JwtPayload jwtUser)
        {
            return Unauthorized(new { success = false, message = "Invalid token claims." });
        }

        if (!Guid.TryParse(jwtUser.Uid, out var userId))
        {
            return Unauthorized(new { success = false, message = "Invalid user identification." });
        }

        var dbUser = await _context.UserAccounts.FindAsync(userId);
        if (dbUser == null)
        {
            return Unauthorized(new { success = false, message = "User account no longer exists." });
        }

        string newToken = JwtHelper.GenerateToken(dbUser, expiryDays: 7);

        return Ok(new
        {
            success = true,
            accessToken = newToken,
            expiresInDays = 7
        });
    }

    // POST: /api/auth/logout
    [HttpPost("/api/auth/logout")]
    [AllowAnonymous]
    public IActionResult Logout()
    {
        return Ok(new
        {
            success = true,
            message = "Logged out successfully from FairTrack Mobile."
        });
    }
}
