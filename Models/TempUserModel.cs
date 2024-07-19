using Microsoft.AspNetCore.Identity;
using SimpleHashing.Net;
namespace LeafLINQWeb.Models;

public class TempUserModel
{
    public int Id { get; set; }
    private int _confCode = 0;
    public int ConfirmationCode
    {
        get
        {
            return _confCode;
        }
        set
        {
            // If we are not clearing the code then hash a new code.
            // 0 = we are finished with it and need to clear it, so it  
            // cannot be reused.
            if (value > 0)
            {
                hashIt(value);
                _confCode = value;
            } else
            {
                _confCode = 0;
            }
        }

    }
    public string EncryptedCode { get; set; }
    private SimpleHash hasher;

    public TempUserModel()
    {
        hasher = new SimpleHash();
    }

    public void hashIt(int hashIt)
    {
        EncryptedCode = hasher.Compute(hashIt.ToString(), 50000);
        ConfirmationCode = 0;
    }

    public bool VerifyHash(string hashIn)
    {
        return hasher.Verify(hashIn, EncryptedCode);
    }
}

