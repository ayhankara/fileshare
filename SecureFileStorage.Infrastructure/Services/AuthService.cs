using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SecureFileStorage.Infrastructure.Services.Interfaces;
using SecureFileStorage.Models;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
namespace SecureFileStorage.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;
        private readonly FirebaseAuth _firebaseAuth;
        private readonly HttpClient _httpClient;
        public AuthService(HttpClient httpClient, IConfiguration configuration, ILogger<AuthService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _configuration = configuration;

            // Firebase Admin SDK'sını başlat (tek seferlik)
            try
            {
                if (FirebaseAdmin.FirebaseApp.DefaultInstance == null)
                {
                    FirebaseApp.Create(new AppOptions()
                    {
                        Credential = GoogleCredential.FromFile("firebase-config.json"),
                        ProjectId = _configuration["Authentication:Audience"]
                    });
                }

                _firebaseAuth = FirebaseAuth.DefaultInstance;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Firebase initialization failed.");
                // Uygulamanın başlatılmasını durdurmak veya uygun bir hata işleme mekanizması kullanmak önemlidir.
                throw;
            }
        }

        //public async Task<LoginResponse> AuthenticateAsync(string email, string password,bool returnSecureToken)
        //{
        //    try
        //    {
        //        // Firebase'de email/password ile oturum açma, Admin SDK ile doğrudan desteklenmez.
        //        // Admin SDK, kullanıcıları oluşturmak, silmek, devre dışı bırakmak gibi yönetici görevleri için tasarlanmıştır.
        //        // Bu nedenle, email/password ile oturum açma işlemini istemci tarafında (örneğin, mobil veya web uygulamanızda) yapmanız gerekir.
        //        // İstemci tarafında başarılı bir oturum açma işleminden sonra, Firebase size bir JWT token'ı verecektir.
        //        // Bu JWT token'ı daha sonra API'nize göndererek kimlik doğrulaması yapabilirsiniz.
        //        // Bu örnekte, sadece istemci tarafında oturum açıldığını varsayarak, email adresini kontrol edeceğiz ve kullanıcı bilgilerini geri döneceğiz.
        //        // Gerçek bir uygulamada, istemci tarafından sağlanan JWT token'ı doğrulamanız GEREKİR.

        //        // Not: Güvenlik nedeniyle, production ortamında bu şekilde direkt password kontrolü YAPMAYIN!  JWT Token ile doğrulama KULLANIN.

        //        // Firebase'den kullanıcı bilgilerini al
        //        UserRecord userRecord = null;
        //        try
        //        {
        //            userRecord = await _firebaseAuth.GetUserByEmailAsync(email);
        //        }
        //        catch (FirebaseAuthException ex)
        //        {
        //            _logger.LogError(ex, $"User with email {email} not found.");
        //            return new LoginResponse
        //            {
        //                IsAuthenticated = false,
        //                ErrorMessage = "Invalid credentials."
        //            };
        //        }

        //        if (userRecord == null)
        //        {
        //            return new LoginResponse
        //            {
        //                IsAuthenticated = false,
        //                ErrorMessage = "Invalid credentials."
        //            };
        //        }

        //        // *** ÖNEMLİ GÜVENLİK UYARISI ***
        //        // Burada, gerçek bir uygulamada şifreyi VERITABANINDA SAKLANAN HASH ile karşılaştırmanız gerekir.
        //        // Bu örnekte, şifre karşılaştırması YAPILMAMAKTADIR! Bu ÇOK GÜVENSİZDİR!
        //        // Production ortamında KESINLIKLE BU ŞEKILDE KULLANMAYIN!

        //        var authenticatedUser = new AuthenticatedUser
        //        {
        //            Uid = userRecord.Uid,
        //            Email = userRecord.Email,
        //            DisplayName = userRecord.DisplayName,
        //            PhotoUrl = userRecord.PhotoUrl, 
        //            //ExpiresIn =userRecord.ex
        //        };

        //        return new LoginResponse
        //        {
        //            IsAuthenticated = true,
        //            User = authenticatedUser
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "An unexpected error occurred during authentication.");
        //        return new LoginResponse
        //        {
        //            IsAuthenticated = false,
        //            ErrorMessage = "An unexpected error occurred."
        //        };
        //    }
        //}
        public async Task<LoginResponse> AuthenticateAsync(string email, string password, bool returnSecureToken)
        {
            try
            {
                // Firebase kimlik doğrulama REST API isteği
                var requestBody = new
                {
                    email = email,
                    password = password,
                    returnSecureToken = returnSecureToken
                };

                var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            
                // Firebase API Key'i Configuration'dan çekiyoruz.
                string firebaseApiKey = _configuration["Authentication:ApiKey"]??"";
                string firebaseAuthUrl = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={firebaseApiKey}";

                var response = await _httpClient.PostAsync(firebaseAuthUrl, jsonContent);

                if (!response.IsSuccessStatusCode)
                {
                    return new LoginResponse
                    {
                        IsAuthenticated = false,
                        ErrorMessage = "Invalid credentials."
                    };
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                var firebaseResponse = JsonSerializer.Deserialize<FirebaseLoginResponse>(responseBody) ?? new FirebaseLoginResponse() ;

                // Kullanıcı bilgilerini modelimize aktarıyoruz
                var authenticatedUser = new AuthenticatedUser
                {
                    Uid = firebaseResponse.LocalId ?? "",
                    Email = firebaseResponse.Email,
                    DisplayName = firebaseResponse.DisplayName,
                    IdToken = firebaseResponse.IdToken,
                    RefreshToken = firebaseResponse.RefreshToken,
                    ExpiresIn = firebaseResponse.ExpiresIn
                };


                 

                return new LoginResponse
                {
                    IsAuthenticated = true,
                    User = authenticatedUser
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred during authentication.");
                return new LoginResponse
                {
                    IsAuthenticated = false,
                    ErrorMessage = "An unexpected error occurred."
                };
            }
        }
        public async Task<LoginResponse> CreateUserAsync(string email, string password, string displayName )
        {
            try
            {
                var createRequest = new UserRecordArgs()
                {
                    Email = email,
                    Password = password,
                    DisplayName = displayName,
                };

                UserRecord userRecord = await _firebaseAuth.CreateUserAsync(createRequest);

                _logger.LogInformation($"Successfully created user: {userRecord.Uid}");

                var authenticatedUser = new AuthenticatedUser
                {
                    Uid = userRecord.Uid,
                    Email = userRecord.Email,
                    DisplayName = userRecord.DisplayName,
                    PhotoUrl = userRecord.PhotoUrl
                };

                return new LoginResponse
                {
                    IsAuthenticated = true,
                    User = authenticatedUser
                };
            }
            catch (FirebaseAuthException ex)
            {
                _logger.LogError(ex, $"Error creating user: {ex.Message}");
                return new LoginResponse
                {
                    IsAuthenticated = false,
                    ErrorMessage = $"Error creating user: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred during user creation.");
                return new LoginResponse
                {
                    IsAuthenticated = false,
                    ErrorMessage = "An unexpected error occurred."
                };
            }
        }
    }
}

