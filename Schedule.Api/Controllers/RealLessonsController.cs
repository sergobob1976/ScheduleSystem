using Microsoft.AspNetCore.Mvc;
using Schedule.Core.Enums;
using Schedule.Core.Interfaces;
using Schedule.Core.Models;

namespace Schedule.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RealLessonsController : ControllerBase
{
    private readonly IRealLessonRepository _lessonRepository;
    private readonly ITeachingAssignmentRepository
        _teachingAssignmentRepository;
    private readonly IGroupDisciplineRepository
        _groupDisciplineRepository;
    private readonly IClassRoomRepository
        _classRoomRepository;
    private readonly ISemesterRepository
        _semesterRepository;

    public RealLessonsController(
        IRealLessonRepository lessonRepository,
        ITeachingAssignmentRepository teachingAssignmentRepository,
        IGroupDisciplineRepository groupDisciplineRepository,
        IClassRoomRepository classRoomRepository,
        ISemesterRepository semesterRepository)
    {
        _lessonRepository = lessonRepository;
        _teachingAssignmentRepository =
            teachingAssignmentRepository;
        _groupDisciplineRepository =
            groupDisciplineRepository;
        _classRoomRepository = classRoomRepository;
        _semesterRepository = semesterRepository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RealLesson>>> GetAll()
    {
        var lessons =
            await _lessonRepository.GetAllAsync();

        return Ok(lessons);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RealLesson>> GetById(int id)
    {
        var lesson =
            await _lessonRepository.GetByIdAsync(id);

        if (lesson == null)
        {
            return NotFound(new
            {
                Message =
                    $"Заняття з ID {id} не знайдено."
            });
        }

        return Ok(lesson);
    }

    [HttpGet("group/{groupId:int}")]
    public async Task<ActionResult<IEnumerable<RealLesson>>>
        GetByGroup(int groupId)
    {
        var lessons =
            await _lessonRepository
                .GetByGroupIdAsync(groupId);

        return Ok(lessons);
    }

    [HttpGet("teacher/{teacherId:int}")]
    public async Task<ActionResult<IEnumerable<RealLesson>>>
        GetByTeacher(int teacherId)
    {
        var lessons =
            await _lessonRepository
                .GetByTeacherIdAsync(teacherId);

        return Ok(lessons);
    }

    [HttpGet("group/{groupId:int}/date/{date:datetime}")]
    public async Task<ActionResult<IEnumerable<RealLesson>>>
        GetByGroupAndDate(
            int groupId,
            DateTime date)
    {
        var allGroupLessons =
            await _lessonRepository
                .GetByGroupIdAsync(groupId);

        var dayLessons =
            allGroupLessons.Where(
                lesson =>
                    lesson.LessonDate.Date ==
                    date.Date);

        return Ok(dayLessons);
    }

    [HttpGet("teacher/{teacherId:int}/date/{date:datetime}")]
    public async Task<ActionResult<IEnumerable<RealLesson>>>
        GetByTeacherAndDate(
            int teacherId,
            DateTime date)
    {
        var allTeacherLessons =
            await _lessonRepository
                .GetByTeacherIdAsync(teacherId);

        var dayLessons =
            allTeacherLessons.Where(
                lesson =>
                    lesson.LessonDate.Date ==
                    date.Date);

        return Ok(dayLessons);
    }

    [HttpPost]
    public async Task<ActionResult<RealLesson>> Create(
        [FromBody] RealLesson lesson)
    {
        var validationResult =
            await PrepareAndValidateAsync(lesson);

        if (validationResult != null)
        {
            return validationResult;
        }

        int newId =
            await _lessonRepository.CreateAsync(lesson);

        var created =
            await _lessonRepository.GetByIdAsync(newId);

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
        [FromBody] RealLesson lesson)
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
            await _lessonRepository.GetByIdAsync(id);

        if (current == null)
        {
            return NotFound(new
            {
                Message =
                    $"Заняття з ID {id} не знайдено."
            });
        }

        var validationResult =
            await PrepareAndValidateAsync(lesson);

        if (validationResult != null)
        {
            return validationResult;
        }

        bool updated =
            await _lessonRepository.UpdateAsync(lesson);

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

    [HttpPatch("{id:int}/links")]
    public async Task<IActionResult> UpdateLinks(
        int id,
        [FromBody] UpdateLinksDto dto)
    {
        var current =
            await _lessonRepository.GetByIdAsync(id);

        if (current == null)
        {
            return NotFound(new
            {
                Message =
                    "Заняття з таким ID не знайдено."
            });
        }

        bool updated =
            await _lessonRepository.UpdateLinksAsync(
                id,
                NormalizeOptionalText(dto.ConferenceLink),
                NormalizeOptionalText(dto.ResourceLink));

        if (!updated)
        {
            return NotFound(new
            {
                Message =
                    "Заняття з таким ID не знайдено."
            });
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var current =
            await _lessonRepository.GetByIdAsync(id);

        if (current == null)
        {
            return NotFound(new
            {
                Message =
                    $"Заняття з ID {id} не знайдено."
            });
        }

        bool deleted =
            await _lessonRepository.DeleteAsync(id);

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

    private async Task<ActionResult?> PrepareAndValidateAsync(
        RealLesson lesson)
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
            await _teachingAssignmentRepository.GetByIdAsync(
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
            ?? await _groupDisciplineRepository.GetByIdAsync(
                assignment.GroupDisciplineId);

        if (groupDiscipline == null)
        {
            return BadRequest(new
            {
                Message =
                    "Навчальний план групи для призначення не знайдено."
            });
        }

        var semester =
            groupDiscipline.Semester
            ?? await _semesterRepository.GetByIdAsync(
                groupDiscipline.SemesterId);

        if (semester == null)
        {
            return BadRequest(new
            {
                Message =
                    "Семестр для цього призначення не знайдено."
            });
        }

        if (lesson.LessonDate == default)
        {
            return BadRequest(new
            {
                Message =
                    "Потрібно вказати дату заняття."
            });
        }

        if (lesson.LessonDate.Date <
                semester.StartDate.Date ||
            lesson.LessonDate.Date >
                semester.EndDate.Date)
        {
            return BadRequest(new
            {
                Message =
                    "Дата заняття повинна бути в межах " +
                    $"семестру: {semester.StartDate:dd.MM.yyyy}–" +
                    $"{semester.EndDate:dd.MM.yyyy}."
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
                    await _classRoomRepository.GetByIdAsync(
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

        WeekDay actualWeekDay =
            ConvertToWeekDay(lesson.LessonDate);

        if (!Enum.IsDefined(
                typeof(WeekDay),
                actualWeekDay))
        {
            return BadRequest(new
            {
                Message =
                    "Дата заняття припадає на день, " +
                    "який не підтримується розкладом."
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

        lesson.WeekDay =
            actualWeekDay;

        lesson.ConferenceLink =
            NormalizeOptionalText(
                lesson.ConferenceLink);

        lesson.ResourceLink =
            NormalizeOptionalText(
                lesson.ResourceLink);

        lesson.TeachingAssignment = null;
        lesson.Group = null;
        lesson.Teacher = null;
        lesson.Discipline = null;
        lesson.ClassRoom = null;
        lesson.Semester = null;

        return null;
    }

    private static WeekDay ConvertToWeekDay(
        DateTime date)
    {
        return date.DayOfWeek switch
        {
            DayOfWeek.Monday =>
                (WeekDay)1,

            DayOfWeek.Tuesday =>
                (WeekDay)2,

            DayOfWeek.Wednesday =>
                (WeekDay)3,

            DayOfWeek.Thursday =>
                (WeekDay)4,

            DayOfWeek.Friday =>
                (WeekDay)5,

            DayOfWeek.Saturday =>
                (WeekDay)6,

            DayOfWeek.Sunday =>
                (WeekDay)7,

            _ => default
        };
    }

    private static string? NormalizeOptionalText(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}

public class UpdateLinksDto
{
    public string? ConferenceLink { get; set; }

    public string? ResourceLink { get; set; }
}