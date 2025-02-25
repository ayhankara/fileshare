using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;

namespace SecureFileStorage.API.Authentication
{
    //public class FirebaseAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    //{
    //    public FirebaseAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder, clock)
    //    {
    //    } 
     
    //    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    //    { 
    //       return AuthenticateResult.NoResult();
    //    }
    //}

    public class FirebaseAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly TimeProvider _timeProvider;

        public FirebaseAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, TimeProvider timeProvider) : base(options, logger, encoder)
        {
            _timeProvider = timeProvider ?? TimeProvider.System; // TimeProvider.System varsayılan değeri
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            await Task.Run(() =>
            {
                // ... CPU yoğun işlem ...
            });

            // ...
            return AuthenticateResult.NoResult(); 
        }
    }
}
