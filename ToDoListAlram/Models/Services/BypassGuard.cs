﻿using Microsoft.Win32;
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

    public enum BypassReasonEnum
    {
        ShortPause,
        TomatoClock,
        LongPause,
        ConsumeReward,
    }

    public class BypassRequest
    {
        public BypassReasonEnum Reason { get; private set; }
        public string InputText { get; set; } = "";
        public int PauseMinute { get; set; }

        public BypassRequest(string text, int minute, bool isConsumeReward)
        {
            this.PauseMinute = minute;
            this.InputText = text;
            if (isConsumeReward)
            {
                this.Reason = BypassReasonEnum.ConsumeReward;
            }
            else if (minute <= 10)
            {
                this.Reason = BypassReasonEnum.ShortPause;
            }
            else if (minute == 25)
            {
                this.Reason = BypassReasonEnum.TomatoClock;
            }
            else if (minute > 25)
            {
                this.Reason = BypassReasonEnum.LongPause;
            }
            else
            {
                throw new ArgumentException($"無法建立 Bypass Request text:{text}, minute:{minute}, isConsumeReward:{isConsumeReward}");
            }
        }
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

        public string RequestPause(BypassRequest request)
        {
            string verifyResult = request.Reason switch
            {
                BypassReasonEnum.ShortPause => "OK",
                BypassReasonEnum.TomatoClock => this.VerifyTomatoClock(request),
                BypassReasonEnum.LongPause => this.VerifyLongPause(request),
                BypassReasonEnum.ConsumeReward => this.VerifyConsumeReward(request),
                _ => "未知的請求類型"
            };
            this.BypassPermission = verifyResult == "OK" 
                ? BypassPermissionEnum.CanPause 
                : BypassPermissionEnum.NotAllow;
            return verifyResult;
        }

        private string VerifyTomatoClock(BypassRequest request)
        {
            if (request.InputText.ToLower() == "tomato")
            {
                return "OK";
            }
            return "請輸入番茄的英文啟動番茄鐘";
        }

        private string VerifyLongPause(BypassRequest request)
        {
            if (this.HasUrgentTodoItem)
            {
                return "待辦清單中有緊急事項，無法暫停超過 25 分鐘";
            }
            return this.VerifyMd5BypassKey(request.InputText);
        }

        private string VerifyConsumeReward(BypassRequest request)
        {
            if (request.PauseMinute < 30)
            {
                return "銷點至少需要銷 30 分鐘 (15點)";
            }
            if (String.IsNullOrEmpty(request.InputText))
            {
                return "珍惜點數！銷點暫停也要輸入 MD5 密碼";
            }
            return this.VerifyMd5BypassKey(request.InputText);
        }

        private string VerifyMd5BypassKey(string inputText)
        {
            string bypassKey = this.GetMd5BypassKey(DateTime.Now);
            if (inputText == bypassKey)
            {
                return "OK";
            }
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
