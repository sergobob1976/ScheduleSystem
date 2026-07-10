using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using Schedule.Core.Interfaces;
using Schedule.Core.Models;

namespace Schedule.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TeacherSemesterLoadsController : ControllerBase
{
    private readonly ITeacherSemesterLoadRepository _repository;
    private readonly ITeacherRepository _teacherRepository;
    private readonly ISemesterRepository _semesterRepository;

    public TeacherSemesterLoadsController(
        ITeacherSemesterLoadRepository repository,
        ITeacherRepository teacherRepository,
        ISemesterRepository semesterRepository)
    {
        _repository = repository;
        _teacherRepository = teacherRepository;
        _semesterRepository = semesterRepository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TeacherSemesterLoad>>> GetAll(
        [FromQuery] int? semesterId,
        [FromQuery] int? teacherId)
    {
        if (semesterId.HasValue)
        {
            var semesterLoads =
                await _repository.GetBySemesterIdAsync(semesterId.Value);

            return Ok(semesterLoads);
        }

        if (teacherId.HasValue)
        {
            var teacherLoads =
                await _repository.GetByTeacherIdAsync(teacherId.Value);

            return Ok(teacherLoads);
        }

        var loads = await _repository.GetAllAsync();

        return Ok(loads);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TeacherSemesterLoad>> GetById(int id)
    {
        var teacherSemesterLoad = await _repository.GetByIdAsync(id);

        if (teacherSemesterLoad == null)
        {
            return NotFound(new
            {
                Message =
                    $"Навантаження викладача на семестр з ID {id} не знайдено."
            });
        }

        return Ok(teacherSemesterLoad);
    }

    [HttpGet("teacher/{teacherId:int}/semester/{semesterId:int}")]
    public async Task<ActionResult<TeacherSemesterLoad>> GetByTeacherAndSemester(
        int teacherId,
        int semesterId)
    {
        var teacherSemesterLoad =
            await _repository.GetByTeacherAndSemesterAsync(
                teacherId,
                semesterId);

        if (teacherSemesterLoad == null)
        {
            return NotFound(new
            {
                Message =
                    "Для цього викладача в обраному семестрі " +
                    "загальне навантаження ще не вказано."
            });
        }

        return Ok(teacherSemesterLoad);
    }

    [HttpPost]
    public async Task<ActionResult<TeacherSemesterLoad>> Create(
        [FromBody] TeacherSemesterLoad teacherSemesterLoad)
    {
        var validationResult =
            await ValidateAsync(teacherSemesterLoad);

        if (validationResult != null)
        {
            return validationResult;
        }

        var existing =
            await _repository.GetByTeacherAndSemesterAsync(
                teacherSemesterLoad.TeacherId,
                teacherSemesterLoad.SemesterId);

        if (existing != null)
        {
            return Conflict(new
            {
                Message =
                    "Загальне навантаження цього викладача " +
                    "для обраного семестру вже існує."
            });
        }

        try
        {
            int newId =
                await _repository.CreateAsync(teacherSemesterLoad);

            var created = await _repository.GetByIdAsync(newId);

            if (created == null)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        Message =
                            "Навантаження створено, але не вдалося " +
                            "отримати його з бази даних."
                    });
            }

            return CreatedAtAction(
                nameof(GetById),
                new { id = created.Id },
                created);
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            return Conflict(new
            {
                Message =
                    "Загальне навантаження цього викладача " +
                    "для обраного семестру вже існує."
            });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] TeacherSemesterLoad teacherSemesterLoad)
    {
        if (id != teacherSemesterLoad.Id)
        {
            return BadRequest(new
            {
                Message =
                    "ID у URL не збігається з ID у тілі запиту."
            });
        }

        var current = await _repository.GetByIdAsync(id);

        if (current == null)
        {
            return NotFound(new
            {
                Message =
                    $"Навантаження викладача на семестр з ID {id} не знайдено."
            });
        }

        var validationResult =
            await ValidateAsync(teacherSemesterLoad);

        if (validationResult != null)
        {
            return validationResult;
        }

        var duplicate =
            await _repository.GetByTeacherAndSemesterAsync(
                teacherSemesterLoad.TeacherId,
                teacherSemesterLoad.SemesterId);

        if (duplicate != null && duplicate.Id != id)
        {
            return Conflict(new
            {
                Message =
                    "Загальне навантаження цього викладача " +
                    "для обраного семестру вже існує."
            });
        }

        try
        {
            bool updated =
                await _repository.UpdateAsync(teacherSemesterLoad);

            if (!updated)
            {
                return NotFound(new
                {
                    Message =
                        $"Не вдалося оновити навантаження з ID {id}."
                });
            }

            return NoContent();
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            return Conflict(new
            {
                Message =
                    "Загальне навантаження цього викладача " +
                    "для обраного семестру вже існує."
            });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var current = await _repository.GetByIdAsync(id);

        if (current == null)
        {
            return NotFound(new
            {
                Message =
                    $"Навантаження викладача на семестр з ID {id} не знайдено."
            });
        }

        bool deleted = await _repository.DeleteAsync(id);

        if (!deleted)
        {
            return NotFound(new
            {
                Message =
                    $"Не вдалося видалити навантаження з ID {id}."
            });
        }

        return NoContent();
    }

    private async Task<ActionResult?> ValidateAsync(
        TeacherSemesterLoad teacherSemesterLoad)
    {
        if (teacherSemesterLoad.TeacherId <= 0)
        {
            return BadRequest(new
            {
                Message = "Потрібно обрати викладача."
            });
        }

        if (teacherSemesterLoad.SemesterId <= 0)
        {
            return BadRequest(new
            {
                Message = "Потрібно обрати семестр."
            });
        }

        if (teacherSemesterLoad.PlannedHours < 0)
        {
            return BadRequest(new
            {
                Message =
                    "Планова кількість годин не може бути від'ємною."
            });
        }

        var teacher =
            await _teacherRepository.GetByIdAsync(
                teacherSemesterLoad.TeacherId);

        if (teacher == null)
        {
            return BadRequest(new
            {
                Message = "Обраного викладача не знайдено."
            });
        }

        var semester =
            await _semesterRepository.GetByIdAsync(
                teacherSemesterLoad.SemesterId);

        if (semester == null)
        {
            return BadRequest(new
            {
                Message = "Обраного семестру не знайдено."
            });
        }

        return null;
    }
}
