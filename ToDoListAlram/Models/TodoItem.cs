using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToDoListAlram.Models
{
    internal class TodoItem
    {
        public static List<TodoItem> GetTestList()
        {
            var result = new List<TodoItem>();
            for (int i = 0; i < 50; i++)
            {
                result.Add(new TodoItem()
                {
                    Goal = $"測試目標{i / 10 + 1}",
                    Steps = $"測試步驟{i % 10 + 1}",
                    Importance = (i % 3 + 1).ToString(),
                    Difficulty = (i % 3 + 1).ToString(),
                    DueDate = DateTime.Now,
                });
            }
            return result;
        }
        public string Goal { get; set; } = "";
        public string Steps { get; set; } = "";
        public string Importance { get; set; } = "0";
        public string Difficulty { get; set; } = "0";
        public DateTime DueDate { get; set; } // 可視需要改為 DateTime
        public string? Remarks { get; set; }
    }
}
