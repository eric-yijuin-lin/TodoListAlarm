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
            //using var stream = new FileStream(credentialPath, FileMode.Open, FileAccess.Read);

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

    public class TodoSheetService
    {
        private readonly SheetsService _googleSheetService;

        public TodoSheetService(SheetsService service)
        {
            _googleSheetService = service;
        }

        private IList<IList<object>> ReadRawData(string sheetId, string tabName, string? range)
        {
            string rangeWithTab = String.IsNullOrEmpty(range) ? tabName : $"{tabName}|{range}";
            SpreadsheetsResource.ValuesResource.GetRequest request =
                _googleSheetService.Spreadsheets.Values.Get(sheetId, rangeWithTab);
            ValueRange response = request.Execute();
            IList<IList<object>> values = response.Values;
            if (values != null)
            {
                return values;
            }
            return new List<IList<object>>();
        }

        public List<TodoItem> GetTodoItems(string sheetId, string tabName, string? range = null)
        {
            var sheetRows = this.ReadRawData(sheetId, tabName, range);
            var todoList = new List<TodoItem>();
            for (int i = 1; i < sheetRows.Count; i++)
            {
                var row = sheetRows[i];
                string? goal = row[0]?.ToString();
                string? completed = row[5]?.ToString();
                if (String.IsNullOrEmpty(goal) || completed == "TRUE")
                {
                    continue;
                }
                var todoItem = TodoItem.FromGoogleSheetRow(row);
                todoItem.GoogleSheetRowIndex = i + 1;
                todoList.Add(todoItem);
            }
            return todoList;
        }
        // TODO: public void SaveTodoItems(List<TodoItem> items) {}
    }
}
