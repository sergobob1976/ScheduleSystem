using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using Schedule.Core.Interfaces;
using Schedule.Core.Models;

namespace Schedule.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GroupSpecialtiesController : ControllerBase
{
    private readonly IGroupSpecialtyRepository _repository;
    private readonly IGroupRepository _groupRepository;
    private readonly ISpecialtyRepository _specialtyRepository;

    public GroupSpecialtiesController(
        IGroupSpecialtyRepository repository,
        IGroupRepository groupRepository,
        ISpecialtyRepository specialtyRepository)
    {
        _repository = repository;
        _groupRepository = groupRepository;
        _specialtyRepository = specialtyRepository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<GroupSpecialty>>> GetAll(
        [FromQuery] int? groupId,
        [FromQuery] int? specialtyId)
    {
        if (groupId.HasValue)
        {
            var items =
                await _repository.GetByGroupIdAsync(
                    groupId.Value);

            return Ok(items);
        }

        if (specialtyId.HasValue)
        {
            var items =
                await _repository.GetBySpecialtyIdAsync(
                    specialtyId.Value);

            return Ok(items);
        }

        var allItems =
            await _repository.GetAllAsync();

        return Ok(allItems);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<GroupSpecialty>> GetById(
        int id)
    {
        var item =
            await _repository.GetByIdAsync(id);

        if (item == null)
        {
            return NotFound(new
            {
                Message =
                    $"Прив'язку групи до спеціальності з ID {id} не знайдено."
            });
        }

        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<GroupSpecialty>> Create(
        [FromBody] GroupSpecialty groupSpecialty)
    {
        var validationResult =
            await ValidateAsync(groupSpecialty);

        if (validationResult != null)
        {
            return validationResult;
        }

        var existing =
            await _repository.GetExistingAsync(
                groupSpecialty.GroupId,
                groupSpecialty.SpecialtyId);

        if (existing != null)
        {
            return Conflict(new
            {
                Message =
                    "Ця група вже прив'язана до обраної спеціальності."
            });
        }

        try
        {
            int newId =
                await _repository.CreateAsync(
                    groupSpecialty);

            var created =
                await _repository.GetByIdAsync(newId);

            if (created == null)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        Message =
                            "Прив'язку створено, але не вдалося отримати її з бази даних."
                    });
            }

            return CreatedAtAction(
                nameof(GetById),
                new { id = created.Id },
                created);
        }
        catch (MySqlException ex)
            when (ex.Number == 1062)
        {
            return Conflict(new
            {
                Message =
                    "Ця група вже прив'язана до обраної спеціальності."
            });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var current =
            await _repository.GetByIdAsync(id);

        if (current == null)
        {
            return NotFound(new
            {
                Message =
                    $"Прив'язку групи до спеціальності з ID {id} не знайдено."
            });
        }

        bool deleted =
            await _repository.DeleteAsync(id);

        if (!deleted)
        {
            return NotFound(new
            {
                Message =
                    $"Не вдалося видалити прив'язку з ID {id}."
            });
        }

        return NoContent();
    }

    private async Task<ActionResult?> ValidateAsync(
        GroupSpecialty groupSpecialty)
    {
        if (groupSpecialty.GroupId <= 0)
        {
            return BadRequest(new
            {
                Message = "Потрібно обрати групу."
            });
        }

        if (groupSpecialty.SpecialtyId <= 0)
        {
            return BadRequest(new
            {
                Message = "Потрібно обрати спеціальність."
            });
        }

        var group =
            await _groupRepository.GetByIdAsync(
                groupSpecialty.GroupId);

        if (group == null)
        {
            return BadRequest(new
            {
                Message =
                    "Обрану групу не знайдено."
            });
        }

        var specialty =
            await _specialtyRepository.GetByIdAsync(
                groupSpecialty.SpecialtyId);

        if (specialty == null)
        {
            return BadRequest(new
            {
                Message =
                    "Обрану спеціальність не знайдено."
            });
        }

        return null;
    }
}