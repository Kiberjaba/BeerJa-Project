using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System;

namespace BeejaServer.Controllers
{
    [Route("api/articles")]
    [ApiController]
    public class ArticlesController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public ArticlesController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult GetArticles()
        {
            var articles = new List<object>();
            var connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT text_id, title, excerpt, content, tag, read_time, is_featured, published_at FROM articles ORDER BY published_at DESC";
                
                using (var command = new MySqlCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            articles.Add(new {
                                textId = reader["text_id"],
                                title = reader["title"],
                                excerpt = reader["excerpt"],
                                content = reader["content"] != DBNull.Value ? reader["content"] : null,
                                tag = reader["tag"],
                                readTime = reader["read_time"],
                                isFeatured = reader.GetBoolean("is_featured"),
                                publishedAt = Convert.ToDateTime(reader["published_at"]).ToString("d MMMM")
                            });
                        }
                    }
                }
            }

            return Ok(articles);
        }
    }
}