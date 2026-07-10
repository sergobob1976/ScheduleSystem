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

        if (discipline == null)
        {
            return NotFound(new { Message = $"Дисципліну з ID {id} не знайдено." });
        }

        return Ok(discipline);
    }

    [HttpPost]
    public async Task<ActionResult<Discipline>> Create([FromBody] Discipline discipline)
    {
        if (discipline.SpecialtyId <= 0)
        {
            return BadRequest(new { Message = "Потрібно обрати спеціальність." });
        }

        if (string.IsNullOrWhiteSpace(discipline.Name))
        {
            return BadRequest(new { Message = "Назва дисципліни не може бути порожньою." });
        }

        var newId = await _disciplineRepository.CreateAsync(discipline);
        discipline.Id = newId;

        return CreatedAtAction(nameof(GetById), new { id = discipline.Id }, discipline);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] Discipline discipline)
    {
        if (id != discipline.Id)
        {
            return BadRequest(new { Message = "ID в URL не збігається з ID в тілі запиту." });
        }

        if (discipline.SpecialtyId <= 0)
        {
            return BadRequest(new { Message = "Потрібно обрати спеціальність." });
        }

        if (string.IsNullOrWhiteSpace(discipline.Name))
        {
            return BadRequest(new { Message = "Назва дисципліни не може бути порожньою." });
        }

        var updated = await _disciplineRepository.UpdateAsync(discipline);

        if (!updated)
        {
            return NotFound(new { Message = $"Не вдалося оновити. Дисципліну з ID {id} не знайдено." });
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _disciplineRepository.DeleteAsync(id);

        if (!deleted)
        {
            return NotFound(new { Message = $"Не вдалося видалити. Дисципліну з ID {id} не знайдено." });
        }

        return NoContent();
    }
}