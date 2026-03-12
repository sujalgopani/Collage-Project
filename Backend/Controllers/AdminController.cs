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
            var teachers = await _adminServices.GetAllTeacher();
            return Ok(teachers);
        }


        [HttpGet]
        public async Task<IActionResult> GetAllStudent()
        {
            var teachers = await _adminServices.GetAllStudent();
            return Ok(teachers);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            var teacher = await _adminServices.GetByIdAsync(id);
            if (teacher == null) return NotFound();

            return Ok(teacher);
        }

        [HttpPost("AddTeacher")]
        public async Task<IActionResult> CreateUser(UserCreateDTO dto)
        {
            var teacher = await _adminServices.CreateAsync(dto);
            return Ok(teacher);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, UserUpdateDTO dto)
        {
            var result = await _adminServices.UpdateAsync(id, dto);
            if (!result) return NotFound();

            return Ok("Teacher Updated Successfully");
        }

        [HttpPut("{id}")] // student
        public async Task<IActionResult> UpdateStudent(int id, UserUpdateDTO dto)
        {
            var result = await _adminServices.UpdateStudentAsync(id, dto);
            if (!result) return NotFound();

            return Ok("student Updated Successfully");
        }



        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var result = await _adminServices.DeleteAsync(id);
            if (!result) return NotFound();
            return Ok("Deleted Successfully");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStudent(int id)
        {
            var result = await _adminServices.DeleteStudentAsync(id);
            if (!result) return NotFound();
            return Ok("Deleted Successfully");
        }

        // course side

        [HttpGet]
        public async Task<IActionResult> GetAllCourses()
        {
            var allcourse = await _adminServices.GetAllCourses();
			var result = allcourse.Select(c => new {
				c.CourseId,
				c.Title,
				c.Description,
				c.Fees,
				c.StartDate,
				c.EndDate,
				c.IsPublished,
                c.CreatedAt,
                TeacherName = c.Teacher != null ? c.Teacher.Username : "Unknown",

			}).ToList();

			if (result == null)
            {
                return Conflict("Course Is Not Found !!!");
            }
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> PublishCourse([FromBody] int courseId)
        {
            var course = await _adminServices.PublishCourse(courseId);
            if(course == null)
            {
                return NotFound("Course Is Not Found !!");
            }
            return Ok(new
            {
                message = "Course Was Published"
            });
        }


    }
}
