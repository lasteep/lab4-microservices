using System.Net;
using Microsoft.AspNetCore.Mvc;
using OrderService.Models;

namespace OrderService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class OrdersController : ControllerBase
{
    private static readonly List<Order> Orders = new();
    private static readonly object Sync = new();

    private readonly HttpClient _users;

    public OrdersController(IHttpClientFactory httpClientFactory)
    {
        // базова адреса береться з appsettings.json -> Services:UserServiceUrl
        _users = httpClientFactory.CreateClient("UserService");
    }

    /// <summary>Повертає всі замовлення.</summary>
    [HttpGet]
    public ActionResult<IEnumerable<Order>> GetOrders()
    {
        lock (Sync) return Ok(Orders.ToList());
    }

    /// <summary>Повертає замовлення за id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(Order), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<Order> GetOrder(int id)
    {
        lock (Sync)
        {
            var order = Orders.FirstOrDefault(o => o.Id == id);
            return order is null ? NotFound() : Ok(order);
        }
    }

    /// <summary>Створює замовлення. Перед цим питає UserService, чи існує користувач.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(Order), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> CreateOrder(Order order)
    {
        if (await FetchUser(order.UserId) is null)
            return BadRequest($"User {order.UserId} not found in UserService");

        lock (Sync)
        {
            if (order.Id == 0)
                order.Id = Orders.Count == 0 ? 1 : Orders.Max(o => o.Id) + 1;
            else if (Orders.Any(o => o.Id == order.Id))
                return Conflict($"Order with id {order.Id} already exists");

            Orders.Add(order);
        }
        return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
    }

    /// <summary>Оновлює замовлення.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult UpdateOrder(int id, Order updated)
    {
        lock (Sync)
        {
            var order = Orders.FirstOrDefault(o => o.Id == id);
            if (order is null) return NotFound();

            order.UserId = updated.UserId;
            order.ProductName = updated.ProductName;
            order.Price = updated.Price;
        }
        return NoContent();
    }

    /// <summary>Видаляє замовлення.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult DeleteOrder(int id)
    {
        lock (Sync)
        {
            var removed = Orders.RemoveAll(o => o.Id == id);
            return removed == 0 ? NotFound() : NoContent();
        }
    }

    /// <summary>Отримує користувача з UserService і всі його замовлення.</summary>
    [HttpGet("user/{userId:int}")]
    [ProducesResponseType(typeof(UserOrders), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserOrders>> GetUser(int userId)
    {
        var user = await FetchUser(userId);
        if (user is null)
            return NotFound($"User {userId} not found in UserService");

        lock (Sync)
            return Ok(new UserOrders { User = user, Orders = Orders.Where(o => o.UserId == userId).ToList() });
    }

    private async Task<User?> FetchUser(int userId)
    {
        var response = await _users.GetAsync($"api/users/{userId}");
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<User>();
    }
}
