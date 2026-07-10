using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using Schedule.Core.Interfaces;
using Schedule.Core.Models;

namespace Schedule.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GroupDisciplinesController : ControllerBase
{
    private readonly IGroupDisciplineRepository _repository;
    private readonly ISemesterRepository _semesterRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IDisciplineRepository _disciplineRepository;

    public GroupDisciplinesController(
        IGroupDisciplineRepository repository,
        ISemesterRepository semesterRepository,
        IGroupRepository groupRepository,
        IDisciplineRepository disciplineRepository)
    {
        _repository = repository;
        _semesterRepository = semesterRepository;
        _groupRepository = groupRepository;
        _disciplineRepository = disciplineRepository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<GroupDiscipline>>> GetAll(
        [FromQuery] int? semesterId,
        [FromQuery] int? groupId)
    {
        if (semesterId.HasValue && groupId.HasValue)
        {
            var filtered =
                await _repository.GetBySemesterAndGroupAsync(
                    semesterId.Value,
                    groupId.Value);

            return Ok(filtered);
        }

        if (semesterId.HasValue)
        {
            var filtered =
                await _repository.GetBySemesterIdAsync(
                    semesterId.Value);

            return Ok(filtered);
        }

        if (groupId.HasValue)
        {
            var filtered =
                await _repository.GetByGroupIdAsync(
                    groupId.Value);

            return Ok(filtered);
        }

        var items = await _repository.GetAllAsync();

        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<GroupDiscipline>> GetById(
        int id)
    {
        var item = await _repository.GetByIdAsync(id);

        if (item == null)
        {
            return NotFound(new
            {
                Message =
                    $"Дисципліну групи з ID {id} не знайдено."
            });
        }

        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<GroupDiscipline>> Create(
        [FromBody] GroupDiscipline groupDiscipline)
    {
        var validationResult =
            await ValidateAsync(groupDiscipline);

        if (validationResult != null)
        {
            return validationResult;
        }

        var duplicate =
            await _repository
                .GetBySemesterGroupAndDisciplineAsync(
                    groupDiscipline.SemesterId,
                    groupDiscipline.GroupId,
                    groupDiscipline.DisciplineId);

        if (duplicate != null)
        {
            return Conflict(new
            {
                Message =
                    "Ця дисципліна вже додана для обраної " +
                    "групи в обраному семестрі."
            });
        }

        try
        {
            int newId =
                await _repository.CreateAsync(
                    groupDiscipline);

            var created =
                await _repository.GetByIdAsync(newId);

            if (created == null)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
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
                    "Ця дисципліна вже додана для обраної " +
                    "групи в обраному семестрі."
            });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] GroupDiscipline groupDiscipline)
    {
        if (id != groupDiscipline.Id)
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
                    $"Дисципліну групи з ID {id} не знайдено."
            });
        }

        var validationResult =
            await ValidateAsync(groupDiscipline);

        if (validationResult != null)
        {
            return validationResult;
        }

        var duplicate =
            await _repository
                .GetBySemesterGroupAndDisciplineAsync(
                    groupDiscipline.SemesterId,
                    groupDiscipline.GroupId,
                    groupDiscipline.DisciplineId);

        if (duplicate != null && duplicate.Id != id)
        {
            return Conflict(new
            {
                Message =
                    "Ця дисципліна вже додана для обраної " +
                    "групи в обраному семестрі."
            });
        }

        try
        {
            bool updated =
                await _repository.UpdateAsync(groupDiscipline);

            if (!updated)
            {
                return NotFound(new
                {
                    Message =
                        $"Не вдалося оновити запис з ID {id}."
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
                    "Ця дисципліна вже додана для обраної " +
                    "групи в обраному семестрі."
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
                    $"Дисципліну групи з ID {id} не знайдено."
            });
        }

        bool deleted = await _repository.DeleteAsync(id);

        if (!deleted)
        {
            return NotFound(new
            {
                Message =
                    $"Не вдалося видалити запис з ID {id}."
            });
        }

        return NoContent();
    }

    private async Task<ActionResult?> ValidateAsync(
        GroupDiscipline groupDiscipline)
    {
        if (groupDiscipline.SemesterId <= 0)
        {
            return BadRequest(new
            {
                Message = "Потрібно обрати семестр."
            });
        }

        if (groupDiscipline.GroupId <= 0)
        {
            return BadRequest(new
            {
                Message = "Потрібно обрати групу."
            });
        }

        if (groupDiscipline.DisciplineId <= 0)
        {
            return BadRequest(new
            {
                Message = "Потрібно обрати дисципліну."
            });
        }

        if (groupDiscipline.LectureHours < 0 ||
            groupDiscipline.PracticalHours < 0 ||
            groupDiscipline.LaboratoryHours < 0 ||
            groupDiscipline.SeminarHours < 0 ||
            groupDiscipline.OtherHours < 0)
        {
            return BadRequest(new
            {
                Message =
                    "Кількість годин не може бути від'ємною."
            });
        }

        if (groupDiscipline.TotalHours <= 0)
        {
            return BadRequest(new
            {
                Message =
                    "Потрібно вказати хоча б одну навчальну годину."
            });
        }

        var semester =
            await _semesterRepository.GetByIdAsync(
                groupDiscipline.SemesterId);

        if (semester == null)
        {
            return BadRequest(new
            {
                Message = "Обраний семестр не знайдено."
            });
        }

        var group =
            await _groupRepository.GetByIdAsync(
                groupDiscipline.GroupId);

        if (group == null)
        {
            return BadRequest(new
            {
                Message = "Обрану групу не знайдено."
            });
        }

        var discipline =
            await _disciplineRepository.GetByIdAsync(
                groupDiscipline.DisciplineId);

        if (discipline == null)
        {
            return BadRequest(new
            {
                Message = "Обрану дисципліну не знайдено."
            });
        }

        return null;
    }
}
