using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        public UsersController()
        {
            
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            return Ok("UserController is working!");
        }
    }
}
