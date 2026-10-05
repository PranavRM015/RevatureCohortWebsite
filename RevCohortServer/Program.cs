using Discord;
using Discord.WebSocket;
using BotCommands;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

DotNetEnv.Env.TraversePath().Load();

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

var app = builder.Build();

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
    return Results.Redirect("/me");
});

app.MapGet("/me", async (ClaimsPrincipal principal, CohortContext db) =>
{
    var user = await db.Users.FindAsync(ulong.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!));
    return user == null ? Results.Unauthorized() : Results.Ok(user);
}).RequireAuthorization();

app.MapPut("/me", async (ProfileUpdate body, ClaimsPrincipal principal, CohortContext db) =>
{
    if (!Regex.IsMatch(body.ProfileColor ?? "", "^#[0-9A-Fa-f]{6}$"))
        return Results.BadRequest("ProfileColor must look like #RRGGBB.");

    string link = body.ProfileLink ?? "";
    if (link != "" && (!Uri.TryCreate(link, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)))
        return Results.BadRequest("ProfileLink must be a valid http(s) URL.");

    var user = await db.Users.FindAsync(ulong.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!));
    if (user == null)
        return Results.Unauthorized();

    user.ProfileColor = body.ProfileColor!;
    user.ProfileLink = link;
    await db.SaveChangesAsync();
    return Results.Ok(user);
}).RequireAuthorization();

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

    await client.GetGuild(GuildId).CreateApplicationCommandAsync(cmd.Build());
    await client.GetGuild(GuildId).CreateApplicationCommandAsync(registerCmd.Build());
    await client.GetGuild(GuildId).CreateApplicationCommandAsync(loginCmd.Build());
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
};

var token = Environment.GetEnvironmentVariable("DISCORD_TOKEN")
            ?? throw new InvalidOperationException("DISCORD_TOKEN not set");

await client.LoginAsync(TokenType.Bot, token);
await client.StartAsync();

app.Run();

record ProfileUpdate(string? ProfileColor, string? ProfileLink);
