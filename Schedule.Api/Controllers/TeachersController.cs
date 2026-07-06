using Microsoft.AspNetCore.Mvc;
using Schedule.Core.Interfaces;
using Schedule.Core.Models;

namespace Schedule.Api.Controllers;

[ApiController]
[Route("api/[controller]")] // Маршрут: api/teachers
public class TeachersController : ControllerBase
{
    private readonly ITeacherRepository _teacherRepository;

    public TeachersController(ITeacherRepository teacherRepository)
    {
        _teacherRepository = teacherRepository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Teacher>>> GetAll()
    {
        var teachers = await _teacherRepository.GetAllAsync();
        return Ok(teachers);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Teacher>> GetById(int id)
    {
        var teacher = await _teacherRepository.GetByIdAsync(id);
        if (teacher == null)
        {
            return NotFound(new { Message = $"Викладача з ID {id} не знайдено." });
        }
        return Ok(teacher);
    }

    [HttpPost]
    public async Task<ActionResult<Teacher>> Create([FromBody] Teacher teacher)
    {
        if (string.IsNullOrWhiteSpace(teacher.Name))
        {
            return BadRequest(new { Message = "ПІБ викладача не може бути порожнім." });
        }

        var newId = await _teacherRepository.CreateAsync(teacher);
        teacher.Id = newId;

        return CreatedAtAction(nameof(GetById), new { id = teacher.Id }, teacher);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] Teacher teacher)
    {
        if (id != teacher.Id)
        {
            return BadRequest(new { Message = "ID в URL не збігається з ID в тілі запиту." });
        }

        var updated = await _teacherRepository.UpdateAsync(teacher);
        if (!updated)
        {
            return NotFound(new { Message = $"Не вдалося оновити. Викладача з ID {id} не знайдено." });
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _teacherRepository.DeleteAsync(id);
        if (!deleted)
        {
            return NotFound(new { Message = $"Не вдалося видалити. Викладача з ID {id} не знайдено." });
        }

        return NoContent();
    }
}
