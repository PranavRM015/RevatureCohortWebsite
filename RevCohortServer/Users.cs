using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Users
{
    public enum Role
    {
        Trainer,
        Trainee
    }
    public enum Track
    {
        React,
        Angular
    }
    public class User
    {
        [Key]
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public ulong DiscordId { get; set; }
        [Required]
        [StringLength(101)]
        public string Username { get; set; }
        [Required]
        [StringLength(50)]
        public string FirstName { get; set; }
        [Required]
        [StringLength(50)]
        public string LastName { get; set; }
        [Required]
        [RegularExpression("^#[0-9A-Fa-f]{6}$")]
        public string ProfileColor { get; set; }
        [Required]
        [EnumDataType(typeof(Role))]
        public Role Role { get; set; }
        [Required]
        public bool IsAdmin { get; set; }
        [Required]
        public Track Track { get; set; }
        public DateTime RegisteredAt { get; set; }
        [Url]
        [EnumDataType(typeof(Role))]
        public string ProfileLink { get; set; }
        public User(ulong discordId, string firstName, string lastName, string profileColor, Role role, bool isAdmin, Track track, DateTime registeredAt, string profileLink)
        {
            this.DiscordId = discordId;
            this.Username = $"{firstName} {lastName}";
            this.FirstName = firstName;
            this.LastName = lastName;
            this.ProfileColor = profileColor;
            this.Role = role;
            this.IsAdmin = isAdmin;
            this.Track = track;
            this.RegisteredAt = registeredAt;
            this.ProfileLink = profileLink;
        }
    }
}