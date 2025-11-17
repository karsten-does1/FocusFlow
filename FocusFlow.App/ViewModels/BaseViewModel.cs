using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace FocusFlow.App.ViewModels
{
    
    public abstract partial class BaseViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string? _errorMessage;

        protected async Task ExecuteAsync(Func<Task> operation, string? errorPrefix = null)
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;
                await operation();
            }
            catch (Exception ex)
            {
                ErrorMessage = errorPrefix != null
                    ? $"{errorPrefix}: {ex.Message}"
                    : ex.Message;
            }
            finally
            {
                IsLoading = false;
            }
        }

        protected async Task<T?> ExecuteAsync<T>(Func<Task<T>> operation, string? errorPrefix = null)
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;
                return await operation();
            }
            catch (Exception ex)
            {
                ErrorMessage = errorPrefix != null
                    ? $"{errorPrefix}: {ex.Message}"
                    : ex.Message;
                return default;
            }
            finally
            {
                IsLoading = false;
            }
        }

        protected void UpdateCollection<T>(ObservableCollection<T> collection, IEnumerable<T> newItems)
        {
            collection.Clear();
            foreach (var item in newItems)
            {
                collection.Add(item);
            }
        }

        protected void NotifyChanged<TMessage>()
            where TMessage : class, new()
        {
            WeakReferenceMessenger.Default.Send(new TMessage());
        }
    }
}

