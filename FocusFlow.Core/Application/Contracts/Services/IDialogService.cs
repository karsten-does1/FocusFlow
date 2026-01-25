using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FocusFlow.Core.Application.Contracts.Services
{
    public interface IDialogService
    {
        void ShowInfo(string message, string title = "FocusFlow");
        void ShowError(string message, string title = "FocusFlow");
    }
}
