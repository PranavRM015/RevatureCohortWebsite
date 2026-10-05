using Microsoft.EntityFrameworkCore;
using Users;

public static class UserSync
{
    public static async Task SyncUsersAsync(CohortContext db, IReadOnlyCollection<User> incoming)
    {
        if (incoming.Count == 0)
            throw new InvalidOperationException("Refusing to sync an empty user list.");

        var incomingIds = incoming.Select(u => u.DiscordId).ToHashSet();
        var existingById = await db.Users.ToDictionaryAsync(u => u.DiscordId);

        // Upsert
        foreach (var newUser in incoming)
        {
            if (!existingById.TryGetValue(newUser.DiscordId, out var existing))
            {
                db.Users.Add(newUser);                    // insert
            }
            else
            {
                existing.Username     = newUser.Username; // update
                existing.FirstName    = newUser.FirstName;
                existing.LastName     = newUser.LastName;
                existing.ProfileColor = newUser.ProfileColor;
                existing.Role         = newUser.Role;
                existing.IsAdmin      = newUser.IsAdmin;
                existing.Track        = newUser.Track;
                existing.ProfileLink  = newUser.ProfileLink;
            }
        }

        var stale = existingById.Values.Where(u => !incomingIds.Contains(u.DiscordId));
        db.Users.RemoveRange(stale);

        await db.SaveChangesAsync(); 
    }
}