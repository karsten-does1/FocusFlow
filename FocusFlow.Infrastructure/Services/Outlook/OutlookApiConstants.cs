using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FocusFlow.Infrastructure.Services.Outlook
{
    public static class OutlookApiConstants
    {
        public const string MessagesEndpoint =
            "me/mailFolders('Inbox')/messages" +
            "?$top={0}" +
            "&$select=id,receivedDateTime,subject,from,body" +
            "&$orderby=receivedDateTime desc";

        public const int DefaultMaxCount = 20;
        public const int MaxMaxCount = 500;
        public const int MinMaxCount = 1;
    }
}




