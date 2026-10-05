using System;
using Users;
using System.ComponentModel.DataAnnotations;

namespace LoginTokens
{
    public class LoginToken
    {
        [Key]
        [StringLength(64)]
        public string Token { get; set; }
        [Required]
        public ulong DiscordId { get; set; }
        [Required]
        public DateTime Expiration { get; set; }
        public bool IsUsed { get; set; }
        public LoginToken(string token, ulong discordId, DateTime expiration)
        {
            this.Token = token;
            this.DiscordId = discordId;
            this.Expiration = expiration;
            this.IsUsed = false;
        }
    }
}