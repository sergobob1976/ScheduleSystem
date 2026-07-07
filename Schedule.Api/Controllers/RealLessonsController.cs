using Microsoft.AspNetCore.Mvc;
using Schedule.Core.Interfaces;
using Schedule.Core.Models;

namespace Schedule.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RealLessonsController : ControllerBase
{
    private readonly IRealLessonRepository _lessonRepository;

    public RealLessonsController(IRealLessonRepository lessonRepository)
    {
        _lessonRepository = lessonRepository;
    }

    // 1. Отримати весь розклад
    [HttpGet]
    public async Task<ActionResult<IEnumerable<RealLesson>>> GetAll()
    {
        var lessons = await _lessonRepository.GetAllAsync();
        return Ok(lessons);
    }

    // 2. Отримати розклад для конкретної групи (мега-важливо для MAUI)
    [HttpGet("group/{groupId}")]
    public async Task<ActionResult<IEnumerable<RealLesson>>> GetByGroup(int groupId)
    {
        var lessons = await _lessonRepository.GetByGroupIdAsync(groupId);
        return Ok(lessons);
    }

    // 3. Отримати розклад для викладача (теж для MAUI)
    [HttpGet("teacher/{teacherId}")]
    public async Task<ActionResult<IEnumerable<RealLesson>>> GetByTeacher(int teacherId)
    {
        var lessons = await _lessonRepository.GetByTeacherIdAsync(teacherId);
        return Ok(lessons);
    }

    // 4. Створити пару (Диспетчер)
    [HttpPost]
    public async Task<ActionResult<RealLesson>> Create([FromBody] RealLesson lesson)
    {
        var newId = await _lessonRepository.CreateAsync(lesson);
        lesson.Id = newId;
        return Ok(lesson);
    }

    // 5. Оновити суто лінки дистанційного навчання (Викладач)
    // Запит буде йти на PATCH api/reallessons/5/links
    [HttpPatch("{id}/links")]
    public async Task<IActionResult> UpdateLinks(int id, [FromBody] UpdateLinksDto dto)
    {
        var updated = await _lessonRepository.UpdateLinksAsync(id, dto.ConferenceLink, dto.ResourceLink);
        if (!updated) return NotFound(new { Message = "Заняття з таким ID не знайдено." });
        return NoContent();
    }

    // 6. Отримати розклад для конкретної групи на ОДИН конкретний день (Сьогодні/Завтра)
    // Запит буде виглядати так: api/reallessons/group/1/date/2026-09-07
    [HttpGet("group/{groupId}/date/{date}")]
    public async Task<ActionResult<IEnumerable<RealLesson>>> GetByGroupAndDate(int groupId, DateTime date)
    {
        var allGroupLessons = await _lessonRepository.GetByGroupIdAsync(groupId);

        // Фільтруємо вже витягнуті дані за конкретною датою
        // Завдяки нашому складеному індексу (GroupId, LessonDate), база віддасть цей набір миттєво
        var dayLessons = allGroupLessons.Where(l => l.LessonDate.Date == date.Date);

        return Ok(dayLessons);
    }

    // 7. Отримати розклад для конкретного викладача на ОДИН конкретний день (Сьогодні/Завтра)
    // Запит буде виглядати так: api/reallessons/teacher/1/date/2026-09-07
    [HttpGet("teacher/{teacherId}/date/{date}")]
    public async Task<ActionResult<IEnumerable<RealLesson>>> GetByTeacherAndDate(int teacherId, DateTime date)
    {
        var allTeacherLessons = await _lessonRepository.GetByTeacherIdAsync(teacherId);
        var dayLessons = allTeacherLessons.Where(l => l.LessonDate.Date == date.Date);

        return Ok(dayLessons);
    }
}

// Маленький допоміжний клас прямо тут внизу для безпечного приймання лінків від викладача
public class UpdateLinksDto
{
    public string? ConferenceLink { get; set; }
    public string? ResourceLink { get; set; }
}
