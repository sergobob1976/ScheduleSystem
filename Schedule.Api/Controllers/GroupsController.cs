using Microsoft.AspNetCore.Mvc;
using Schedule.Core.Interfaces;
using Schedule.Core.Models;

namespace Schedule.Api.Controllers;

[ApiController]
[Route("api/[controller]")] // Наш маршрут буде: api/groups
public class GroupsController : ControllerBase
{
    private readonly IGroupRepository _groupRepository;

    // Через конструктор .NET автоматично підставить сюди наш GroupRepository,
    // бо ми зареєстрували його в Program.cs (Dependency Injection)
    public GroupsController(IGroupRepository groupRepository)
    {
        _groupRepository = groupRepository;
    }

    // 1. Отримати всі групи (GET api/groups)
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Group>>> GetAll()
    {
        var groups = await _groupRepository.GetAllAsync();
        return Ok(groups);
    }

    // 2. Отримати групу за ID (GET api/groups/5)
    [HttpGet("{id}")]
    public async Task<ActionResult<Group>> GetById(int id)
    {
        var group = await _groupRepository.GetByIdAsync(id);
        if (group == null)
        {
            return NotFound(new { Message = $"Групу з ID {id} не знайдено." });
        }
        return Ok(group);
    }

    // 3. Створити нову групу (POST api/groups)
    [HttpPost]
    public async Task<ActionResult<Group>> Create([FromBody] Group group)
    {
        if (string.IsNullOrWhiteSpace(group.Name))
        {
            return BadRequest(new { Message = "Назва групи не може бути порожньою." });
        }

        // CreateAsync поверне ID, який MySQL присвоїв групі (AUTO_INCREMENT)
        var newId = await _groupRepository.CreateAsync(group);
        group.Id = newId;

        // Повертаємо статус 201 Created та посилання на створений ресурс
        return CreatedAtAction(nameof(GetById), new { id = group.Id }, group);
    }

    // 4. Оновити групу (PUT api/groups/5)
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] Group group)
    {
        if (id != group.Id)
        {
            return BadRequest(new { Message = "ID в URL не збігається з ID в тілі запиту." });
        }

        var updated = await _groupRepository.UpdateAsync(group);
        if (!updated)
        {
            return NotFound(new { Message = $"Не вдалося оновити. Групу з ID {id} не знайдено." });
        }

        return NoContent(); // Стандартна успішна відповідь без тіла (204)
    }

    // 5. Видалити групу (DELETE api/groups/5)
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _groupRepository.DeleteAsync(id);
        if (!deleted)
        {
            return NotFound(new { Message = $"Не вдалося видалити. Групу з ID {id} не знайдено." });
        }

        return NoContent();
    }
}
