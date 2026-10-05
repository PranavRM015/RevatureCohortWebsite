using Users;
using FluentValidation;

public class UserValidator : AbstractValidator<User>
{
    public UserValidator()
    {
        RuleFor(x => x.DiscordId).NotEmpty().WithMessage("Discord ID cannot be empty");
        RuleFor(x => x.Username).NotEmpty().WithMessage("Username cannot be empty");
        RuleFor(x => x.FirstName).NotEmpty().WithMessage("First name cannot be empty");
        RuleFor(x => x.LastName).NotEmpty().WithMessage("Last name cannot be empty");
        RuleFor(x => x.ProfileColor).NotEmpty().WithMessage("Profile color cannot be empty");
        RuleFor(x => x.Role).IsInEnum().WithMessage("Role cannot be empty");
        RuleFor(x => x.IsAdmin).NotEmpty().WithMessage("Is admin cannot be empty");
        RuleFor(x => x.Track).IsInEnum().WithMessage("Track name must be either React or Angular");
        RuleFor(x => x.RegisteredAt).NotEmpty().WithMessage("Registered at cannot be empty");
    }
}