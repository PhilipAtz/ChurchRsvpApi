using ChurchWebApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ChurchWebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ChurchApiController : ControllerBase
    {
        private readonly ILogger<ChurchApiController> _log;
        private readonly IChurchService _churchService;
        private readonly IDatabaseConnector _databaseConnector;

        public ChurchApiController(ILogger<ChurchApiController> logger, IChurchService churchService)
        {
            _log = logger;
            _churchService = churchService;
        }

        [HttpGet]
        public string Register()
        {
            _churchService.Register();
            return "OK";
        }
    }
}
