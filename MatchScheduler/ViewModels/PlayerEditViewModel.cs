using System.Net.Mail;
using MatchScheduler.Models;

namespace MatchScheduler.ViewModels;

public class PlayerEditViewModel : ViewModelBase
{
    private readonly Player? _existing;
    private string _name = "";
    private Position _desiredPosition = Position.Any;
    private string _phoneNumber = "";
    private string _email = "";
    private string _errorMessage = "";

    public PlayerEditViewModel(Player? existing)
    {
        _existing = existing;

        if (existing == null)
            return;

        _name = existing.Name;
        _desiredPosition = existing.DesiredPosition;
        _phoneNumber = existing.PhoneNumber;
        _email = existing.Email;
    }

    public string Title => _existing == null ? "Add player" : "Edit player";
    public IReadOnlyList<PositionItem> PositionOptions => PositionInfo.All;

    public string Name { get => _name; set => Set(ref _name, value); }
    public Position DesiredPosition { get => _desiredPosition; set => Set(ref _desiredPosition, value); }
    public string PhoneNumber { get => _phoneNumber; set => Set(ref _phoneNumber, value); }
    public string Email { get => _email; set => Set(ref _email, value); }
    public string ErrorMessage { get => _errorMessage; private set => Set(ref _errorMessage, value); }

    /// <summary>Name is required. Phone and email are optional, but must look right when filled in.</summary>
    public bool Validate()
    {
        var phone = PhoneNumber.Trim();
        var email = Email.Trim();

        if (string.IsNullOrWhiteSpace(Name))
            ErrorMessage = "Please enter a name.";
        else if (phone.Length > 0 && !LooksLikePhone(phone))
            ErrorMessage = "That phone number doesn't look right. Use at least 7 digits.";
        else if (email.Length > 0 && !LooksLikeEmail(email))
            ErrorMessage = "That email address doesn't look right.";
        else
            ErrorMessage = "";

        return ErrorMessage.Length == 0;
    }

    public Player ToPlayer() => new()
    {
        Id = _existing?.Id ?? 0,
        Name = Name.Trim(),
        DesiredPosition = DesiredPosition,
        PhoneNumber = PhoneNumber.Trim(),
        Email = Email.Trim()
    };

    private static bool LooksLikePhone(string value) =>
        value.Count(char.IsDigit) >= 7 && value.All(c => char.IsDigit(c) || " +-().x#".Contains(c));

    private static bool LooksLikeEmail(string value) =>
        MailAddress.TryCreate(value, out var address) && address.Address == value;
}
