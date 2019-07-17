using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PackageService.Models;
using PackageService.Services;
using Microsoft.Extensions.Logging;

namespace PackageService.Controllers
{

    [Route("api/models/package")]
    [ApiController]
    public class PackageController : ControllerBase
    {

        private readonly IPackageRepository packageRepository;
        private readonly ILogger<PackageController> _logger;

        public PackageController(
            IPackageRepository packageRepository,
            ILogger<PackageController> logger)
        {
            this.packageRepository = packageRepository;
            this._logger = logger;

        }

        // GET /api/models/package/package1
        [Route("/api/models/package/{id}", Name = "GetById")]
        [HttpGet]
        [ProducesResponseType(typeof(Package), 200)]
        [ProducesResponseType(typeof(void), 400)]
        [ProducesResponseType(typeof(void), 404)]
        public async Task<ActionResult> Get(string id)
        {
            _logger.LogInformation("In GET request with id: {Id}", id);

            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest();
            }

            var package = await this.packageRepository.GetPackageAsync(id);

            if (package == null)
            {
                _logger.LogDebug("Package id: {Id} not found", id);

                return NotFound();
            }
            else
            {
                return Ok(package);
            }
        }

        // PATCH  /api/models/package/package1
        [Route("/api/models/package/{id}", Name = "UpdateById")]
        [HttpPatch()]
        [ProducesResponseType(typeof(Package), 200)]
        [ProducesResponseType(typeof(void), 400)]
        [ProducesResponseType(typeof(void), 404)]
        public async Task<ActionResult> Patch(string id, [FromBody]Package package)
        {
            _logger.LogInformation("In PATCH request with id: {Id}", id);

            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest();
            }

            var returnedPackage = await packageRepository.UpdatePackageAsync(id, package);

            if (returnedPackage == null)
            {
                _logger.LogDebug("Package id: {Id} not found", id);

                return NotFound();
            }
            else
            {
                return Ok(returnedPackage);
            }
        }



        // PUT  /api/models/package/package1
        [Route("/api/models/package/{id}", Name = "CreateOrUpdate")]
        [HttpPut()]
        [ProducesResponseType(typeof(void), 200)]
        [ProducesResponseType(typeof(Package), 201)]
        [ProducesResponseType(typeof(void), 400)]
        [ProducesResponseType(typeof(void), 500)]
        public async Task<ActionResult> Put(string id, [FromBody]Package package)
        {
            _logger.LogInformation("In PUT request for: {Package}", package);

            //Validate that package Id is passed by the client
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest();
            }

            package.Id = id;

            try
            {
                // call addpackage      
                PackageUpsertStatusCode result = await packageRepository.AddPackageAsync(package);

                switch (result)
                {
                    case PackageUpsertStatusCode.Updated:
                        _logger.LogDebug("Package updated.");
                        return NoContent();

                    case PackageUpsertStatusCode.Created:
                        _logger.LogDebug("Package created.");
                        return CreatedAtRoute("GetById", new { id = package.Id }, package);

                    default:
                        return StatusCode(500);
                }

            }
            catch (EventException ex)
            {
                _logger.LogError(ex, "PUT Package failed.");

                return StatusCode(500);
            }
        }
    }
}
