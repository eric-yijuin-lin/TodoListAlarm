using Google;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
        private readonly TodoListService _todoSheetService;

        private readonly Dictionary<string, Dictionary<string, Exception>> errorDict = new ();

        public int RewardPoint { get; private set; }

        public List<TodoItem> TodoList { get; private set; } = new List<TodoItem>();

        public MainViewModel(TodoListService todoSheetService)
        {
            _todoSheetService = todoSheetService;
            this.RewardPoint = _todoSheetService.GetCurrentRewardPoint();
        }

        public void LoadTodoList()
        {
            try
            {
                this.TodoList = _todoSheetService
                    .GetTodoItems()
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
            catch (InvalidOperationException operationEx)
            {
                this.SetErrorDictionary("Load", "Operation", operationEx);
            }
        }

        public void CompleteTodoItems(List<TodoItem> itemsToComplete)
        {
            if (itemsToComplete.Count == 0)
            {
                return;
            }

            try
            {
                _todoSheetService.CompleteTodoItems(itemsToComplete);
            }
            catch (System.Net.Http.HttpRequestException requestEx)
            {
                this.SetErrorDictionary("Update", "Http", requestEx);
            }
            catch (GoogleApiException googleEx)
            {
                this.SetErrorDictionary("Update", "Api", googleEx);
            }
            catch (InvalidOperationException operationEx)
            {
                this.SetErrorDictionary("Update", "Operation", operationEx);
            }
        }

        public void ConsumeRewardPoint(int point)
        {
            if (point > this.RewardPoint)
            {
                this.SetErrorDictionary("Consume", "Check",
                    new InvalidOperationException($"點數不足，目前點數: {this.RewardPoint}"));
                return;
            }
            try
            {
                _todoSheetService.ConsumeRewardPoint(point);
            }
            catch (System.Net.Http.HttpRequestException requestEx)
            {
                this.SetErrorDictionary("Consume", "Http", requestEx);
            }
            catch (GoogleApiException googleEx)
            {
                this.SetErrorDictionary("Consume", "Api", googleEx);
            }
            catch (InvalidOperationException operationEx)
            {
                this.SetErrorDictionary("Consume", "Operation", operationEx);
            }
            this.RewardPoint -= point;
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
