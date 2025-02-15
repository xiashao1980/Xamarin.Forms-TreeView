﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace XamarinFormsTreeView.TreeViewEx
{
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        public INavigation Navigation { get; set; }

        //		string Md5Hash (string value)
        //		{
        //			var hash = MD5.Create ();
        //			var data = hash.ComputeHash (Encoding.UTF8.GetBytes (value));
        //			var builder = new StringBuilder ();
        //			for (var i = 0; i < data.Length; i++) {
        //				builder.Append (data [i].ToString ("x2"));
        //			}
        //			return builder.ToString ();
        //		}

        #region INotifyPropertyChanged implementation

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        internal virtual Task Initialize(params object[] args)
        {
            return Task.FromResult(0);
        }

        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged == null) return;
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        protected void SetObservableProperty<T>(
            ref T field,
            T value,
            [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return;
            field = value;
            OnPropertyChanged(propertyName);
        }
    }

    public abstract class BaseViewModelWithWaitResult<T> : BaseViewModel
    {
        private TaskCompletionSource<T> _taskCompletionSource;

        public Task<T> WaitResult()
        {
            _taskCompletionSource = new TaskCompletionSource<T>();
            return _taskCompletionSource.Task;
        }
    }
}
