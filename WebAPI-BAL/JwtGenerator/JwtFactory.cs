﻿using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Threading.Tasks;
using Common;
using Microsoft.Extensions.Options;
using WebAPI_ViewModel.DTO;

namespace WebAPI_BAL.JwtGenerator
{
    public interface IJwtFactory
    {
        Task<string> GenerateEncodedToken(string userName, ClaimsIdentity identity, string audience);
        Task<string> GenerateEncodedTokenForChat(GetWebsiteInfoFromDomainViewModel websiteInfo, string audience);
        ClaimsIdentity GenerateClaimsIdentity(string userName, string id, string remoteIp);
        string GenerateRefreshToken();
    }
    public class JwtFactory : IJwtFactory
    {
        private readonly JwtIssuerOptions _jwtOptions;
        private readonly JwtChatIssuerOptions _jwtChatOptions;

        public JwtFactory(IOptions<JwtIssuerOptions> jwtOptions, IOptions<JwtChatIssuerOptions> jwtChatOptions)
        {
            _jwtOptions = jwtOptions.Value;
            _jwtChatOptions = jwtChatOptions.Value;
            ThrowIfInvalidOptions(_jwtOptions);
            ThrowIfInvalidOptions(_jwtChatOptions);
        }

        public async Task<string> GenerateEncodedToken(string userName, ClaimsIdentity identity, string audience)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userName),
                new Claim(JwtRegisteredClaimNames.Jti, await _jwtOptions.JtiGenerator()),
                new Claim(JwtRegisteredClaimNames.Iat, ToUnixEpochDate(_jwtOptions.IssuedAt).ToString(),
                    ClaimValueTypes.Integer64),
                identity.FindFirst(Constants.Strings.JwtClaimIdentifiers.Rol),
                identity.FindFirst(Constants.Strings.JwtClaimIdentifiers.Id),
                identity.FindFirst(AppConstants.RemoteIp),
            };

            // Create the JWT security token and encode it.
            var jwt = new JwtSecurityToken(
                issuer: _jwtOptions.Issuer,
                //audience: audience,
                audience: _jwtOptions.Audience,
                claims: claims,
                notBefore: _jwtOptions.NotBefore,
                expires: _jwtOptions.Expiration,
                signingCredentials: _jwtOptions.SigningCredentials);

            var encodedJwt = new JwtSecurityTokenHandler().WriteToken(jwt);

            return encodedJwt;
        }

        public async Task<string> GenerateEncodedTokenForChat(GetWebsiteInfoFromDomainViewModel websiteInfo, string audience)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, websiteInfo.DomainName),
                new Claim(JwtRegisteredClaimNames.Jti, await _jwtOptions.JtiGenerator()),
                new Claim(JwtRegisteredClaimNames.Iat, ToUnixEpochDate(_jwtOptions.IssuedAt).ToString(),
                    ClaimValueTypes.Integer64),
                new Claim("W_I", Convert.ToString(websiteInfo.WebsiteId),
                    ClaimValueTypes.Integer64),
                new Claim("P_L", Convert.ToString(websiteInfo.PriorityLanguage),
                    ClaimValueTypes.Integer),
                new Claim("T_Z", Convert.ToString(websiteInfo.TimeZone),
                    ClaimValueTypes.String),
            };

            // Create the JWT security token and encode it.
            var jwt = new JwtSecurityToken(
                issuer: _jwtChatOptions.Issuer,
                audience: audience,
                //audience: _jwtOptions.Audience,
                claims: claims,
                notBefore: _jwtChatOptions.NotBefore,
                expires: _jwtChatOptions.Expiration,
                signingCredentials: _jwtChatOptions.SigningCredentials);

            var encodedJwt = new JwtSecurityTokenHandler().WriteToken(jwt);

            return encodedJwt;
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }


        public ClaimsIdentity GenerateClaimsIdentity(string userName, string id, string remoteIp)
        {
            return new ClaimsIdentity(new GenericIdentity(userName, "Token"), new[]
            {
                new Claim(Constants.Strings.JwtClaimIdentifiers.Id, id),
                new Claim(Constants.Strings.JwtClaimIdentifiers.Rol, Constants.Strings.JwtClaims.ApiAccess),
                new Claim(AppConstants.RemoteIp,remoteIp)
            });
        }

        /// <returns>Date converted to seconds since Unix epoch (Jan 1, 1970, midnight UTC).</returns>
        private static long ToUnixEpochDate(DateTime date)
          => (long)Math.Round((date.ToUniversalTime() -
                               new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero))
                              .TotalSeconds);

        private static void ThrowIfInvalidOptions(IJwtOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            if (options.ValidFor <= TimeSpan.Zero)
            {
                throw new ArgumentException("Must be a non-zero TimeSpan.", nameof(JwtIssuerOptions.ValidFor));
            }

            if (options.SigningCredentials == null)
            {
                throw new ArgumentNullException(nameof(JwtIssuerOptions.SigningCredentials));
            }

            if (options.JtiGenerator == null)
            {
                throw new ArgumentNullException(nameof(JwtIssuerOptions.JtiGenerator));
            }
        }
    }

}
