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
        public bool IsCompleted { get; set; }
        public DateTime DueDate { get; set; }
        public string? Remarks { get; set; }

        private static void ValidateSheetRow(IList<object> row)
        {

            string? todo = row[0]?.ToString();
            if (String.IsNullOrEmpty(todo))
            {
                throw new InvalidOperationException("第一欄位 (目標)不可為空");
            }
            if (row == null || row.Count < 7)
            {
                throw new InvalidOperationException($"({todo}) 無效的 row 長度");
            }
            if (!Int32.TryParse(row[2]?.ToString(), out int _))
            {
                throw new InvalidOperationException($"({todo}) 第三欄位 (重要性) 必須是數字");
            }
            if (!Int32.TryParse(row[3]?.ToString(), out int _))
            {
                throw new InvalidOperationException($"({todo}) 第四欄位 (難度) 必須是數字");
            }
            if (row[4]?.ToString() != "TRUE" && row[4]?.ToString() != "FALSE")
            {
                throw new InvalidOperationException($"({todo}) 第五欄位 (等待中) 必須是 TRUE 或 FALSE");
            }
            if (row[5]?.ToString() != "TRUE" && row[5]?.ToString() != "FALSE")
            {
                throw new InvalidOperationException($"({todo}) 第六欄位 (已完成) 必須是 TRUE 或 FALSE");
            }
            if (!DateTime.TryParse(row[6]?.ToString(), out DateTime _))
            {
                throw new InvalidOperationException($"({todo}) 第七欄位 (期限) 必須是時間");
            }
            if (row[4]?.ToString() == "TRUE" && row[5]?.ToString() == "FALSE")
            {
                if (row.Count < 8 || String.IsNullOrEmpty(row[7]?.ToString()))
                {
                    throw new InvalidOperationException($"({todo}) 若等待中為 TRUE，第八欄位 (備註) 不可為空");
                }
            }
        }

        public static TodoItem FromGoogleSheetRow(IList<object> row)
        {
            TodoItem.ValidateSheetRow(row);
            return new TodoItem
            {
                Goal = row[0]?.ToString()!,
                Steps = row[1]?.ToString()!,
                Importance = row[2]?.ToString()!,
                Difficulty = row[3]?.ToString()!,
                IsWaiting = Convert.ToBoolean(row[4]?.ToString()!),
                IsCompleted = Convert.ToBoolean(row[5]?.ToString()!),
                DueDate = DateTime.Parse(row[6]?.ToString()!),
                Remarks = row.Count == 8 ? row[7]?.ToString() : "",
            };
        }
    }
}
