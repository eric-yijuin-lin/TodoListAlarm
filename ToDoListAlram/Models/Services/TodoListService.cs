using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToDoListAlram.Models;

namespace ToDoListAlram.Models.Services
{
    public static class GoogleCredentialProvider
    {
        public static SheetsService CreateSheetsApi(string credentialPath)
        {
            var credential = GoogleCredential
                .FromFile(credentialPath)
                .CreateScoped(SheetsService.Scope.Spreadsheets);


            return new SheetsService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "ToDoListAlram"
            });
        }
    }

    public class TodoListService
    {
        private readonly string _sheetId = "1DHrseaJEFdbsAcM3NP_UvyfMkjFuH82dYx6uH_v8Ov0";
        private readonly string _todoListTabName = "Todo";
        private readonly string _rewardTabName = "獎勵";
        private readonly string _completedColumnCode = "F";
        private readonly string _rewardCell = "H1";
        private readonly string _currentPointRange = $"獎勵!H1";
        private readonly SheetsService _googleSheetService;

        public TodoListService(SheetsService? service)
        {
            if (service == null)
            {
                throw new ArgumentNullException("Google Sheets API service 不可以為空.");
            }
            _googleSheetService = service;
        }

        private IList<IList<object>> GetSheetRows(string sheetId, string tabName)
        {
            SpreadsheetsResource.ValuesResource.GetRequest request =
                _googleSheetService.Spreadsheets.Values.Get(sheetId, tabName);
            ValueRange response = request.Execute();
            IList<IList<object>> values = response.Values;
            if (values != null)
            {
                return values;
            }
            return new List<IList<object>>();
        }

        public void CompleteTodoItems(List<TodoItem> itemsToComplete)
        {
            int addPoint = itemsToComplete.Sum(x => Convert.ToInt32(x.Difficulty) * Convert.ToInt32(x.Importance));
            var updateStatusResponse = this.UpdateCompletedStatus(itemsToComplete);
            var appendRecordResponse = this.AppendCompletedRecord(itemsToComplete);
            var updatePointResponse = this.UpdateRewardPoint(addPoint);
            if (updateStatusResponse.TotalUpdatedRows <= 0
                || appendRecordResponse.Updates.UpdatedRows <= 0
                || updatePointResponse.UpdatedRows <= 0)
            {
                throw new InvalidOperationException("更新 Todo、寫入 Record 或更新點數出現 0 affected response");
            }
        }

        public void ConsumeRewardPoint(int consumePoint)
        {
            var updateResponse = this.UpdateRewardPoint(consumePoint * -1);
            if (updateResponse.UpdatedRows <= 0)
            {
                throw new InvalidOperationException("更新獎勵點出現 0 affected response");
            }
            this.AppendCompletedRecord(new List<TodoItem>
            {
                new TodoItem
                {
                    Goal = "消耗獎勵點",
                    Importance = consumePoint.ToString(),
                    Difficulty = "-1",
                    IsCompleted = true,
                    Remarks = ""
                }
            });
        }

        private ValueRange GetAppendRewardValueRange(List<TodoItem> itemsToComplete)
        {
            var rowValues = itemsToComplete
                .Select(item => (IList<object>)new List<object>
                {
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    item.Goal,
                    item.Importance,
                    item.Difficulty,
                    Convert.ToInt32(item.Importance) * Convert.ToInt32(item.Difficulty)
                })
                .ToList();
            return new ValueRange
            {
                Values = rowValues
            };
        }


        private BatchUpdateValuesResponse UpdateCompletedStatus(List<TodoItem> itemsToComplete)
        {
            var requestBody = this.GetCompleteTodoRequest(itemsToComplete);
            var updateRequest = _googleSheetService.Spreadsheets.Values.BatchUpdate(requestBody, _sheetId);
            return updateRequest.Execute();
        }

        private AppendValuesResponse AppendCompletedRecord(List<TodoItem> itemsToComplete)
        {
            var valueRange = GetAppendRewardValueRange(itemsToComplete);
            string appendRange = $"{_rewardTabName}!A1";
            var appendRequest = _googleSheetService.Spreadsheets.Values.Append(valueRange, _sheetId, appendRange);
            appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
            appendRequest.InsertDataOption = SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum.INSERTROWS;
            var response = appendRequest.Execute();
            return response;
        }

        private UpdateValuesResponse UpdateRewardPoint(int changeAmount)
        {
            int currentPoint = this.GetCurrentRewardPoint();
            var valueRange = new ValueRange
            {
                Values = new List<IList<object>>
                {
                    new List<object>
                    {
                        currentPoint + changeAmount
                    }
                }
            };

            var request = _googleSheetService.Spreadsheets.Values.Update(valueRange, _sheetId, _currentPointRange);
            request.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
            return request.Execute();
        }

        private BatchUpdateValuesRequest GetCompleteTodoRequest( List<TodoItem> todoItems)
        {
            var valueRanges = new List<ValueRange>();
            foreach (var item in todoItems)
            {
                valueRanges.Add(new ValueRange
                {
                    Range = $"{_todoListTabName}!{_completedColumnCode}{item.GoogleSheetRowIndex}",
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

        public int GetCurrentRewardPoint()
        {
            var response = _googleSheetService.Spreadsheets.Values.Get(_sheetId, _currentPointRange).Execute();
            string? rawValue = response?.Values?.FirstOrDefault()?.FirstOrDefault()?.ToString();
            if (!Int32.TryParse(rawValue, out int currentPoint))
            {
                throw new InvalidOperationException($"無法取得目前獎勵點數。rawValue={rawValue}");
            }
            return currentPoint;
        }

        public List<TodoItem> GetTodoItems()
        {
            var sheetRows = this.GetSheetRows(_sheetId, _todoListTabName);
            var todoList = new List<TodoItem>();
            for (int i = 1; i < sheetRows.Count; i++)
            {
                var row = sheetRows[i];
                var todoItem = TodoItem.FromGoogleSheetRow(row);
                if (todoItem.IsCompleted)
                {
                    continue;
                }
                todoItem.GoogleSheetRowIndex = i + 1;
                todoList.Add(todoItem);
            }
            return todoList;
        }
        // TODO: public void SaveTodoItems(List<TodoItem> items) {}
    }
}
