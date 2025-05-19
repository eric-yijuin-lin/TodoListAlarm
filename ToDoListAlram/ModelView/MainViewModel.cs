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
        private readonly string _sheetId = "1DHrseaJEFdbsAcM3NP_UvyfMkjFuH82dYx6uH_v8Ov0";
        private readonly string _todoListTabName = "Todo";
        private readonly string _rewardTabName = "獎勵";
        private readonly TodoSheetService todoSheetService;
        public Dictionary<string, Dictionary<string, Exception>> errorDict = new ();

        public List<TodoItem> TodoList { get; private set; } = new List<TodoItem>();

        public MainViewModel()
        {
            string credPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Credentials", "google-sheet-api.json");
            var sheetApi = GoogleCredentialProvider.CreateSheetsApi(credPath);
            this.todoSheetService = new TodoSheetService(sheetApi);
        }

        public void LoadTodoList()
        {
            try
            {
                this.TodoList = todoSheetService.GetTodoItems(_sheetId, _todoListTabName, null);
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

        public void CompleteTodoItems(List<TodoItem>? todoItems)
        {
            throw new NotImplementedException();
            //if (todoItems == null || todoItems.Count == 0)
            //{
            //    return;
            //}

            //var updateValueRanges = this.GetCompleteTodoRequest(_todoListTabName, todoItems);
            //this.todoSheetService.Spreadsheets.Values.BatchUpdate(updateValueRanges);
        }

        private BatchUpdateValuesRequest GetCompleteTodoRequest(string sheetName, List<TodoItem> todoItems, string columnCode = "F")
        {
            var valueRanges = new List<ValueRange>();
            foreach (var item in todoItems)
            {
                valueRanges.Add(new ValueRange
                {
                    Range = $"{sheetName}|{columnCode}{item.GoogleSheetRowIndex}",
                    Values = new List<IList<object>>
                    {
                        new List<object> { "TRUE" }
                    }
                });
            }
            var updateRequest = new BatchUpdateValuesRequest()
            {
                ValueInputOption = "USER_ENTERED",
                Data = valueRanges
            };
            return updateRequest;
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

        public List<TodoItem> GetFilteredList(string durationTag)
        {
            double remainDay = double.MaxValue; ;
            switch (durationTag)
            {
                case "Week":
                    remainDay = 7;
                    break;
                case "OneMonth":
                    remainDay = 30;
                    break;
                case "TwoMonths":
                    remainDay = 60;
                    break;
                default:
                    break;
            }
            return this.TodoList
                .Where(
                    x => (x.DueDate - DateTime.Now).TotalDays < remainDay)
                .ToList();
        }
    }
}
