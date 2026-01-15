using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FocusFlow.Core.Application.Contracts.Services
{
    public interface IAiService
    {
        Task<(string Summary, int Priority, string Category, string Action)> AnalyzeEmailAsync(string subject, string body);
    }
}
