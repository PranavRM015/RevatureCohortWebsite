using System;
using Users;
using LoginTokens;
using System.Threading.Tasks;
using Discord.WebSocket;
using System.Text.RegularExpressions;
using System.Security.Cryptography;

namespace BotCommands
{
    public class BotCommand
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public BotCommand(string name, string description)
        {
            this.Name = name;
            this.Description = description;
        }

        private static (Role Role, bool IsAdmin, Track? Track) ReadRoles(SocketGuildUser user)
        {
            var names = user.Roles.Where(r => !r.IsEveryone).Select(r => r.Name.ToLower()).ToList();
            var role = names.Contains("trainer") ? Role.Trainer : Role.Trainee;
            Track? track = names.Contains("react") ? Track.React : names.Contains("angular") ? Track.Angular : null;
            return (role, names.Contains("admin"), track);
        }
    
        public async Task RegisterCommand(SocketSlashCommand command)
        {
            if(command.User is not SocketGuildUser user)
            {
                await command.RespondAsync("This command only works inside the server.", ephemeral: true);
                return;
            }
            var (role, isAdmin, track) = ReadRoles(user);
            string firstName = command.Data.Options.First(o => o.Name == "first_name").Value.ToString()!.Trim();
            string lastName = command.Data.Options.First(o => o.Name == "last_name").Value.ToString()!.Trim();
            if (firstName == "" || firstName.Length > 50 || !Regex.IsMatch(firstName, @"^[a-zA-Z\s]+$"))
            {
                await command.RespondAsync("First name must be 1-50 letters and spaces only.", ephemeral: true);
                return;
            }
            if (lastName == "" || lastName.Length > 50 || !Regex.IsMatch(lastName, @"^[a-zA-Z\s]+$"))
            {
                await command.RespondAsync("Last name must be 1-50 letters and spaces only.", ephemeral: true);
                return;
            }
            DateTime registeredAt = DateTime.UtcNow;
            
            var colorRole = user.Roles
                .Where(r => r.Color.RawValue != 0)
                .OrderByDescending(r => r.Position)
                .FirstOrDefault();

            uint profileColorValue = colorRole?.Color.RawValue ?? 0;
            string profileColor = "#" + profileColorValue.ToString("X6");
            int trackVal = track == 0 ? 0 : 1;
  
            User user_c = new User(user.Id, firstName, lastName, profileColor, role, isAdmin, (Track)trackVal, registeredAt, "");

            using var db = new CohortContext();

            if (await db.Users.FindAsync(user_c.DiscordId) != null)
            {
                await command.RespondAsync("You are already registered.", ephemeral: true);
                return;
            }

            db.Users.Add(user_c);
            await db.SaveChangesAsync();

            await command.RespondAsync($"Registered {user_c.Username}.", ephemeral: true);
        }

        public async Task LoginCommand(SocketSlashCommand command)
        {
            if (command.User is not SocketGuildUser user)
            {
                await command.RespondAsync("This command only works inside the server.", ephemeral: true);
                return;
            }
            using var db = new CohortContext();
            var user_c = await db.Users.FindAsync(user.Id);
            if(user_c == null)
            {
                await command.RespondAsync("You are not registered; please run /register first to create an account", ephemeral: true);
                return;
            }
            else
            {
                await command.RespondAsync($"Welcome back, {user_c.Username}!", ephemeral: true);
                string randomBytes = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
                LoginToken Logintoken = new LoginToken(randomBytes, user.Id, DateTime.UtcNow.AddMinutes(60));
                
                var (role, isAdmin, track) = ReadRoles(user);
                user_c.Role = role;
                user_c.IsAdmin = isAdmin;
                if (track != null)
                    user_c.Track = track.Value;

                db.LoginTokens.Add(Logintoken);
                await db.SaveChangesAsync();
                string baseUrl = Environment.GetEnvironmentVariable("WEBSITE_URL") ?? throw new InvalidOperationException("WEBSITE_URL not set");
                string link = $"{baseUrl.TrimEnd('/')}/login?token={Uri.EscapeDataString(Logintoken.Token)}";
                await command.FollowupAsync($"Logged in as {user_c.Username}. Your login link (expires in 60 minutes, single use): {link}", ephemeral: true);
            }
        }
    }
}
