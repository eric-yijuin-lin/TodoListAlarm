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

    public class GoogleSheetService
    {
        private readonly SheetsService _service;

        public GoogleSheetService(SheetsService service)
        {
            _service = service;
        }

        public IList<IList<object>> ReadSheet(string sheetId, string tabName, string? range)
        {
            string rangeWithTab = String.IsNullOrEmpty(range) ? tabName : $"{tabName}|{range}";
            SpreadsheetsResource.ValuesResource.GetRequest request =
                _service.Spreadsheets.Values.Get(sheetId, rangeWithTab);
            ValueRange response = request.Execute();
            IList<IList<object>> values = response.Values;
            if (values != null)
            {
                return values;
            }
            return new List<IList<object>>();
        }
        // TODO: public void SaveTodoItems(List<TodoItem> items) {}
    }
}
