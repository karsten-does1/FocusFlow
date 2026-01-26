using System;
using System.Drawing;
using System.Windows.Forms;
using FocusFlow.Core.Application.Contracts.Services;

namespace FocusFlow.App.Services.Notifications
{
    public sealed class TrayNotificationService : INotificationService, IDisposable
    {
        private readonly NotifyIcon _notifyIcon;
        private bool _disposed;

        public TrayNotificationService()
        {
            _notifyIcon = new NotifyIcon
            {
                Visible = true,
                Icon = SystemIcons.Information,
                Text = "FocusFlow"
            };
        }

        public void Show(string title, string message)
        {
            string safeTitle;
            string safeMessage;

            if (string.IsNullOrWhiteSpace(title))
            {
                safeTitle = "FocusFlow";
            }
            else
            {
                safeTitle = title.Trim();
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                safeMessage = string.Empty;
            }
            else
            {
                safeMessage = message.Trim();
            }

            const int MaxTitleLength = 63;
            const int MaxMessageLength = 255;

            string finalTitle;
            string finalMessage;

            if (safeTitle.Length > MaxTitleLength)
            {
                finalTitle = safeTitle.Substring(0, MaxTitleLength);
            }
            else
            {
                finalTitle = safeTitle;
            }

            if (safeMessage.Length > MaxMessageLength)
            {
                finalMessage = safeMessage.Substring(0, MaxMessageLength);
            }
            else
            {
                finalMessage = safeMessage;
            }

            _notifyIcon.BalloonTipTitle = finalTitle;
            _notifyIcon.BalloonTipText = finalMessage;
            _notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
            _notifyIcon.ShowBalloonTip(5000);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }
    }
}
