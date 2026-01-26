using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FocusFlow.App.Messages
{
    public sealed class BriefingSettingsSavedMessage
    {
        public static readonly BriefingSettingsSavedMessage Instance = new();

        private BriefingSettingsSavedMessage() { }
    }
}

