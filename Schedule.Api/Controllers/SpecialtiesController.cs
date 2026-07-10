using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using Schedule.Core.Interfaces;
using Schedule.Core.Models;

namespace Schedule.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SpecialtiesController : ControllerBase
{
    private readonly ISpecialtyRepository _specialtyRepository;

    public SpecialtiesController(
        ISpecialtyRepository specialtyRepository)
    {
        _specialtyRepository = specialtyRepository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Specialty>>> GetAll()
    {
        var specialties =
            await _specialtyRepository.GetAllAsync();

        return Ok(specialties);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Specialty>> GetById(int id)
    {
        var specialty =
            await _specialtyRepository.GetByIdAsync(id);

        if (specialty == null)
        {
            return NotFound(new
            {
                Message =
                    $"Спеціальність з ID {id} не знайдено."
            });
        }

        return Ok(specialty);
    }

    [HttpPost]
    public async Task<ActionResult<Specialty>> Create(
        [FromBody] Specialty specialty)
    {
        var validationResult = Validate(specialty);

        if (validationResult != null)
        {
            return validationResult;
        }

        try
        {
            int newId =
                await _specialtyRepository.CreateAsync(
                    specialty);

            specialty.Id = newId;

            return CreatedAtAction(
                nameof(GetById),
                new { id = specialty.Id },
                specialty);
        }
        catch (MySqlException ex)
            when (ex.Number == 1062)
        {
            return Conflict(new
            {
                Message =
                    "Спеціальність з таким кодом уже існує."
            });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] Specialty specialty)
    {
        if (id != specialty.Id)
        {
            return BadRequest(new
            {
                Message =
                    "ID у URL не збігається з ID у тілі запиту."
            });
        }

        var current =
            await _specialtyRepository.GetByIdAsync(id);

        if (current == null)
        {
            return NotFound(new
            {
                Message =
                    $"Спеціальність з ID {id} не знайдено."
            });
        }

        var validationResult = Validate(specialty);

        if (validationResult != null)
        {
            return validationResult;
        }

        try
        {
            bool updated =
                await _specialtyRepository.UpdateAsync(
                    specialty);

            if (!updated)
            {
                return NotFound(new
                {
                    Message =
                        $"Не вдалося оновити спеціальність з ID {id}."
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
                    "Спеціальність з таким кодом уже існує."
            });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var specialty =
            await _specialtyRepository.GetByIdAsync(id);

        if (specialty == null)
        {
            return NotFound(new
            {
                Message =
                    $"Спеціальність з ID {id} не знайдено."
            });
        }

        try
        {
            bool deleted =
                await _specialtyRepository.DeleteAsync(id);

            if (!deleted)
            {
                return NotFound(new
                {
                    Message =
                        $"Не вдалося видалити спеціальність з ID {id}."
                });
            }

            return NoContent();
        }
        catch (MySqlException ex)
            when (ex.Number == 1451)
        {
            return Conflict(new
            {
                Message =
                    "Неможливо видалити спеціальність, оскільки " +
                    "до неї вже прив'язані групи або дисципліни."
            });
        }
    }

    private ActionResult? Validate(Specialty specialty)
    {
        if (string.IsNullOrWhiteSpace(specialty.Code))
        {
            return BadRequest(new
            {
                Message =
                    "Код спеціальності не може бути порожнім."
            });
        }

        if (string.IsNullOrWhiteSpace(specialty.Name))
        {
            return BadRequest(new
            {
                Message =
                    "Назва спеціальності не може бути порожньою."
            });
        }

        specialty.Code = specialty.Code.Trim();
        specialty.Name = specialty.Name.Trim();

        if (specialty.Code.Length > 20)
        {
            return BadRequest(new
            {
                Message =
                    "Код спеціальності не може містити більше 20 символів."
            });
        }

        if (specialty.Name.Length > 200)
        {
            return BadRequest(new
            {
                Message =
                    "Назва спеціальності не може містити більше 200 символів."
            });
        }

        return null;
    }
}