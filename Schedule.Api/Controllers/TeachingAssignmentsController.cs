using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using Schedule.Core.Enums;
using Schedule.Core.Interfaces;
using Schedule.Core.Models;

namespace Schedule.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TeachingAssignmentsController
    : ControllerBase
{
    private readonly
        ITeachingAssignmentRepository _repository;

    private readonly
        IGroupDisciplineRepository
            _groupDisciplineRepository;

    private readonly
        ITeacherRepository _teacherRepository;

    private readonly
        ITeacherSemesterLoadRepository
            _teacherSemesterLoadRepository;

    private readonly
        ITeacherDisciplineLoadRepository
            _teacherDisciplineLoadRepository;

    public TeachingAssignmentsController(
        ITeachingAssignmentRepository repository,
        IGroupDisciplineRepository
            groupDisciplineRepository,
        ITeacherRepository teacherRepository,
        ITeacherSemesterLoadRepository
            teacherSemesterLoadRepository,
        ITeacherDisciplineLoadRepository
            teacherDisciplineLoadRepository)
    {
        _repository = repository;

        _groupDisciplineRepository =
            groupDisciplineRepository;

        _teacherRepository =
            teacherRepository;

        _teacherSemesterLoadRepository =
            teacherSemesterLoadRepository;

        _teacherDisciplineLoadRepository =
            teacherDisciplineLoadRepository;
    }

    [HttpGet]
    public async Task<
        ActionResult<IEnumerable<TeachingAssignment>>>
        GetAll(
            [FromQuery] int? groupDisciplineId,
            [FromQuery] int? teacherId)
    {
        if (groupDisciplineId.HasValue)
        {
            var filtered =
                await _repository
                    .GetByGroupDisciplineIdAsync(
                        groupDisciplineId.Value);

            return Ok(filtered);
        }

        if (teacherId.HasValue)
        {
            var filtered =
                await _repository
                    .GetByTeacherIdAsync(
                        teacherId.Value);

            return Ok(filtered);
        }

        var items =
            await _repository.GetAllAsync();

        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<
        ActionResult<TeachingAssignment>>
        GetById(int id)
    {
        var item =
            await _repository.GetByIdAsync(id);

        if (item == null)
        {
            return NotFound(new
            {
                Message =
                    $"Призначення викладача " +
                    $"з ID {id} не знайдено."
            });
        }

        return Ok(item);
    }

    [HttpPost]
    public async Task<
        ActionResult<TeachingAssignment>>
        Create(
            [FromBody]
            TeachingAssignment assignment)
    {
        var validation =
            await ValidateAsync(assignment);

        if (validation != null)
        {
            return validation;
        }

        var duplicate =
            await _repository.GetExistingAsync(
                assignment.GroupDisciplineId,
                assignment.TeacherId,
                assignment.LessonType);

        if (duplicate != null)
        {
            return Conflict(new
            {
                Message =
                    "Цей викладач уже призначений " +
                    "на обраний вид занять для цієї " +
                    "групи та дисципліни."
            });
        }

        var hoursValidation =
            await ValidateHoursAsync(assignment);

        if (hoursValidation != null)
        {
            return hoursValidation;
        }

        try
        {
            int newId =
                await _repository.CreateAsync(
                    assignment);

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
                            "Призначення створено, але " +
                            "не вдалося отримати його " +
                            "з бази даних."
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
                    "Таке призначення викладача " +
                    "вже існує."
            });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody]
        TeachingAssignment assignment)
    {
        if (id != assignment.Id)
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
                    $"Призначення викладача " +
                    $"з ID {id} не знайдено."
            });
        }

        var validation =
            await ValidateAsync(assignment);

        if (validation != null)
        {
            return validation;
        }

        var duplicate =
            await _repository.GetExistingAsync(
                assignment.GroupDisciplineId,
                assignment.TeacherId,
                assignment.LessonType);

        if (duplicate != null &&
            duplicate.Id != id)
        {
            return Conflict(new
            {
                Message =
                    "Цей викладач уже призначений " +
                    "на обраний вид занять для цієї " +
                    "групи та дисципліни."
            });
        }

        var hoursValidation =
            await ValidateHoursAsync(
                assignment,
                id);

        if (hoursValidation != null)
        {
            return hoursValidation;
        }

        try
        {
            bool updated =
                await _repository.UpdateAsync(
                    assignment);

            if (!updated)
            {
                return NotFound(new
                {
                    Message =
                        $"Не вдалося оновити " +
                        $"призначення з ID {id}."
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
                    "Таке призначення викладача " +
                    "вже існує."
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
                    $"Призначення викладача " +
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
                    $"Не вдалося видалити " +
                    $"призначення з ID {id}."
            });
        }

        return NoContent();
    }

    private async Task<ActionResult?>
        ValidateAsync(
            TeachingAssignment assignment)
    {
        if (assignment.GroupDisciplineId <= 0)
        {
            return BadRequest(new
            {
                Message =
                    "Потрібно обрати дисципліну групи."
            });
        }

        if (assignment.TeacherId <= 0)
        {
            return BadRequest(new
            {
                Message =
                    "Потрібно обрати викладача."
            });
        }

        if (!Enum.IsDefined(
                typeof(LessonType),
                assignment.LessonType))
        {
            return BadRequest(new
            {
                Message =
                    "Вказано невідомий вид заняття."
            });
        }

        if (assignment.AssignedHours <= 0)
        {
            return BadRequest(new
            {
                Message =
                    "Кількість призначених годин " +
                    "має бути більшою за нуль."
            });
        }

        var groupDiscipline =
            await _groupDisciplineRepository
                .GetByIdAsync(
                    assignment.GroupDisciplineId);

        if (groupDiscipline == null)
        {
            return BadRequest(new
            {
                Message =
                    "Дисципліну групи не знайдено."
            });
        }

        var teacher =
            await _teacherRepository.GetByIdAsync(
                assignment.TeacherId);

        if (teacher == null)
        {
            return BadRequest(new
            {
                Message =
                    "Обраного викладача не знайдено."
            });
        }

        return null;
    }

    private async Task<ActionResult?>
        ValidateHoursAsync(
            TeachingAssignment assignment,
            int? excludedId = null)
    {
        var groupDiscipline =
            await _groupDisciplineRepository
                .GetByIdAsync(
                    assignment.GroupDisciplineId);

        if (groupDiscipline == null)
        {
            return BadRequest(new
            {
                Message =
                    "Дисципліну групи не знайдено."
            });
        }

        int plannedGroupHours =
            GetPlannedHoursByLessonType(
                groupDiscipline,
                assignment.LessonType);

        if (plannedGroupHours <= 0)
        {
            return BadRequest(new
            {
                Message =
                    "Для цього виду занять у групи " +
                    "не передбачено навчальних годин."
            });
        }

        int alreadyAssignedForGroup =
            await _repository
                .GetAssignedHoursForGroupDisciplineTypeAsync(
                    assignment.GroupDisciplineId,
                    assignment.LessonType,
                    excludedId);

        int groupTotalAfterSaving =
            alreadyAssignedForGroup +
            assignment.AssignedHours;

        if (groupTotalAfterSaving >
            plannedGroupHours)
        {
            int exceededBy =
                groupTotalAfterSaving -
                plannedGroupHours;

            return BadRequest(new
            {
                Message =
                    "Призначені години перевищують " +
                    "план групи для цього виду занять " +
                    $"на {exceededBy} год.",
                PlannedGroupHours =
                    plannedGroupHours,
                AlreadyAssignedForGroup =
                    alreadyAssignedForGroup,
                RequestedHours =
                    assignment.AssignedHours,
                TotalAfterSaving =
                    groupTotalAfterSaving,
                ExceededBy =
                    exceededBy
            });
        }

        var teacherSemesterLoad =
            await _teacherSemesterLoadRepository
                .GetByTeacherAndSemesterAsync(
                    assignment.TeacherId,
                    groupDiscipline.SemesterId);

        if (teacherSemesterLoad == null)
        {
            return BadRequest(new
            {
                Message =
                    "Для цього викладача не задано " +
                    "загальне навантаження в обраному " +
                    "семестрі."
            });
        }

        var teacherDisciplineLoad =
            await _teacherDisciplineLoadRepository
                .GetByLoadAndDisciplineAsync(
                    teacherSemesterLoad.Id,
                    groupDiscipline.DisciplineId);

        if (teacherDisciplineLoad == null)
        {
            return BadRequest(new
            {
                Message =
                    "Для цього викладача не задано " +
                    "планове навантаження з обраної " +
                    "дисципліни в цьому семестрі."
            });
        }

        int alreadyAssignedToTeacher =
            await _repository
                .GetAssignedHoursForTeacherDisciplineAsync(
                    assignment.TeacherId,
                    groupDiscipline.SemesterId,
                    groupDiscipline.DisciplineId,
                    excludedId);

        int teacherTotalAfterSaving =
            alreadyAssignedToTeacher +
            assignment.AssignedHours;

        if (teacherTotalAfterSaving >
            teacherDisciplineLoad.PlannedHours)
        {
            int exceededBy =
                teacherTotalAfterSaving -
                teacherDisciplineLoad.PlannedHours;

            return BadRequest(new
            {
                Message =
                    "Призначені години перевищують " +
                    "план викладача з цієї дисципліни " +
                    $"на {exceededBy} год.",
                TeacherDisciplinePlannedHours =
                    teacherDisciplineLoad.PlannedHours,
                AlreadyAssignedToTeacher =
                    alreadyAssignedToTeacher,
                RequestedHours =
                    assignment.AssignedHours,
                TotalAfterSaving =
                    teacherTotalAfterSaving,
                ExceededBy =
                    exceededBy
            });
        }

        return null;
    }

    private static int GetPlannedHoursByLessonType(
        GroupDiscipline groupDiscipline,
        LessonType lessonType)
    {
        return lessonType switch
        {
            LessonType.Lecture =>
                groupDiscipline.LectureHours,

            LessonType.Practical =>
                groupDiscipline.PracticalHours,

            LessonType.Laboratory =>
                groupDiscipline.LaboratoryHours,

            LessonType.Seminar =>
                groupDiscipline.SeminarHours,

            LessonType.Other =>
                groupDiscipline.OtherHours,

            _ => 0
        };
    }
}
