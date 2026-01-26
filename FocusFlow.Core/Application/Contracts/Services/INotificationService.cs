using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FocusFlow.Core.Application.Contracts.Services
{
    public interface INotificationService
    {
        void Show(string title, string message);
    }
}
