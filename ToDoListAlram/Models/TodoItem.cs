using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToDoListAlram.Models
{
    public class TodoItem
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

        public int GoogleSheetRowIndex { get; set; }
        public bool IsChecked { get; set; }
        public string Goal { get; set; } = "";
        public string Steps { get; set; } = "";
        public string Importance { get; set; } = "0";
        public string Difficulty { get; set; } = "0";
        public bool IsWaiting { get; set; }
        public DateTime DueDate { get; set; }
        public string? Remarks { get; set; }


        public static TodoItem FromGoogleSheetRow(IList<object> row)
        {
            string? isWaiting = row[4]?.ToString();
            string? isCompleted = row[5]?.ToString();

            if (isWaiting == "TRUE" && isCompleted == "FALSE")
            {
                if (row.Count < 8 || String.IsNullOrEmpty(row[7].ToString()))
                {
                    throw new InvalidOperationException("「等待中」欄位為 TRUE 時必須有備註說明！");
                }
            }
            return new TodoItem
            {
                Goal = row[0]?.ToString()!,
                Steps = row[1]?.ToString()!,
                Importance = row[2]?.ToString()!,
                Difficulty = row[3]?.ToString()!,
                IsWaiting = Convert.ToBoolean(row[4]?.ToString()!),
                DueDate = String.IsNullOrEmpty(row[6]?.ToString()) ? DateTime.Now.AddDays(7) : DateTime.Parse(row[6]?.ToString()!),
                Remarks = row.Count == 8 ? row[7]?.ToString() : "",
            };
        }
    }
}
