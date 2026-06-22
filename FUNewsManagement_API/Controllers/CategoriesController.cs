using DataAccessObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Repository.Interface;
using Service.Interface;

namespace FUNewsManagement_API.Controllers
{
    public class CategoriesController : ODataController
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly ICategoryService _categoryService;

        public CategoriesController(ICategoryRepository categoryRepository, ICategoryService categoryService)
        {
            _categoryRepository = categoryRepository;
            _categoryService = categoryService;
        }

        [EnableQuery]
        public IActionResult Get()
        {
            return Ok(_categoryRepository.GetAllAsQueryable());
        }

        [EnableQuery]
        public async Task<IActionResult> Get([FromRoute] short key)
        {
            var item = await _categoryRepository.GetByIdAsync(key);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [Authorize(Roles = "1")]
        public async Task<IActionResult> Post([FromBody] Category category)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _categoryService.AddCategoryAsync(category);
            if (result.IsSuccess) return Created(category);
            return BadRequest(result.Message);
        }

        [Authorize(Roles = "1")]
        public async Task<IActionResult> Put([FromRoute] short key, [FromBody] Category category)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (key != category.CategoryId) return BadRequest();

            var result = await _categoryService.UpdateCategoryAsync(category);
            if (result.IsSuccess) return Updated(category);
            return BadRequest(result.Message);
        }

        [Authorize(Roles = "1")]
        public async Task<IActionResult> Delete([FromRoute] short key)
        {
            var result = await _categoryService.DeleteCategoryAsync(key);
            if (result.IsSuccess) return NoContent();
            return BadRequest(result.Message);
        }
    }
}
