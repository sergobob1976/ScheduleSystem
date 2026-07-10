using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using Schedule.Core.Interfaces;
using Schedule.Core.Models;

namespace Schedule.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClassRoomsController : ControllerBase
{
    private readonly IClassRoomRepository _classRoomRepository;

    public ClassRoomsController(
        IClassRoomRepository classRoomRepository)
    {
        _classRoomRepository = classRoomRepository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ClassRoom>>> GetAll()
    {
        var classRooms =
            await _classRoomRepository.GetAllAsync();

        return Ok(classRooms);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ClassRoom>> GetById(int id)
    {
        var classRoom =
            await _classRoomRepository.GetByIdAsync(id);

        if (classRoom == null)
        {
            return NotFound(new
            {
                Message =
                    $"Аудиторію з ID {id} не знайдено."
            });
        }

        return Ok(classRoom);
    }

    [HttpPost]
    public async Task<ActionResult<ClassRoom>> Create(
        [FromBody] ClassRoom classRoom)
    {
        var validationResult = Validate(classRoom);

        if (validationResult != null)
        {
            return validationResult;
        }

        try
        {
            int newId =
                await _classRoomRepository.CreateAsync(
                    classRoom);

            classRoom.Id = newId;

            return CreatedAtAction(
                nameof(GetById),
                new { id = classRoom.Id },
                classRoom);
        }
        catch (MySqlException ex)
            when (ex.Number == 1062)
        {
            return Conflict(new
            {
                Message =
                    "Аудиторія з такою назвою вже існує."
            });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] ClassRoom classRoom)
    {
        if (id != classRoom.Id)
        {
            return BadRequest(new
            {
                Message =
                    "ID у URL не збігається з ID у тілі запиту."
            });
        }

        var current =
            await _classRoomRepository.GetByIdAsync(id);

        if (current == null)
        {
            return NotFound(new
            {
                Message =
                    $"Аудиторію з ID {id} не знайдено."
            });
        }

        var validationResult = Validate(classRoom);

        if (validationResult != null)
        {
            return validationResult;
        }

        try
        {
            bool updated =
                await _classRoomRepository.UpdateAsync(
                    classRoom);

            if (!updated)
            {
                return NotFound(new
                {
                    Message =
                        $"Не вдалося оновити аудиторію з ID {id}."
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
                    "Аудиторія з такою назвою вже існує."
            });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var classRoom =
            await _classRoomRepository.GetByIdAsync(id);

        if (classRoom == null)
        {
            return NotFound(new
            {
                Message =
                    $"Аудиторію з ID {id} не знайдено."
            });
        }

        try
        {
            bool deleted =
                await _classRoomRepository.DeleteAsync(id);

            if (!deleted)
            {
                return NotFound(new
                {
                    Message =
                        $"Не вдалося видалити аудиторію з ID {id}."
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
                    "Неможливо видалити аудиторію, оскільки " +
                    "вона вже використовується в розкладі."
            });
        }
    }

    private ActionResult? Validate(ClassRoom classRoom)
    {
        if (string.IsNullOrWhiteSpace(classRoom.Name))
        {
            return BadRequest(new
            {
                Message =
                    "Назва аудиторії не може бути порожньою."
            });
        }

        classRoom.Name = classRoom.Name.Trim();

        if (classRoom.Name.Length > 50)
        {
            return BadRequest(new
            {
                Message =
                    "Назва аудиторії не може містити більше 50 символів."
            });
        }

        return null;
    }
}