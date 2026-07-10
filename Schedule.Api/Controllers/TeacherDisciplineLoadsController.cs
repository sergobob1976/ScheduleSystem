using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using Schedule.Core.Interfaces;
using Schedule.Core.Models;

namespace Schedule.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TeacherDisciplineLoadsController
    : ControllerBase
{
    private readonly
        ITeacherDisciplineLoadRepository _repository;

    private readonly
        ITeacherSemesterLoadRepository
            _teacherSemesterLoadRepository;

    private readonly
        IDisciplineRepository _disciplineRepository;

    public TeacherDisciplineLoadsController(
        ITeacherDisciplineLoadRepository repository,
        ITeacherSemesterLoadRepository
            teacherSemesterLoadRepository,
        IDisciplineRepository disciplineRepository)
    {
        _repository = repository;

        _teacherSemesterLoadRepository =
            teacherSemesterLoadRepository;

        _disciplineRepository =
            disciplineRepository;
    }

    [HttpGet]
    public async Task<
        ActionResult<IEnumerable<TeacherDisciplineLoad>>>
        GetAll(
            [FromQuery]
            int? teacherSemesterLoadId)
    {
        if (teacherSemesterLoadId.HasValue)
        {
            var filtered =
                await _repository
                    .GetByTeacherSemesterLoadIdAsync(
                        teacherSemesterLoadId.Value);

            return Ok(filtered);
        }

        var items = await _repository.GetAllAsync();

        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<
        ActionResult<TeacherDisciplineLoad>>
        GetById(int id)
    {
        var item =
            await _repository.GetByIdAsync(id);

        if (item == null)
        {
            return NotFound(new
            {
                Message =
                    $"Навантаження за дисципліною " +
                    $"з ID {id} не знайдено."
            });
        }

        return Ok(item);
    }

    [HttpPost]
    public async Task<
        ActionResult<TeacherDisciplineLoad>>
        Create(
            [FromBody]
            TeacherDisciplineLoad
                teacherDisciplineLoad)
    {
        var validationResult =
            await ValidateAsync(
                teacherDisciplineLoad);

        if (validationResult != null)
        {
            return validationResult;
        }

        var duplicate =
            await _repository
                .GetByLoadAndDisciplineAsync(
                    teacherDisciplineLoad
                        .TeacherSemesterLoadId,
                    teacherDisciplineLoad
                        .DisciplineId);

        if (duplicate != null)
        {
            return Conflict(new
            {
                Message =
                    "Для цього викладача в обраному " +
                    "семестрі навантаження за цією " +
                    "дисципліною вже створено."
            });
        }

        var limitValidation =
            await ValidateTotalHoursAsync(
                teacherDisciplineLoad);

        if (limitValidation != null)
        {
            return limitValidation;
        }

        try
        {
            int newId =
                await _repository.CreateAsync(
                    teacherDisciplineLoad);

            var created =
                await _repository.GetByIdAsync(newId);

            if (created == null)
            {
                return StatusCode(
                    StatusCodes
                        .Status500InternalServerError,
                    new
                    {
                        Message =
                            "Запис створено, але не вдалося " +
                            "отримати його з бази даних."
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
                    "Для цього викладача в обраному " +
                    "семестрі навантаження за цією " +
                    "дисципліною вже існує."
            });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody]
        TeacherDisciplineLoad
            teacherDisciplineLoad)
    {
        if (id != teacherDisciplineLoad.Id)
        {
            return BadRequest(new
            {
                Message =
                    "ID у URL не збігається " +
                    "з ID у тілі запиту."
            });
        }

        var current =
            await _repository.GetByIdAsync(id);

        if (current == null)
        {
            return NotFound(new
            {
                Message =
                    $"Навантаження за дисципліною " +
                    $"з ID {id} не знайдено."
            });
        }

        var validationResult =
            await ValidateAsync(
                teacherDisciplineLoad);

        if (validationResult != null)
        {
            return validationResult;
        }

        var duplicate =
            await _repository
                .GetByLoadAndDisciplineAsync(
                    teacherDisciplineLoad
                        .TeacherSemesterLoadId,
                    teacherDisciplineLoad
                        .DisciplineId);

        if (duplicate != null &&
            duplicate.Id != id)
        {
            return Conflict(new
            {
                Message =
                    "Для цього викладача в обраному " +
                    "семестрі навантаження за цією " +
                    "дисципліною вже існує."
            });
        }

        var limitValidation =
            await ValidateTotalHoursAsync(
                teacherDisciplineLoad,
                id);

        if (limitValidation != null)
        {
            return limitValidation;
        }

        try
        {
            bool updated =
                await _repository.UpdateAsync(
                    teacherDisciplineLoad);

            if (!updated)
            {
                return NotFound(new
                {
                    Message =
                        $"Не вдалося оновити запис " +
                        $"з ID {id}."
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
                    "Для цього викладача в обраному " +
                    "семестрі навантаження за цією " +
                    "дисципліною вже існує."
            });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var current =
            await _repository.GetByIdAsync(id);

        if (current == null)
        {
            return NotFound(new
            {
                Message =
                    $"Навантаження за дисципліною " +
                    $"з ID {id} не знайдено."
            });
        }

        bool deleted =
            await _repository.DeleteAsync(id);

        if (!deleted)
        {
            return NotFound(new
            {
                Message =
                    $"Не вдалося видалити запис " +
                    $"з ID {id}."
            });
        }

        return NoContent();
    }

    private async Task<ActionResult?>
        ValidateAsync(
            TeacherDisciplineLoad
                teacherDisciplineLoad)
    {
        if (teacherDisciplineLoad
                .TeacherSemesterLoadId <= 0)
        {
            return BadRequest(new
            {
                Message =
                    "Потрібно обрати загальне " +
                    "навантаження викладача на семестр."
            });
        }

        if (teacherDisciplineLoad
                .DisciplineId <= 0)
        {
            return BadRequest(new
            {
                Message =
                    "Потрібно обрати дисципліну."
            });
        }

        if (teacherDisciplineLoad
                .PlannedHours < 0)
        {
            return BadRequest(new
            {
                Message =
                    "Планова кількість годин " +
                    "не може бути від'ємною."
            });
        }

        var semesterLoad =
            await _teacherSemesterLoadRepository
                .GetByIdAsync(
                    teacherDisciplineLoad
                        .TeacherSemesterLoadId);

        if (semesterLoad == null)
        {
            return BadRequest(new
            {
                Message =
                    "Загальне навантаження " +
                    "викладача на семестр не знайдено."
            });
        }

        var discipline =
            await _disciplineRepository
                .GetByIdAsync(
                    teacherDisciplineLoad
                        .DisciplineId);

        if (discipline == null)
        {
            return BadRequest(new
            {
                Message =
                    "Обрану дисципліну не знайдено."
            });
        }

        return null;
    }

    private async Task<ActionResult?>
        ValidateTotalHoursAsync(
            TeacherDisciplineLoad
                teacherDisciplineLoad,
            int? excludedId = null)
    {
        var semesterLoad =
            await _teacherSemesterLoadRepository
                .GetByIdAsync(
                    teacherDisciplineLoad
                        .TeacherSemesterLoadId);

        if (semesterLoad == null)
        {
            return BadRequest(new
            {
                Message =
                    "Загальне навантаження " +
                    "викладача на семестр не знайдено."
            });
        }

        int alreadyPlanned =
            await _repository
                .GetTotalPlannedHoursAsync(
                    teacherDisciplineLoad
                        .TeacherSemesterLoadId,
                    excludedId);

        int totalAfterSaving =
            alreadyPlanned +
            teacherDisciplineLoad.PlannedHours;

        if (totalAfterSaving >
            semesterLoad.PlannedHours)
        {
            int exceededBy =
                totalAfterSaving -
                semesterLoad.PlannedHours;

            return BadRequest(new
            {
                Message =
                    "Сума навантаження за дисциплінами " +
                    "перевищує загальне навантаження " +
                    $"викладача на {exceededBy} год.",
                SemesterPlannedHours =
                    semesterLoad.PlannedHours,
                AlreadyPlannedHours =
                    alreadyPlanned,
                RequestedHours =
                    teacherDisciplineLoad
                        .PlannedHours,
                TotalAfterSaving =
                    totalAfterSaving,
                ExceededBy =
                    exceededBy
            });
        }

        return null;
    }
}
