using Microsoft.AspNetCore.Mvc;
using Schedule.Core.Interfaces;
using Schedule.Core.Models;

namespace Schedule.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BaseLessonsController : ControllerBase
{
    private readonly IBaseLessonRepository _baseLessonRepository;

    public BaseLessonsController(IBaseLessonRepository baseLessonRepository)
    {
        _baseLessonRepository = baseLessonRepository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BaseLesson>>> GetAll()
    {
        var lessons = await _baseLessonRepository.GetAllAsync();
        return Ok(lessons);
    }

    [HttpGet("group/{groupId}")]
    public async Task<ActionResult<IEnumerable<BaseLesson>>> GetByGroup(int groupId)
    {
        var lessons = await _baseLessonRepository.GetByGroupIdAsync(groupId);
        return Ok(lessons);
    }

    [HttpPost]
    public async Task<ActionResult<BaseLesson>> Create([FromBody] BaseLesson lesson)
    {
        var newId = await _baseLessonRepository.CreateAsync(lesson);
        lesson.Id = newId;
        return Ok(lesson);
    }
}
