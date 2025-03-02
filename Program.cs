using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("\n--- Simple Encryption/Decryption App ---\n");

        while (true)
        {
            Console.WriteLine("Choose Operation:");
            Console.WriteLine("1. Encrypt Text");
            Console.WriteLine("2. Decrypt Text");
            Console.WriteLine("3. Exit");

            Console.Write("Choice: ");
            string choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    EncryptText();
                    break;
                case "2":
                    DecryptText();
                    break;
                case "3":
                    Console.WriteLine("Exiting application.");
                    return;
                default:
                    Console.WriteLine("Invalid choice.");
                    break;
            }

            Console.WriteLine("\n---------------------------\n");
        }
    }

    public static void EncryptText()
    {
        Console.WriteLine("\n--- Encrypt Text ---");
        Console.Write("Enter Text: ");
        string plaintext = Console.ReadLine();

        Console.Write("Enter Secret Key: ");
        string secretKey = Console.ReadLine();

        int expirationMinutes = GetExpirationMinutes();

        string encryptedText = EncryptString(plaintext, secretKey, expirationMinutes);
        Console.WriteLine($"\nEncrypted Text: \n{encryptedText}");
    }

    private static int GetExpirationMinutes()
    {
        int expirationMinutes;
        while (true)
        {
            Console.Write("Expiration (minutes): ");
            if (int.TryParse(Console.ReadLine(), out expirationMinutes) && expirationMinutes > 0)
            {
                break;
            }
            Console.WriteLine("Invalid input. Enter a positive number.");
        }
        return expirationMinutes;
    }

    public static void DecryptText()
    {
        Console.WriteLine("\n--- Decrypt Text ---");
        Console.Write("Encrypted Text: ");
        string encryptedText = Console.ReadLine();

        Console.Write("Secret Key: ");
        string secretKey = Console.ReadLine();

        string decryptedText = DecryptString(encryptedText, secretKey);
        if (decryptedText != null)
        {
            Console.WriteLine($"\nDecrypted Text: \n{decryptedText}");
        }
    }

    public static string EncryptString(string plainText, string secretKey, int expirationMinutes)
    {
        string dataWithTimestamp = $"{DateTime.UtcNow.Ticks}:{expirationMinutes}:{plainText}";
        Console.WriteLine(dataWithTimestamp);

        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = Encoding.UTF8.GetBytes(secretKey.PadRight(32, '\0').Substring(0, 32));
            aesAlg.IV = new byte[16];

            ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

            using (MemoryStream msEncrypt = new MemoryStream())
            {
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                {
                    using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(dataWithTimestamp);
                    }
                    byte[] encrypted = msEncrypt.ToArray();
                    return Convert.ToBase64String(encrypted);
                }
            }
        }
    }

    public static string DecryptString(string cipherText, string secretKey)
    {
        try
        {
            byte[] cipherBytes = Convert.FromBase64String(cipherText);

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Encoding.UTF8.GetBytes(secretKey.PadRight(32, '\0').Substring(0, 32));
                aesAlg.IV = new byte[16];

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msDecrypt = new MemoryStream(cipherBytes))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            string dataWithTimestamp = srDecrypt.ReadToEnd();

                            string[] parts = dataWithTimestamp.Split(':', 3);
                            if (parts.Length != 3)
                            {
                                Console.WriteLine("Invalid format.");
                                return null;
                            }

                            long timestampTicks = long.Parse(parts[0]);
                            int expirationMinutes = int.Parse(parts[1]);
                            string originalData = parts[2];

                            DateTime encryptedTime = new DateTime(timestampTicks, DateTimeKind.Utc);
                            DateTime expirationTime = encryptedTime.AddMinutes(expirationMinutes);

                            if (DateTime.UtcNow > expirationTime)
                            {
                                Console.WriteLine("Token Expired.");
                                return null;
                            }

                            return originalData;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return null;
        }
    }
}