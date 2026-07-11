using Microsoft.AspNetCore.Mvc;
using Schedule.Core.Enums;
using Schedule.Core.Interfaces;
using Schedule.Core.Models;

namespace Schedule.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BaseLessonsController : ControllerBase
{
    private readonly IBaseLessonRepository
        _baseLessonRepository;

    private readonly ITeachingAssignmentRepository
        _teachingAssignmentRepository;

    private readonly IGroupDisciplineRepository
        _groupDisciplineRepository;

    private readonly IClassRoomRepository
        _classRoomRepository;

    public BaseLessonsController(
        IBaseLessonRepository baseLessonRepository,
        ITeachingAssignmentRepository
            teachingAssignmentRepository,
        IGroupDisciplineRepository
            groupDisciplineRepository,
        IClassRoomRepository classRoomRepository)
    {
        _baseLessonRepository =
            baseLessonRepository;

        _teachingAssignmentRepository =
            teachingAssignmentRepository;

        _groupDisciplineRepository =
            groupDisciplineRepository;

        _classRoomRepository =
            classRoomRepository;
    }

    [HttpGet]
    public async Task<
        ActionResult<IEnumerable<BaseLesson>>>
        GetAll()
    {
        var lessons =
            await _baseLessonRepository.GetAllAsync();

        return Ok(lessons);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<BaseLesson>>
        GetById(int id)
    {
        var lesson =
            await _baseLessonRepository.GetByIdAsync(id);

        if (lesson == null)
        {
            return NotFound(new
            {
                Message =
                    $"Базове заняття з ID {id} не знайдено."
            });
        }

        return Ok(lesson);
    }

    [HttpGet("group/{groupId:int}")]
    public async Task<
        ActionResult<IEnumerable<BaseLesson>>>
        GetByGroup(int groupId)
    {
        var lessons =
            await _baseLessonRepository
                .GetByGroupIdAsync(groupId);

        return Ok(lessons);
    }

    [HttpPost]
    public async Task<ActionResult<BaseLesson>> Create(
        [FromBody] BaseLesson lesson)
    {
        var validationResult =
            await PrepareAndValidateAsync(lesson);

        if (validationResult != null)
        {
            return validationResult;
        }

        var conflictResult =
            await ValidateConflictsAsync(lesson);

        if (conflictResult != null)
        {
            return conflictResult;
        }

        int newId =
            await _baseLessonRepository.CreateAsync(
                lesson);

        var created =
            await _baseLessonRepository.GetByIdAsync(
                newId);

        if (created == null)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    Message =
                        "Заняття створено, але не вдалося " +
                        "отримати його з бази даних."
                });
        }

        return CreatedAtAction(
            nameof(GetById),
            new { id = created.Id },
            created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] BaseLesson lesson)
    {
        if (id != lesson.Id)
        {
            return BadRequest(new
            {
                Message =
                    "ID у URL не збігається з ID у тілі запиту."
            });
        }

        var current =
            await _baseLessonRepository.GetByIdAsync(id);

        if (current == null)
        {
            return NotFound(new
            {
                Message =
                    $"Базове заняття з ID {id} не знайдено."
            });
        }

        var validationResult =
            await PrepareAndValidateAsync(lesson);

        if (validationResult != null)
        {
            return validationResult;
        }

        var conflictResult =
            await ValidateConflictsAsync(
                lesson,
                id);

        if (conflictResult != null)
        {
            return conflictResult;
        }

        bool updated =
            await _baseLessonRepository.UpdateAsync(
                lesson);

        if (!updated)
        {
            return NotFound(new
            {
                Message =
                    $"Не вдалося оновити заняття з ID {id}."
            });
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var current =
            await _baseLessonRepository.GetByIdAsync(id);

        if (current == null)
        {
            return NotFound(new
            {
                Message =
                    $"Базове заняття з ID {id} не знайдено."
            });
        }

        bool deleted =
            await _baseLessonRepository.DeleteAsync(id);

        if (!deleted)
        {
            return NotFound(new
            {
                Message =
                    $"Не вдалося видалити заняття з ID {id}."
            });
        }

        return NoContent();
    }

    private async Task<ActionResult?>
        PrepareAndValidateAsync(
            BaseLesson lesson)
    {
        if (!lesson.TeachingAssignmentId.HasValue ||
            lesson.TeachingAssignmentId.Value <= 0)
        {
            return BadRequest(new
            {
                Message =
                    "Потрібно обрати призначення викладача."
            });
        }

        var assignment =
            await _teachingAssignmentRepository
                .GetByIdAsync(
                    lesson.TeachingAssignmentId.Value);

        if (assignment == null)
        {
            return BadRequest(new
            {
                Message =
                    "Обране призначення викладача не знайдено."
            });
        }

        var groupDiscipline =
            assignment.GroupDiscipline
            ?? await _groupDisciplineRepository
                .GetByIdAsync(
                    assignment.GroupDisciplineId);

        if (groupDiscipline == null)
        {
            return BadRequest(new
            {
                Message =
                    "Навчальний план групи для " +
                    "призначення не знайдено."
            });
        }

        if (lesson.ClassRoomId.HasValue)
        {
            if (lesson.ClassRoomId.Value <= 0)
            {
                lesson.ClassRoomId = null;
            }
            else
            {
                var classRoom =
                    await _classRoomRepository
                        .GetByIdAsync(
                            lesson.ClassRoomId.Value);

                if (classRoom == null)
                {
                    return BadRequest(new
                    {
                        Message =
                            "Обрану аудиторію не знайдено."
                    });
                }
            }
        }

        if (lesson.LessonPosition is < 1 or > 8)
        {
            return BadRequest(new
            {
                Message =
                    "Номер пари має бути від 1 до 8."
            });
        }

        if (!Enum.IsDefined(
                typeof(WeekDay),
                lesson.WeekDay))
        {
            return BadRequest(new
            {
                Message =
                    "Вказано невідомий день тижня."
            });
        }

        if (!Enum.IsDefined(
                typeof(WeekProperty),
                lesson.WeekProperty))
        {
            return BadRequest(new
            {
                Message =
                    "Вказано невідому властивість тижня."
            });
        }

        lesson.GroupId =
            groupDiscipline.GroupId;

        lesson.TeacherId =
            assignment.TeacherId;

        lesson.DisciplineId =
            groupDiscipline.DisciplineId;

        lesson.SemesterId =
            groupDiscipline.SemesterId;

        lesson.LessonType =
            assignment.LessonType;

        lesson.TeachingAssignment = null;
        lesson.Group = null;
        lesson.Teacher = null;
        lesson.Discipline = null;
        lesson.ClassRoom = null;
        lesson.Semester = null;

        return null;
    }

    private async Task<ActionResult?>
        ValidateConflictsAsync(
            BaseLesson lesson,
            int? excludedId = null)
    {
        var conflicts =
            (
                await _baseLessonRepository
                    .GetConflictingLessonsAsync(
                        lesson,
                        excludedId)
            ).ToList();

        var groupConflict =
            conflicts.FirstOrDefault(
                existing =>
                    existing.GroupId ==
                    lesson.GroupId);

        if (groupConflict != null)
        {
            return Conflict(new
            {
                Message =
                    "Обрана група вже має заняття " +
                    "в цей день і на цій парі.",
                ConflictType = "Group",
                ConflictingLessonId =
                    groupConflict.Id
            });
        }

        var teacherConflict =
            conflicts.FirstOrDefault(
                existing =>
                    existing.TeacherId ==
                    lesson.TeacherId);

        if (teacherConflict != null)
        {
            return Conflict(new
            {
                Message =
                    "Обраний викладач уже проводить " +
                    "інше заняття в цей день і " +
                    "на цій парі.",
                ConflictType = "Teacher",
                ConflictingLessonId =
                    teacherConflict.Id
            });
        }

        if (lesson.ClassRoomId.HasValue)
        {
            var classRoomConflict =
                conflicts.FirstOrDefault(
                    existing =>
                        existing.ClassRoomId ==
                        lesson.ClassRoomId);

            if (classRoomConflict != null)
            {
                return Conflict(new
                {
                    Message =
                        "Обрана аудиторія вже зайнята " +
                        "в цей день і на цій парі.",
                    ConflictType = "ClassRoom",
                    ConflictingLessonId =
                        classRoomConflict.Id
                });
            }
        }

        return null;
    }
}