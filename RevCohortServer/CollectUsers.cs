using Discord;
using Discord.WebSocket;
using System.Globalization;
using CsvHelper;
using Users;
public class CollectUsers(DiscordSocketClient client)
{
    public async Task PrintUsersAsync(ulong guildId)
    {
        var guild = client.GetGuild(guildId);
        if (guild == null)
        {
            Console.WriteLine($"Guild {guildId} not found. Is the bot invited to that server?");
            return;
        }

        await guild.DownloadUsersAsync(); // make sure the member cache is complete
        // if (nameOverrides.TryGet(member.Id, out var name)) { firstName = name.FirstName; lastName = name.LastName; }
        Console.WriteLine("[");
        foreach (var member in guild.Users.Where(m => !m.IsBot))
        {
            /*User myUser = new(
                member.Id,
                member.DisplayName,
                member.FirstName,
                member.LastName,
                member.ProfileColor,
                member.Role,
                member.IsAdmin,
                member.Track,
                member.RegisteredAt,
                member.ProfileLink
            );*/
            string[] x = string.Join(", ", member.Roles.Select(r => r.Name)).Split(", ");
            List<string> roles = new List<string>(x);
            roles.Remove("@everyone");
            int track = 0;
            bool isAdmin = roles.Contains("admin") ? true : false;
            int isTrainer = roles.Contains("trainer") ? 0 : 1;
            if(roles[0] == "react")
            {
                track = 0;
            }
            else if(roles[0] == "angular")
            {
                track = 1;
            }
            var colorRole = member.Roles
            .Where(r => r.Colors.PrimaryColor != Color.Default)   // Color.Default (0) means "no color"
            .OrderByDescending(r => r.Position)
            .FirstOrDefault();

            string profileColor = colorRole?.Colors.PrimaryColor.ToString() ?? "#99AAB5";
            Console.WriteLine($"DisplayName: {member.DisplayName}");
            Console.WriteLine("{");
            Console.WriteLine($"\"id\": {member.Id},");
            Console.WriteLine($"\"username\": \"{member.Username}\",");
            Console.WriteLine($"\"firstname\": \"{"  "}\",");
            Console.WriteLine($"\"lastname\": \"{"  "}\",");
            Console.WriteLine($"\"color\": \"{profileColor}\",");
            Console.WriteLine($"\"roles\": [{isTrainer}]");
            Console.WriteLine($"\"track\": {track},");
            Console.WriteLine($"\"isadmin\": {isAdmin},");
            Console.WriteLine($"\"registeredAt\": \"{"  "}\",");
            Console.WriteLine($"\"profileLink\": \"{"  "}\"");

            Console.WriteLine("},");

        }
        Console.WriteLine("]");
    }
}
