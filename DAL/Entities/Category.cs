using System;
using System.Collections.Generic;
using System.Text;

namespace DAL.Entities
{
   public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property :
        //one category can have many products
        public ICollection<Product> Products { get; set; } = new List<Product>();

    }
}
