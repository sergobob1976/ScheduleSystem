using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using Schedule.Core.Interfaces;
using Schedule.Core.Models;

namespace Schedule.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SemestersController : ControllerBase
{
    private readonly ISemesterRepository _semesterRepository;

    public SemestersController(
        ISemesterRepository semesterRepository)
    {
        _semesterRepository = semesterRepository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Semester>>> GetAll()
    {
        var semesters =
            await _semesterRepository.GetAllAsync();

        return Ok(semesters);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Semester>> GetById(int id)
    {
        var semester =
            await _semesterRepository.GetByIdAsync(id);

        if (semester == null)
        {
            return NotFound(new
            {
                Message =
                    $"Семестр з ID {id} не знайдено."
            });
        }

        return Ok(semester);
    }

    [HttpPost]
    public async Task<ActionResult<Semester>> Create(
        [FromBody] Semester semester)
    {
        var validationResult = Validate(semester);

        if (validationResult != null)
        {
            return validationResult;
        }

        try
        {
            int newId =
                await _semesterRepository.CreateAsync(
                    semester);

            semester.Id = newId;

            return CreatedAtAction(
                nameof(GetById),
                new { id = semester.Id },
                semester);
        }
        catch (MySqlException ex)
            when (ex.Number == 1062)
        {
            return Conflict(new
            {
                Message =
                    "Семестр із такою назвою вже існує."
            });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] Semester semester)
    {
        if (id != semester.Id)
        {
            return BadRequest(new
            {
                Message =
                    "ID у URL не збігається з ID у тілі запиту."
            });
        }

        var current =
            await _semesterRepository.GetByIdAsync(id);

        if (current == null)
        {
            return NotFound(new
            {
                Message =
                    $"Семестр з ID {id} не знайдено."
            });
        }

        var validationResult = Validate(semester);

        if (validationResult != null)
        {
            return validationResult;
        }

        try
        {
            bool updated =
                await _semesterRepository.UpdateAsync(
                    semester);

            if (!updated)
            {
                return NotFound(new
                {
                    Message =
                        $"Не вдалося оновити семестр з ID {id}."
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
                    "Семестр із такою назвою вже існує."
            });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var semester =
            await _semesterRepository.GetByIdAsync(id);

        if (semester == null)
        {
            return NotFound(new
            {
                Message =
                    $"Семестр з ID {id} не знайдено."
            });
        }

        try
        {
            bool deleted =
                await _semesterRepository.DeleteAsync(id);

            if (!deleted)
            {
                return NotFound(new
                {
                    Message =
                        $"Не вдалося видалити семестр з ID {id}."
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
                    "Неможливо видалити семестр, оскільки " +
                    "він уже використовується в навантаженні " +
                    "або розкладі."
            });
        }
    }

    private ActionResult? Validate(Semester semester)
    {
        if (string.IsNullOrWhiteSpace(semester.Name))
        {
            return BadRequest(new
            {
                Message =
                    "Назва семестру не може бути порожньою."
            });
        }

        if (semester.StartDate == default)
        {
            return BadRequest(new
            {
                Message =
                    "Потрібно вказати дату початку семестру."
            });
        }

        if (semester.EndDate == default)
        {
            return BadRequest(new
            {
                Message =
                    "Потрібно вказати дату завершення семестру."
            });
        }

        if (semester.EndDate < semester.StartDate)
        {
            return BadRequest(new
            {
                Message =
                    "Дата завершення не може бути раніше " +
                    "дати початку семестру."
            });
        }

        semester.Name = semester.Name.Trim();

        return null;
    }
}