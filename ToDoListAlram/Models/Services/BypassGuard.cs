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
        public BypassGuard()
        {

        }

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

        public bool RequestPause(string inputText)
        {
            string bypassKey = this.GetMd5BypassKey(DateTime.Now);
            if (inputText == bypassKey)
            {
                this.BypassPermission = BypassPermissionEnum.CanPause;
                return true;
            }
            this.BypassPermission = BypassPermissionEnum.NotAllow;
            return false;
        }

        public bool RequestCloseByShutDown()
        {
            this.BypassPermission = BypassPermissionEnum.ShuttingDown;
            return true;
        }

        public bool RequestCloseBySecondVerify(string inputText)
        {
            if (String.IsNullOrEmpty(inputText) 
                || inputText != this.secondVerifyKey
                || this.BypassPermission != BypassPermissionEnum.SecondVerify)
            {
                this.BypassPermission = BypassPermissionEnum.NotAllow;
                return false;
            }
            this.BypassPermission = BypassPermissionEnum.CanClose;
            return true;
        }

        public bool RequestClose(string inputText)
        {
            if (this.BypassPermission == BypassPermissionEnum.CanClose
                || this.BypassPermission == BypassPermissionEnum.ShuttingDown
                || this.BypassPermission == BypassPermissionEnum.SecondVerify)
            {
                return true;
            }
            if (String.IsNullOrEmpty(inputText))
            {
                this.BypassPermission = BypassPermissionEnum.NotAllow;
                return false;
            }

            string bypassKey = GetMd5BypassKey(DateTime.Now);
            if (inputText == bypassKey)
            {
                this.BypassPermission = BypassPermissionEnum.SecondVerify;
                this.secondVerifyKey = this.GetMd5BypassKey(bypassKey.Substring(0, bypassKey.Length / 2));
                return true;
            }
            this.BypassPermission = BypassPermissionEnum.NotAllow;
            return false;
        }
    }
}
