using Google.Apis.Sheets.v4.Data;
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
        public Dictionary<string, Dictionary<string, Exception>> errorDict = new ();

        public List<TodoItem> TodoList { get; private set; } = new List<TodoItem>();

        public MainViewModel()
        {
            string credPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Credentials", "google-sheet-api.json");
            var sheetApi = GoogleCredentialProvider.CreateSheetsApi(credPath);
            googleSheetService = new GoogleSheetService(sheetApi);
        }


        public void LoadTodoList()
        {
            try
            {
                var sheetRows = googleSheetService.ReadSheet("1DHrseaJEFdbsAcM3NP_UvyfMkjFuH82dYx6uH_v8Ov0", "Todo", null);
                this.TodoList = sheetRows
                    .Skip(1)
                    .Where(row => row[5]?.ToString() == "FALSE" && !String.IsNullOrEmpty(row[0].ToString()))
                    .Select(TodoItem.FromGoogleSheetRow)
                    .OrderBy(item => item.DueDate)
                    .ThenByDescending(item => int.Parse(item.Importance))
                    .ThenBy(item => int.Parse(item.Difficulty))
                    .ThenBy(item => item.Goal)
                    .ThenBy(item => item.Steps)
                    .ToList();
            }
            catch (System.Net.Http.HttpRequestException requestEx)
            {
                this.SetErrorDictionary("Load", "Http", requestEx);
            }
            catch (FormatException formatEx)
            {
                this.SetErrorDictionary("Load", "Format", formatEx);
            }
            catch (InvalidOperationException operationEx)
            {
                this.SetErrorDictionary("Load", "Operation", operationEx);
            }
        }

        private void SetErrorDictionary(string taskType, string errorType, Exception ex)
        {
            if (!this.errorDict.ContainsKey(taskType))
            {
                this.errorDict[taskType] = new Dictionary<string, Exception>();
            }
            this.errorDict[taskType][errorType] = ex;
        }

        public bool HasError(string taskType)
        {
            return this.errorDict.ContainsKey(taskType) && this.errorDict[taskType].Any();
        }

        public string GetErrorMessage(string taskType, bool flush = true)
        {
            string message = "";
            var errors = this.errorDict[taskType];
            foreach (var error in errors)
            {
                message += $"[{error.Key}]  {error.Value.Message}\n";
            }
            if (flush)
            {
                this.errorDict.Remove(taskType);
            }
            return message;
        }
    }
}
