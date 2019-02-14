using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Common;
using Common.Messages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Annotations;
using WebAPI_BAL;
using WebAPI_BAL.JwtGenerator;
using WebAPI_DataAccess.Context;
using WebAPI_Model;
using WebAPI_ViewModel.DTO;
using WebAPI_ViewModel.Response;

namespace WebAPI_Server.Controllers.v1
{
    /// <inheritdoc />
    [ApiVersion(ApiVersionNumber.V1)]
    [Route("api/chat")]
    [ApiController]
    public class ChatWindowController : ChatWindowBaseController
    {
        private IHttpContextAccessor _httpContextAccessor;
        private static readonly HttpClient Client = new HttpClient();
        private readonly ICommonStoreProcBusinessLogic<IApplicationDbContext> _cBalProc;
        private readonly ILogger<ChatWindowController> _logger;
        private readonly IJwtFactory _jwtFactory;
        private readonly JwtChatIssuerOptions _jwtChatOptions;

        /// <inheritdoc />
        public ChatWindowController(IHttpContextAccessor httpContextAccessor,
            ICommonStoreProcBusinessLogic<IApplicationDbContext> cBalProc,
            ILogger<ChatWindowController> logger, IJwtFactory jwtFactory, IOptions<JwtChatIssuerOptions> jwtChatOptions)
        {
            _cBalProc = cBalProc;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _jwtChatOptions = jwtChatOptions.Value;
            _jwtFactory = jwtFactory;
        }

        /// <summary>
        /// Check Website info from Origin
        /// </summary>
        /// <returns></returns>
        [SwaggerResponse(StatusCodes.Status200OK, type: typeof(ApiResponse<JwtToken>))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, type: typeof(ApiResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, type: typeof(ApiResponse))]
        [HttpPost("getWi")]
        [AllowAnonymous]
        public async Task<IActionResult> GetWebsiteIdForChatWindow()
        {
            if (!HttpContext.Request.Headers.ContainsKey(HttpRequestHeaders.Origin) ||
                (HttpContext.Request.Headers.ContainsKey(HttpRequestHeaders.Origin) &&
                 string.IsNullOrEmpty(HttpContext.Request.Headers[HttpRequestHeaders.Origin])))
                return BadRequest(ErrorMessages.Forbidden, null);

            string originValue = HttpContext.Request.Headers[HttpRequestHeaders.Origin];
            string userAgent = HttpContext.Request.Headers[HttpRequestHeaders.UserAgent];

            bool isBot = false; //Check flag for boot
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (isBot)
                return StatusCodeResult(StatusCodes.Status403Forbidden, null, ErrorMessages.Forbidden);

            Uri uri = new Uri(originValue);

            string domainName = uri.Host.Replace("www.", "") + (uri.PathAndQuery == "/" ? "" : uri.PathAndQuery);

            List<GetWebsiteInfoFromDomainViewModel> websiteInfo = _cBalProc
                .ExecuteStoreProcedure<GetWebsiteInfoFromDomainModel, GetWebsiteInfoFromDomainParam,
                    GetWebsiteInfoFromDomainViewModel, GetWebsiteInfoFromDomainParamViewModel>(
                    new GetWebsiteInfoFromDomainParamViewModel { DomainName = domainName }).ToList();
            if (websiteInfo.Any())
            {
                var websiteInfoData = websiteInfo.FirstOrDefault();
                if (websiteInfoData == null)
                    return StatusCodeResult(StatusCodes.Status401Unauthorized, null, ErrorMessages.UnAuthorized);

                JwtToken accessToken = await Tokens.GenerateJwtForChat(websiteInfoData, _jwtFactory, _jwtChatOptions, originValue);
                return Ok(accessToken, InfoMessages.CommonInfoMessage);
            }
            return StatusCodeResult(StatusCodes.Status401Unauthorized, null, ErrorMessages.UnAuthorized);
        }
    }
}