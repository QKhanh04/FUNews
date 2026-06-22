using DataAccessObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Repository.Interface;
using Service.Interface;

namespace FUNewsManagement_API.Controllers
{
    [Authorize]
    public class AccountsController : ODataController
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IAccountService _accountService;

        public AccountsController(IAccountRepository accountRepository, IAccountService accountService)
        {
            _accountRepository = accountRepository;
            _accountService = accountService;
        }

        [EnableQuery]
        public IActionResult Get()
        {
            return Ok(_accountRepository.GetAllAsQueryable());
        }

        [EnableQuery]
        public async Task<IActionResult> Get([FromRoute] short key)
        {
            var item = await _accountRepository.GetByIdAsync(key);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [Authorize(Roles = "0")]
        public async Task<IActionResult> Post([FromBody] SystemAccount account)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _accountService.AddAccount(account);
            if (result.IsSuccess) return Created(account);
            return BadRequest(result.Message);
        }

        [Authorize(Roles = "0")]
        public async Task<IActionResult> Put([FromRoute] short key, [FromBody] SystemAccount account)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (key != account.AccountId) return BadRequest();

            // Needs Current User ID logic
            var currentUserIdStr = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            short.TryParse(currentUserIdStr, out short currentUserId);

            var result = await _accountService.UpdateAccount(account, currentUserId);
            if (result.IsSuccess) return Updated(account);
            return BadRequest(result.Message);
        }

        [Authorize(Roles = "0")]
        public async Task<IActionResult> Delete([FromRoute] short key)
        {
            var result = await _accountService.DeleteAccount(key);
            if (result.IsSuccess) return NoContent();
            return BadRequest(result.Message);
        }
    }
}
