using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using System.Threading.Tasks;
namespace SecureFileStorage.Services
{
    public class FirebaseAuthService
    {
        public FirebaseAuthService()
        {
            // Firebase Admin SDK'nın başlatılması
            FirebaseApp.Create(new AppOptions()
            {
                Credential = GoogleCredential.FromFile("firebase-config.json")
            });
        }

        // Firebase token'ını doğrulama
        public async Task<FirebaseToken> VerifyIdTokenAsync(string idToken)
        {
            var auth = FirebaseAuth.DefaultInstance;
            var decodedToken = await auth.VerifyIdTokenAsync(idToken);
            return decodedToken;  // FirebaseToken türünde dönecektir
        }
    }
}