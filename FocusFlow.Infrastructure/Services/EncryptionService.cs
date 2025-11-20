using System;
using System.Security.Cryptography;
using System.Text;
using FocusFlow.Core.Application.Contracts.Services;
using Microsoft.Extensions.Configuration;

namespace FocusFlow.Infrastructure.Services
{
    
    public sealed class EncryptionService : IEncryptionService
    {
        private readonly byte[] _key;
        private const int KeySize = 32;
        private const int IvSize = 16;

        public EncryptionService(IConfiguration configuration)
        {
            var encryptionKey = configuration["Encryption:Key"];
            if (string.IsNullOrWhiteSpace(encryptionKey))
            {
                throw new InvalidOperationException(
                    "Encryption key is not configured. Please set 'Encryption:Key' in appsettings.json");
            }

            using var sha256 = SHA256.Create();
            _key = sha256.ComputeHash(Encoding.UTF8.GetBytes(encryptionKey));
        }

        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return string.Empty;

            using var aes = Aes.Create();
            aes.Key = _key;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            var result = new byte[IvSize + encryptedBytes.Length];
            Buffer.BlockCopy(aes.IV, 0, result, 0, IvSize);
            Buffer.BlockCopy(encryptedBytes, 0, result, IvSize, encryptedBytes.Length);

            return Convert.ToBase64String(result);
        }

        public string Decrypt(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText)) return string.Empty;

            var encryptedBytes = Convert.FromBase64String(encryptedText);

            if (encryptedBytes.Length < IvSize) return string.Empty;

            var iv = new byte[IvSize];
            var cipherText = new byte[encryptedBytes.Length - IvSize];
            Buffer.BlockCopy(encryptedBytes, 0, iv, 0, IvSize);
            Buffer.BlockCopy(encryptedBytes, IvSize, cipherText, 0, cipherText.Length);

            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            var decryptedBytes = decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);

            return Encoding.UTF8.GetString(decryptedBytes);
        }
    }
}