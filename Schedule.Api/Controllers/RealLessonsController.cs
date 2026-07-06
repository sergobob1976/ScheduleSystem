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
}

// Маленький допоміжний клас прямо тут внизу для безпечного приймання лінків від викладача
public class UpdateLinksDto
{
    public string? ConferenceLink { get; set; }
    public string? ResourceLink { get; set; }
}
