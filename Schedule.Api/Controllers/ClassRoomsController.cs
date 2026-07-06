using Microsoft.AspNetCore.Mvc;
using Schedule.Core.Interfaces;
using Schedule.Core.Models;

namespace Schedule.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClassRoomsController : ControllerBase
{
    private readonly IClassRoomRepository _classRoomRepository;

    public ClassRoomsController(IClassRoomRepository classRoomRepository)
    {
        _classRoomRepository = classRoomRepository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ClassRoom>>> GetAll()
    {
        var rooms = await _classRoomRepository.GetAllAsync();
        return Ok(rooms);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ClassRoom>> GetById(int id)
    {
        var room = await _classRoomRepository.GetByIdAsync(id);
        if (room == null) return NotFound(new { Message = "Аудиторію не знайдено." });
        return Ok(room);
    }

    [HttpPost]
    public async Task<ActionResult<ClassRoom>> Create([FromBody] ClassRoom classRoom)
    {
        if (string.IsNullOrWhiteSpace(classRoom.Name))
            return BadRequest(new { Message = "Назва аудиторії не може бути порожньою." });

        var newId = await _classRoomRepository.CreateAsync(classRoom);
        classRoom.Id = newId;

        return CreatedAtAction(nameof(GetById), new { id = classRoom.Id }, classRoom);
    }
}