using Microsoft.AspNetCore.Mvc;
using UserService.Models;

namespace UserService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private static readonly List<User> Users = new();
    private static readonly object Sync = new();

    /// <summary>Повертає всіх користувачів.</summary>
    [HttpGet]
    public ActionResult<IEnumerable<User>> GetUsers()
    {
        lock (Sync) return Ok(Users.ToList());
    }

    /// <summary>Повертає користувача за id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(User), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<User> GetUser(int id)
    {
        lock (Sync)
        {
            var user = Users.FirstOrDefault(u => u.Id == id);
            return user is null ? NotFound() : Ok(user);
        }
    }

    /// <summary>Створює користувача. Якщо Id = 0, його призначить сервіс.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(User), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public ActionResult CreateUser(User user)
    {
        lock (Sync)
        {
            if (user.Id == 0)
                user.Id = Users.Count == 0 ? 1 : Users.Max(u => u.Id) + 1;
            else if (Users.Any(u => u.Id == user.Id))
                return Conflict($"User with id {user.Id} already exists");

            Users.Add(user);
        }
        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
    }

    /// <summary>Оновлює дані користувача.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult UpdateUser(int id, User updated)
    {
        lock (Sync)
        {
            var user = Users.FirstOrDefault(u => u.Id == id);
            if (user is null) return NotFound();

            user.FirstName = updated.FirstName;
            user.LastName = updated.LastName;
            user.Email = updated.Email;
        }
        return NoContent();
    }

    /// <summary>Видаляє користувача.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult DeleteUser(int id)
    {
        lock (Sync)
        {
            var removed = Users.RemoveAll(u => u.Id == id);
            return removed == 0 ? NotFound() : NoContent();
        }
    }
}
