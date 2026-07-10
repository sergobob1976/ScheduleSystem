using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using Schedule.Core.Interfaces;
using Schedule.Core.Models;

namespace Schedule.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TeachersController : ControllerBase
{
    private readonly ITeacherRepository _teacherRepository;

    public TeachersController(
        ITeacherRepository teacherRepository)
    {
        _teacherRepository = teacherRepository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Teacher>>> GetAll()
    {
        var teachers =
            await _teacherRepository.GetAllAsync();

        return Ok(teachers);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Teacher>> GetById(int id)
    {
        var teacher =
            await _teacherRepository.GetByIdAsync(id);

        if (teacher == null)
        {
            return NotFound(new
            {
                Message =
                    $"Викладача з ID {id} не знайдено."
            });
        }

        return Ok(teacher);
    }

    [HttpPost]
    public async Task<ActionResult<Teacher>> Create(
        [FromBody] Teacher teacher)
    {
        var validationResult = Validate(teacher);

        if (validationResult != null)
        {
            return validationResult;
        }

        try
        {
            int newId =
                await _teacherRepository.CreateAsync(
                    teacher);

            teacher.Id = newId;

            return CreatedAtAction(
                nameof(GetById),
                new { id = teacher.Id },
                teacher);
        }
        catch (MySqlException ex)
            when (ex.Number == 1062)
        {
            return Conflict(new
            {
                Message =
                    "Викладач із таким ПІБ уже існує."
            });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] Teacher teacher)
    {
        if (id != teacher.Id)
        {
            return BadRequest(new
            {
                Message =
                    "ID у URL не збігається з ID у тілі запиту."
            });
        }

        var current =
            await _teacherRepository.GetByIdAsync(id);

        if (current == null)
        {
            return NotFound(new
            {
                Message =
                    $"Викладача з ID {id} не знайдено."
            });
        }

        var validationResult = Validate(teacher);

        if (validationResult != null)
        {
            return validationResult;
        }

        try
        {
            bool updated =
                await _teacherRepository.UpdateAsync(
                    teacher);

            if (!updated)
            {
                return NotFound(new
                {
                    Message =
                        $"Не вдалося оновити викладача з ID {id}."
                });
            }

            return NoContent();
        }
        catch (MySqlException ex)
            when (ex.Number == 1062)
        {
            return Conflict(new
            {
                Message =
                    "Викладач із таким ПІБ уже існує."
            });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var teacher =
            await _teacherRepository.GetByIdAsync(id);

        if (teacher == null)
        {
            return NotFound(new
            {
                Message =
                    $"Викладача з ID {id} не знайдено."
            });
        }

        try
        {
            bool deleted =
                await _teacherRepository.DeleteAsync(id);

            if (!deleted)
            {
                return NotFound(new
                {
                    Message =
                        $"Не вдалося видалити викладача з ID {id}."
                });
            }

            return NoContent();
        }
        catch (MySqlException ex)
            when (ex.Number == 1451)
        {
            return Conflict(new
            {
                Message =
                    "Неможливо видалити викладача, оскільки " +
                    "для нього вже задано навантаження, " +
                    "призначення або заняття в розкладі."
            });
        }
    }

    private ActionResult? Validate(Teacher teacher)
    {
        if (string.IsNullOrWhiteSpace(teacher.Name))
        {
            return BadRequest(new
            {
                Message =
                    "ПІБ викладача не може бути порожнім."
            });
        }

        teacher.Name = teacher.Name.Trim();
        teacher.Position = string.IsNullOrWhiteSpace(
            teacher.Position)
            ? null
            : teacher.Position.Trim();

        if (teacher.Name.Length > 100)
        {
            return BadRequest(new
            {
                Message =
                    "ПІБ викладача не може містити більше 100 символів."
            });
        }

        if (teacher.Position?.Length > 100)
        {
            return BadRequest(new
            {
                Message =
                    "Посада не може містити більше 100 символів."
            });
        }

        return null;
    }
}