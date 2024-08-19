using System.Collections.Generic;

namespace api_my_web.Models // Projeye uygun namespace
{
    public class Destination
    {
        public int Id { get; set; } // Birincil anahtar olarak kullanılacak özellik
        public string Name { get; set; } = string.Empty; // Null olamaz
        public string DescriptionEnglish { get; set; } = string.Empty; // Zorunlu özellik
        public List<string> AttractionsEnglish { get; set; } = new List<string>(); // Zorunlu özellik
        public List<string> LocalDishesEnglish { get; set; } = new List<string>(); // Zorunlu özellik
    }
}
