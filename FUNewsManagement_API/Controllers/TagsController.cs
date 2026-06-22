using DataAccessObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Repository.Interface;
using System.Threading.Tasks;

namespace FUNewsManagement_API.Controllers
{
    public class TagsController : ODataController
    {
        private readonly ITagRepository _tagRepository;

        public TagsController(ITagRepository tagRepository)
        {
            _tagRepository = tagRepository;
        }

        [EnableQuery]
        public IActionResult Get()
        {
            return Ok(_tagRepository.GetAllAsQueryable());
        }

        [EnableQuery]
        public async Task<IActionResult> Get([FromRoute] int key)
        {
            var item = await _tagRepository.GetByIdAsync(key);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [Authorize(Roles = "1")]
        public async Task<IActionResult> Post([FromBody] Tag tag)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            await _tagRepository.AddAsync(tag);
            return Created(tag);
        }
    }
}
