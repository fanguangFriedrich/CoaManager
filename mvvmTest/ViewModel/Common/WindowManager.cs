using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace mvvmTest.ViewModel.Common
{
    /// <summary>
    /// 窗口管理器
    /// </summary>
    public static class WindowManager
    {
        private static Hashtable _RegisterWindow = new Hashtable();

        public static void Register<T>(string key)
        {
            if (!_RegisterWindow.Contains(key))
            {
                _RegisterWindow.Add(key, typeof(T));
            }
        }

        public static void Register(string key, Type t)
        {
            if (!_RegisterWindow.Contains(key))
            {
                _RegisterWindow.Add(key, t);
            }
        }

        public static void Remove(string key)
        {
            if (_RegisterWindow.ContainsKey(key))
            {
                _RegisterWindow.Remove(key);
            }
        }

        public static void Show(string key, object VM)
        {
            if (_RegisterWindow.ContainsKey(key))
            {
                var win = (Window)Activator.CreateInstance((Type)_RegisterWindow[key]);
                win.DataContext = VM;
                win.Show();
            }
        }
    }
}
