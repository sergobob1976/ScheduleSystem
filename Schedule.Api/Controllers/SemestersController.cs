using Microsoft.AspNetCore.Mvc;
using Schedule.Core.Interfaces;
using Schedule.Core.Models;

namespace Schedule.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SemestersController : ControllerBase
{
    private readonly ISemesterRepository _semesterRepository;

    public SemestersController(ISemesterRepository semesterRepository)
    {
        _semesterRepository = semesterRepository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Semester>>> GetAll()
    {
        var semesters = await _semesterRepository.GetAllAsync();
        return Ok(semesters);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Semester>> GetById(int id)
    {
        var semester = await _semesterRepository.GetByIdAsync(id);
        if (semester == null) return NotFound(new { Message = "Семестр не знайдено." });
        return Ok(semester);
    }

    [HttpPost]
    public async Task<ActionResult<Semester>> Create([FromBody] Semester semester)
    {
        if (string.IsNullOrWhiteSpace(semester.Name))
            return BadRequest(new { Message = "Назва семестру не може бути порожньою." });

        var newId = await _semesterRepository.CreateAsync(semester);
        semester.Id = newId;

        return CreatedAtAction(nameof(GetById), new { id = semester.Id }, semester);
    }
}
