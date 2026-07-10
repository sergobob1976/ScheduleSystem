using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using Schedule.Core.Interfaces;
using Schedule.Core.Models;

namespace Schedule.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GroupsController : ControllerBase
{
    private readonly IGroupRepository _groupRepository;

    public GroupsController(
        IGroupRepository groupRepository)
    {
        _groupRepository = groupRepository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Group>>> GetAll()
    {
        var groups =
            await _groupRepository.GetAllAsync();

        return Ok(groups);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Group>> GetById(int id)
    {
        var group =
            await _groupRepository.GetByIdAsync(id);

        if (group == null)
        {
            return NotFound(new
            {
                Message =
                    $"Групу з ID {id} не знайдено."
            });
        }

        return Ok(group);
    }

    [HttpPost]
    public async Task<ActionResult<Group>> Create(
        [FromBody] Group group)
    {
        var validationResult = Validate(group);

        if (validationResult != null)
        {
            return validationResult;
        }

        try
        {
            int newId =
                await _groupRepository.CreateAsync(group);

            group.Id = newId;

            return CreatedAtAction(
                nameof(GetById),
                new { id = group.Id },
                group);
        }
        catch (MySqlException ex)
            when (ex.Number == 1062)
        {
            return Conflict(new
            {
                Message =
                    "Група з такою назвою вже існує."
            });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] Group group)
    {
        if (id != group.Id)
        {
            return BadRequest(new
            {
                Message =
                    "ID у URL не збігається з ID у тілі запиту."
            });
        }

        var current =
            await _groupRepository.GetByIdAsync(id);

        if (current == null)
        {
            return NotFound(new
            {
                Message =
                    $"Групу з ID {id} не знайдено."
            });
        }

        var validationResult = Validate(group);

        if (validationResult != null)
        {
            return validationResult;
        }

        try
        {
            bool updated =
                await _groupRepository.UpdateAsync(group);

            if (!updated)
            {
                return NotFound(new
                {
                    Message =
                        $"Не вдалося оновити групу з ID {id}."
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
                    "Група з такою назвою вже існує."
            });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var group =
            await _groupRepository.GetByIdAsync(id);

        if (group == null)
        {
            return NotFound(new
            {
                Message =
                    $"Групу з ID {id} не знайдено."
            });
        }

        try
        {
            bool deleted =
                await _groupRepository.DeleteAsync(id);

            if (!deleted)
            {
                return NotFound(new
                {
                    Message =
                        $"Не вдалося видалити групу з ID {id}."
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
                    "Неможливо видалити групу, оскільки " +
                    "вона вже використовується у дисциплінах, " +
                    "навантаженні або розкладі."
            });
        }
    }

    private ActionResult? Validate(Group group)
    {
        if (string.IsNullOrWhiteSpace(group.Name))
        {
            return BadRequest(new
            {
                Message =
                    "Назва групи не може бути порожньою."
            });
        }

        group.Name = group.Name.Trim();

        if (group.Name.Length > 50)
        {
            return BadRequest(new
            {
                Message =
                    "Назва групи не може містити більше 50 символів."
            });
        }

        return null;
    }
}