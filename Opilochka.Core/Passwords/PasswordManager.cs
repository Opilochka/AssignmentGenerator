using System.Security.Cryptography;
using System.Text;


namespace Opilochka.Core.Passwords
{
    public class PasswordManager
    {
        public readonly string VALID_CHARS = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";

        /// <summary>
        /// Генерация случайного пароля
        /// </summary>
        /// <param name="length"></param> длина пароля
        /// <returns></returns>
        public string GeneratePassword(int length)
        {
            StringBuilder password = new StringBuilder();
            Random random = new Random();

            while (0 < length--)
            {
                password.Append(VALID_CHARS[random.Next(VALID_CHARS.Length)]);
            }
            return password.ToString();
        }

        /// <summary>
        /// Хеширование пароля с использованием MD5
        /// </summary>
        /// <param name="password"></param> сгенерированный пароль
        /// <returns></returns>
        public string HashPassword(string password)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(password);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                StringBuilder passwordHash = new StringBuilder();

                for (int i = 0; i < hashBytes.Length; i++)
                {
                    passwordHash.Append(hashBytes[i].ToString("x2"));
                }

                return passwordHash.ToString();
            }
        }
    }
}
