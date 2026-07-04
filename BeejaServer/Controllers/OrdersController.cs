using Microsoft.AspNetCore.Mvc;
using BeejaServer.Data;
using BeejaServer.Models;

namespace BeejaServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public OrdersController(AppDbContext context)
        {
            _context = context;
        }

        // кто-то делает заказ этот метод вызывается 
        [HttpPost]
        public IActionResult CreateOrder(int productId, int userId, int gameId)
        {
            // Проверка: не заказывал ли он чего-то последние 15 минут?
            var lastOrder = _context.Orders
                 .Where(o => o.UserId == userId)
                 .OrderByDescending(o => o.OrderedAt)
                 .FirstOrDefault();

            if (lastOrder != null && (DateTime.Now - lastOrder.OrderedAt).TotalMinutes < 15)
            {
                return BadRequest("Подожди 15 минут перед следующим заказом!");
            }
            // берем продукт из базы вместе с ценой и коэфициентом 
            var product = _context.Products.FirstOrDefault(p => p.ProductId == productId);
            if (product == null) return NotFound("Товар не найден"); 

            //считаем цену сами , сюда добавить мат модель или сделать ссылку на код короче да
            decimal finalPrice = product.BasePrice * product.Multiplier;

            // создаем заказик
            var order = new Order
            { 
                ProductId = productId,
                UserId = userId,
                GameId = gameId,
                FinalPrice = finalPrice,
                OrderedAt = DateTime.Now
            };

            _context.Orders.Add(order);
            _context.SaveChanges();

            return Ok(new { message = "Заказ принят!", orderId = order.Id });
        }

        // этот метод чтоб бармен увидел список заказов 
        [HttpGet]
        public IActionResult GetOrders()
        {
            var orders = _context.Orders.ToList();
            return Ok(orders);
        }
    }
}