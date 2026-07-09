using Microsoft.AspNetCore.Mvc;
using Schedule.Core.Interfaces;
using Schedule.Core.Models;

namespace Schedule.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SpecialtiesController : ControllerBase
{
    private readonly ISpecialtyRepository _specialtyRepository;

    public SpecialtiesController(ISpecialtyRepository specialtyRepository)
    {
        _specialtyRepository = specialtyRepository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Specialty>>> GetAll()
    {
        var specialties = await _specialtyRepository.GetAllAsync();
        return Ok(specialties);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Specialty>> GetById(int id)
    {
        var specialty = await _specialtyRepository.GetByIdAsync(id);

        if (specialty == null)
        {
            return NotFound(new { Message = $"Спеціальність з ID {id} не знайдено." });
        }

        return Ok(specialty);
    }

    [HttpPost]
    public async Task<ActionResult<Specialty>> Create([FromBody] Specialty specialty)
    {
        if (string.IsNullOrWhiteSpace(specialty.Code))
        {
            return BadRequest(new { Message = "Код спеціальності не може бути порожнім." });
        }

        if (string.IsNullOrWhiteSpace(specialty.Name))
        {
            return BadRequest(new { Message = "Назва спеціальності не може бути порожньою." });
        }

        var newId = await _specialtyRepository.CreateAsync(specialty);
        specialty.Id = newId;

        return CreatedAtAction(nameof(GetById), new { id = specialty.Id }, specialty);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] Specialty specialty)
    {
        if (id != specialty.Id)
        {
            return BadRequest(new { Message = "ID в URL не збігається з ID в тілі запиту." });
        }

        var updated = await _specialtyRepository.UpdateAsync(specialty);

        if (!updated)
        {
            return NotFound(new { Message = $"Не вдалося оновити. Спеціальність з ID {id} не знайдено." });
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _specialtyRepository.DeleteAsync(id);

        if (!deleted)
        {
            return NotFound(new { Message = $"Не вдалося видалити. Спеціальність з ID {id} не знайдено." });
        }

        return NoContent();
    }
}
