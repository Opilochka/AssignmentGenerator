namespace Opilochka.Core.Passwords
{
    public interface IPasswordManager
    {
        string GeneratePassword(int length);
        string HashPassword(string password);
    }
}