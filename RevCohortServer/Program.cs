using Discord;
using Discord.WebSocket;
using BotCommands;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

try { DotNetEnv.Env.TraversePath().Load(); } catch (FileNotFoundException) { }

var client = new DiscordSocketClient(new DiscordSocketConfig
{
    GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.GuildMembers,
    AlwaysDownloadUsers = true
});

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CohortContext>();
builder.Services.AddSingleton(client);
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.HttpOnly = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.Events.OnRedirectToLogin = context => { context.Response.StatusCode = 401; return Task.CompletedTask; };
        options.Events.OnRedirectToAccessDenied = context => { context.Response.StatusCode = 403; return Task.CompletedTask; };
    });
builder.Services.AddAuthorization();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/login", async (string token, CohortContext db, HttpContext http) =>
{
    var loginToken = await db.LoginTokens.FindAsync(token);
    if (loginToken == null || loginToken.IsUsed || loginToken.Expiration < DateTime.UtcNow)
        return Results.Unauthorized();

    var user = await db.Users.FindAsync(loginToken.DiscordId);
    if (user == null)
        return Results.Unauthorized();

    loginToken.IsUsed = true;
    await db.SaveChangesAsync();

    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, user.DiscordId.ToString()),
        new(ClaimTypes.Role, user.Role.ToString())
    };
    if (user.IsAdmin)
        claims.Add(new Claim(ClaimTypes.Role, "Admin"));

    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
    return Results.Redirect("/");
});

app.MapGet("/me", async (ClaimsPrincipal principal, CohortContext db) =>
{
    var user = await db.Users.FindAsync(ulong.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!));
    return user == null ? Results.Unauthorized() : Results.Ok(user);
}).RequireAuthorization();

app.MapPut("/me", async (ProfileUpdate body, ClaimsPrincipal principal, CohortContext db) =>
{
    if (body.ProfileColor != null && !Regex.IsMatch(body.ProfileColor, "^#[0-9A-Fa-f]{6}$"))
        return Results.BadRequest("ProfileColor must look like #RRGGBB.");

    if (body.ProfileLink != null && body.ProfileLink != "" && (!Uri.TryCreate(body.ProfileLink, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)))
        return Results.BadRequest("ProfileLink must be a valid http(s) URL.");

    var user = await db.Users.FindAsync(ulong.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!));
    if (user == null)
        return Results.Unauthorized();

    if (body.ProfileColor != null)
    {
        var taken = (await db.Users.Where(u => u.DiscordId != user.DiscordId).Select(u => u.ProfileColor).ToListAsync())
            .Select(c => c.ToUpper()).ToHashSet();
        int colorValue = Convert.ToInt32(body.ProfileColor.Substring(1), 16);
        string candidate = "#" + colorValue.ToString("X6");
        while (taken.Contains(candidate))
        {
            colorValue = (colorValue + 1) % 0x1000000;
            candidate = "#" + colorValue.ToString("X6");
        }
        user.ProfileColor = candidate;
    }
    if (body.ProfileLink != null)
        user.ProfileLink = body.ProfileLink;

    await db.SaveChangesAsync();
    return Results.Ok(user);
}).RequireAuthorization();

app.MapGet("/profiles", async (CohortContext db) =>
{
    var profiles = await db.Users
        .OrderBy(u => u.Username)
        .Select(u => new { DiscordId = u.DiscordId.ToString(), u.Username, u.Track, u.ProfileColor, u.ProfileLink })
        .ToListAsync();
    return Results.Ok(profiles);
}).RequireAuthorization();

app.MapFallbackToFile("index.html");

app.MapDelete("/me", async (string confirm, ClaimsPrincipal principal, CohortContext db, HttpContext http) =>
{
    string[] options = { "yes", "sure", "please" };
    if (!options.Contains(confirm.ToLower()))
        return Results.BadRequest("Deletion not confirmed.");

    var user = await db.Users.FindAsync(ulong.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!));
    if (user == null)
        return Results.Unauthorized();

    db.LoginTokens.RemoveRange(db.LoginTokens.Where(t => t.DiscordId == user.DiscordId));
    db.Users.Remove(user);
    await db.SaveChangesAsync();
    await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.NoContent();
}).RequireAuthorization();


client.Log += msg => { Console.WriteLine(msg); return Task.CompletedTask; };

ulong GuildId = ulong.Parse(Environment.GetEnvironmentVariable("SERVER_ID") ?? throw new InvalidOperationException("SERVER_ID not set"));

client.Ready += async () =>
{
    var cmd = new SlashCommandBuilder()
        .WithName("ping")
        .WithDescription("Check if the bot is alive");

    var registerCmd = new SlashCommandBuilder()
        .WithName("register")
        .WithDescription("Register for the cohort site")
        .AddOption("first_name", ApplicationCommandOptionType.String, "Your first name", isRequired: true)
        .AddOption("last_name", ApplicationCommandOptionType.String, "Your last name", isRequired: true);

    var loginCmd = new SlashCommandBuilder()
        .WithName("login")
        .WithDescription("Get a login link for the cohort site");

    var roleAssignmentCmd = new SlashCommandBuilder()
        .WithName("role_assignment")
        .WithDescription("Assign a role to a user")
        .AddOption("user", ApplicationCommandOptionType.User, "The user to assign the role to", isRequired: true)
        .AddOption("role", ApplicationCommandOptionType.String, "The role to assign", isRequired: true, choices: new[]
        {
            new ApplicationCommandOptionChoiceProperties { Name = "trainee", Value = "trainee" },
            new ApplicationCommandOptionChoiceProperties { Name = "trainer", Value = "trainer" },
            new ApplicationCommandOptionChoiceProperties { Name = "react", Value = "react" },
            new ApplicationCommandOptionChoiceProperties { Name = "angular", Value = "angular" }
        });

    await client.GetGuild(GuildId).CreateApplicationCommandAsync(cmd.Build());
    await client.GetGuild(GuildId).CreateApplicationCommandAsync(registerCmd.Build());
    await client.GetGuild(GuildId).CreateApplicationCommandAsync(loginCmd.Build());
    await client.GetGuild(GuildId).CreateApplicationCommandAsync(roleAssignmentCmd.Build());
};

var bot = new BotCommand("bot", "Cohort bot commands");

client.SlashCommandExecuted += async command =>
{
    if (command.Data.Name == "ping")
        await command.RespondAsync($"Pong — {client.Latency} ms");
    else if (command.Data.Name == "register")
        await bot.RegisterCommand(command);
    else if (command.Data.Name == "login")
        await bot.LoginCommand(command);
    else if (command.Data.Name == "role_assignment")
        await bot.roleAssignmentCommand(command);
};

var token = Environment.GetEnvironmentVariable("DISCORD_TOKEN")
            ?? throw new InvalidOperationException("DISCORD_TOKEN not set");

await client.LoginAsync(TokenType.Bot, token);
await client.StartAsync();

app.Run();

record ProfileUpdate(string? ProfileColor, string? ProfileLink);
