using Microsoft.AspNetCore.Mvc;
using Schedule.Core.Interfaces;
using Schedule.Core.Models;

namespace Schedule.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DisciplinesController : ControllerBase
{
    private readonly IDisciplineRepository _disciplineRepository;

    public DisciplinesController(IDisciplineRepository disciplineRepository)
    {
        _disciplineRepository = disciplineRepository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Discipline>>> GetAll()
    {
        var disciplines = await _disciplineRepository.GetAllAsync();
        return Ok(disciplines);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Discipline>> GetById(int id)
    {
        var discipline = await _disciplineRepository.GetByIdAsync(id);
        if (discipline == null) return NotFound(new { Message = "Предмет не знайдено." });
        return Ok(discipline);
    }

    [HttpPost]
    public async Task<ActionResult<Discipline>> Create([FromBody] Discipline discipline)
    {
        if (string.IsNullOrWhiteSpace(discipline.Name))
            return BadRequest(new { Message = "Назва предмета не може бути порожньою." });

        var newId = await _disciplineRepository.CreateAsync(discipline);
        discipline.Id = newId;

        return CreatedAtAction(nameof(GetById), new { id = discipline.Id }, discipline);
    }
}