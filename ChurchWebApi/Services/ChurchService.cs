using ChurchWebApi.Controllers;
using Microsoft.Extensions.Logging;

namespace ChurchWebApi.Services
{
    public class ChurchService : IChurchService
    {
        private readonly ILogger<ChurchService> _log;
        private readonly IChurchService _churchService;
        private readonly IDatabaseConnector _databaseConnector;

        public ChurchService(ILogger<ChurchService> logger, IDatabaseConnector databaseConnector)
        {
            _log = logger;
            _databaseConnector = databaseConnector;
        }

        public void Register()
        {
            _log.LogInformation("Registering");
        }
    }
}
