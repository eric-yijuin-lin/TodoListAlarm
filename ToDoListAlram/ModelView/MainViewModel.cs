using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToDoListAlram.Models;
using ToDoListAlram.Models.Services;

namespace ToDoListAlram.ModelView
{
    // TODO: 改成 INotifyPropertyChanged + ICommand 
    internal class MainViewModel
    {
        private readonly GoogleSheetService googleSheetService;

        public List<TodoItem> TodoList { get; private set; } = new List<TodoItem>();

        public MainViewModel()
        {
            string credPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Credentials", "google-sheet-api.json");
            var sheetApi = GoogleCredentialProvider.CreateSheetsApi(credPath);
            googleSheetService = new GoogleSheetService(sheetApi);
        }


        public void LoadTodoList()
        {
            var sheetRows = googleSheetService.ReadSheet("1DHrseaJEFdbsAcM3NP_UvyfMkjFuH82dYx6uH_v8Ov0", "Todo", null);
            this.TodoList = sheetRows
                .Skip(1)
                .Where(row => row[5]?.ToString() == "FALSE" && !String.IsNullOrEmpty(row[0].ToString()))
                .Select(row => new TodoItem
                {
                    Goal = row[0]?.ToString()!,
                    Steps = row[1]?.ToString()!,
                    Importance = row[2]?.ToString()!,
                    Difficulty = row[3]?.ToString()!,
                    DueDate = String.IsNullOrEmpty(row[6]?.ToString()) ? DateTime.Now.AddDays(7) : DateTime.Parse(row[6]?.ToString()!),
                    Remarks = row.Count == 8 ? row[7]?.ToString() : "",
                })
                .OrderBy(item => item.DueDate)
                .ThenByDescending(item => int.Parse(item.Importance))
                .ThenBy(item => int.Parse(item.Difficulty))
                .ThenBy(item => item.Goal)
                .ThenBy(item => item.Steps)
                .ToList();
        }
    }
}
