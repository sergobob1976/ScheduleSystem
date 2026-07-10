using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using Schedule.Core.Interfaces;
using Schedule.Core.Models;

namespace Schedule.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DisciplinesController : ControllerBase
{
    private readonly IDisciplineRepository _disciplineRepository;
    private readonly ISpecialtyRepository _specialtyRepository;

    public DisciplinesController(
        IDisciplineRepository disciplineRepository,
        ISpecialtyRepository specialtyRepository)
    {
        _disciplineRepository = disciplineRepository;
        _specialtyRepository = specialtyRepository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Discipline>>> GetAll(
        [FromQuery] int? specialtyId)
    {
        var disciplines =
            await _disciplineRepository.GetAllAsync();

        if (specialtyId.HasValue)
        {
            disciplines = disciplines.Where(
                x => x.SpecialtyId == specialtyId.Value);
        }

        return Ok(disciplines);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Discipline>> GetById(int id)
    {
        var discipline =
            await _disciplineRepository.GetByIdAsync(id);

        if (discipline == null)
        {
            return NotFound(new
            {
                Message =
                    $"Дисципліну з ID {id} не знайдено."
            });
        }

        return Ok(discipline);
    }

    [HttpPost]
    public async Task<ActionResult<Discipline>> Create(
        [FromBody] Discipline discipline)
    {
        var validationResult =
            await ValidateAsync(discipline);

        if (validationResult != null)
        {
            return validationResult;
        }

        ClearLegacyHours(discipline);

        try
        {
            int newId =
                await _disciplineRepository.CreateAsync(
                    discipline);

            var created =
                await _disciplineRepository.GetByIdAsync(newId);

            if (created == null)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        Message =
                            "Дисципліну створено, але не вдалося " +
                            "отримати її з бази даних."
                    });
            }

            return CreatedAtAction(
                nameof(GetById),
                new { id = created.Id },
                created);
        }
        catch (MySqlException ex)
            when (ex.Number == 1062)
        {
            return Conflict(new
            {
                Message =
                    "Дисципліна з такою назвою вже існує " +
                    "для обраної спеціальності."
            });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] Discipline discipline)
    {
        if (id != discipline.Id)
        {
            return BadRequest(new
            {
                Message =
                    "ID у URL не збігається з ID у тілі запиту."
            });
        }

        var current =
            await _disciplineRepository.GetByIdAsync(id);

        if (current == null)
        {
            return NotFound(new
            {
                Message =
                    $"Дисципліну з ID {id} не знайдено."
            });
        }

        var validationResult =
            await ValidateAsync(discipline);

        if (validationResult != null)
        {
            return validationResult;
        }

        ClearLegacyHours(discipline);

        try
        {
            bool updated =
                await _disciplineRepository.UpdateAsync(
                    discipline);

            if (!updated)
            {
                return NotFound(new
                {
                    Message =
                        $"Не вдалося оновити дисципліну з ID {id}."
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
                    "Дисципліна з такою назвою вже існує " +
                    "для обраної спеціальності."
            });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var discipline =
            await _disciplineRepository.GetByIdAsync(id);

        if (discipline == null)
        {
            return NotFound(new
            {
                Message =
                    $"Дисципліну з ID {id} не знайдено."
            });
        }

        try
        {
            bool deleted =
                await _disciplineRepository.DeleteAsync(id);

            if (!deleted)
            {
                return NotFound(new
                {
                    Message =
                        $"Не вдалося видалити дисципліну з ID {id}."
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
                    "Неможливо видалити дисципліну, оскільки " +
                    "вона вже використовується в навчальному " +
                    "навантаженні або розкладі."
            });
        }
    }

    private async Task<ActionResult?> ValidateAsync(
        Discipline discipline)
    {
        if (discipline.SpecialtyId <= 0)
        {
            return BadRequest(new
            {
                Message =
                    "Потрібно обрати спеціальність."
            });
        }

        if (string.IsNullOrWhiteSpace(discipline.Name))
        {
            return BadRequest(new
            {
                Message =
                    "Назва дисципліни не може бути порожньою."
            });
        }

        discipline.Name = discipline.Name.Trim();

        if (discipline.Name.Length > 150)
        {
            return BadRequest(new
            {
                Message =
                    "Назва дисципліни не може містити " +
                    "більше 150 символів."
            });
        }

        var specialty =
            await _specialtyRepository.GetByIdAsync(
                discipline.SpecialtyId);

        if (specialty == null)
        {
            return BadRequest(new
            {
                Message =
                    "Обрану спеціальність не знайдено."
            });
        }

        return null;
    }

    private static void ClearLegacyHours(
        Discipline discipline)
    {
        discipline.TotalHours = null;
        discipline.LectureHours = null;
        discipline.PracticalHours = null;
        discipline.LaboratoryHours = null;
        discipline.SeminarHours = null;
        discipline.OtherHours = null;
    }
}