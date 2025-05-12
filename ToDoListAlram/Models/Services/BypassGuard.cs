using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ToDoListAlram.Models.Services
{
    internal enum BypassPermissionEnum
    {
        NotAllow,
        CanPause,
        SecondVerify,
        CanClose,
        ShuttingDown
    }

    internal class BypassGuard
    {
        private List<TodoItem>? todoList;

        private BypassPermissionEnum BypassPermission = BypassPermissionEnum.NotAllow;
        private string secondVerifyKey = "";
        public bool CanPause 
        { 
            get 
            {
                return this.BypassPermission == BypassPermissionEnum.CanPause || this.BypassPermission == BypassPermissionEnum.ShuttingDown;
            } 
        }

        public bool CanClose
        {
            get
            {
                return this.BypassPermission == BypassPermissionEnum.CanClose || this.BypassPermission == BypassPermissionEnum.ShuttingDown;
            }
        }

        public bool IsSecondVerifying { get { return this.BypassPermission == BypassPermissionEnum.SecondVerify; } }

        public bool HasUrgentTodoItem
        {
            get
            {
                if (this.todoList == null || this.todoList.Count == 0)
                {
                    return false;
                }
                if (this.todoList.Any(x => 
                    (x.DueDate - DateTime.Now).TotalDays < 1 
                    && !x.IsWaiting))
                {
                    return true;
                }
                if (this.todoList.Any(x => 
                    Convert.ToInt32(x.Importance) > 2 
                    && (x.DueDate - DateTime.Now).TotalDays < 2 
                    && !x.IsWaiting))
                {
                    return true;
                }
                return false;
            }
        }

        private string GetMd5BypassKey(DateTime dateTime)
        {
            string timeString = dateTime.ToString("yyyyMMddHHmm").Substring(0, 11);
            return this.GetMd5BypassKey(timeString);
        }

        private string GetMd5BypassKey(string str)
        {
            using var md5 = MD5.Create();
            byte[] bytes = Encoding.UTF8.GetBytes(str);
            byte[] hash = md5.ComputeHash(bytes);
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

        public string RequestPause(string inputText, int pauseMinute)
        {
            if (pauseMinute < 25)
            {
                return "OK";
            }
            if (pauseMinute == 25)
            {
                if (inputText == "tomato")
                {
                    return "OK";
                }
                else
                {
                    return "進入番茄鐘需要輸入「番茄」的英文";
                }
            }
            if (this.HasUrgentTodoItem)
            {
                return "待辦清單中有緊急事項，無法暫停超過 25 分鐘";
            }

            string bypassKey = this.GetMd5BypassKey(DateTime.Now);
            if (inputText == bypassKey)
            {
                this.BypassPermission = BypassPermissionEnum.CanPause;
                return "OK";
            }
            this.BypassPermission = BypassPermissionEnum.NotAllow;
            return "密碼錯誤 (yyyyMMddHHm 十一碼的 MD5)";
        }

        public bool RequestCloseByShutDown()
        {
            this.BypassPermission = BypassPermissionEnum.ShuttingDown;
            return true;
        }

        public string RequestCloseBySecondVerify(string inputText)
        {
            if (String.IsNullOrEmpty(inputText) 
                || inputText != this.secondVerifyKey
                || this.BypassPermission != BypassPermissionEnum.SecondVerify)
            {
                this.BypassPermission = BypassPermissionEnum.NotAllow;
                return "密碼錯誤，需以一階 MD5 的前半段再做一次 MD5";
            }
            this.BypassPermission = BypassPermissionEnum.CanClose;
            return "OK";
        }

        public string RequestClose(string inputText)
        {
            if (this.BypassPermission == BypassPermissionEnum.CanClose
                || this.BypassPermission == BypassPermissionEnum.ShuttingDown
                || this.BypassPermission == BypassPermissionEnum.SecondVerify)
            {
                return "OK";
            }

            string bypassKey = GetMd5BypassKey(DateTime.Now);
            if (inputText == bypassKey)
            {
                this.BypassPermission = BypassPermissionEnum.SecondVerify;
                this.secondVerifyKey = this.GetMd5BypassKey(bypassKey.Substring(0, bypassKey.Length / 2));
                return "OK";
            }
            this.BypassPermission = BypassPermissionEnum.NotAllow;
            return "密碼錯誤 (yyyyMMddHHm 十一碼的 MD5)";
        }

        public void ResetTodoList(List<TodoItem> todoList)
        {
            this.todoList = todoList;
        }
    }
}
