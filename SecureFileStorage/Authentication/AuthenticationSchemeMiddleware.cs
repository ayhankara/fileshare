using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
namespace SecureFileStorage.API.Authentication
{
    public class AuthenticationSchemeMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;

        public AuthenticationSchemeMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            _next = next;
            _configuration = configuration;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Token'ı al
            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

            if (!string.IsNullOrEmpty(token))
            {
                // Token'ı analiz ederek şemayı belirle (örneğin, belirli bir claim'e göre)
                string scheme = DetermineAuthenticationScheme(token, context);

                // Authentication şemasını ayarla
                context.Features.Set<IAuthenticationSchemeFeature>(new AuthenticationSchemeFeature { AuthenticationSchemes = new string[] { scheme } });
            }

            await _next(context);
        }

        private string DetermineAuthenticationScheme(string token, HttpContext context)
        {
            // **ÖNEMLİ:** Bu kısım tamamen sizin token'larınızın yapısına bağlıdır.
            // Buradaki örnek, token içinde belirli bir claim'in varlığına göre şema seçimi yapmaktadır.
            // JWT token'ınızın yapısını inceleyerek en uygun yöntemi belirlemelisiniz.

            // Örnek: Token içinde "firebase" adında bir claim varsa Firebase şemasını kullan
            var tokenHandler = context.RequestServices.GetRequiredService<System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler>();
            if (tokenHandler.CanReadToken(token))
            {
                var jwtToken = tokenHandler.ReadJwtToken(token);

                // Firebase claim'ini kontrol et.  Claim'in adını ve içeriğini kendi yapınıza göre ayarlayın.
                if (jwtToken.Claims.Any(c => c.Type == "firebase_auth"))
                {
                    return "JwtBearer"; // Firebase scheme adı (program.cs'de tanımladığınız)
                }
            }

            // Başka bir kontrol ekleyebilir veya varsayılan şemayı belirleyebilirsiniz.
            return "JwtBearer"; // Varsayılan olarak JWT şeması
        }
    }

    // IAuthenticationSchemeFeature arayüzünü tanımla
    public interface IAuthenticationSchemeFeature
    {
        string[] AuthenticationSchemes { get; set; }
    }

    // AuthenticationSchemeFeature sınıfını tanımla
    public class AuthenticationSchemeFeature : IAuthenticationSchemeFeature
    {

        public string[] AuthenticationSchemes { get; set; }

        public AuthenticationSchemeFeature()
        {
            // AuthenticationSchemes özelliğine varsayılan bir değer atayın (örneğin, boş bir dizi):
            AuthenticationSchemes = Array.Empty<string>();
        }
    }
}
