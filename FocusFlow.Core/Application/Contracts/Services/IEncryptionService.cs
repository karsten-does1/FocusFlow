namespace FocusFlow.Core.Application.Contracts.Services
{
    
    public interface IEncryptionService
    {
      
        string Encrypt(string plainText);
        string Decrypt(string encryptedText);
    }
}

