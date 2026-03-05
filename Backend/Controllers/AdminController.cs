using ExamNest.Models;
using ExamNest.Models.DTOs.User;
using ExamNest.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamNest.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    //[Authorize]
    public class AdminController : Controller
    {
        private readonly AdminServices _adminServices;
        public AdminController(AdminServices adminServices) {
            _adminServices = adminServices;
        }

        // teacher side
        [HttpGet]
        public async Task<IActionResult> GetAllTeacher()
        {
            var teachers = await _adminServices.GetAllAsync();
            return Ok(teachers);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTeacherById(int id)
        {
            var teacher = await _adminServices.GetByIdAsync(id);
            if (teacher == null) return NotFound();

            return Ok(teacher);
        }

        [HttpPost("AddTeacher")]
        public async Task<IActionResult> CreateTeacher(UserCreateDTO dto)
        {
            var teacher = await _adminServices.CreateAsync(dto);
            return Ok(teacher);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTeacher(int id, UserUpdateDTO dto)
        {
            var result = await _adminServices.UpdateAsync(id, dto);
            if (!result) return NotFound();

            return Ok("Teacher Updated Successfully");
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTeacher(int id)
        {
            var result = await _adminServices.DeleteAsync(id);
            if (!result) return NotFound();

            return Ok("Teacher Deleted Successfully");
        }



    }
}
