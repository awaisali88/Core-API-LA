using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebAPI_Server.AppStart;
using WebAPI_ViewModel.Response;

namespace WebAPI_Server.Controllers
{
    /// <inheritdoc />
    [Authorize]
    public class ChatWindowBaseController : BaseController
    {
    }
}