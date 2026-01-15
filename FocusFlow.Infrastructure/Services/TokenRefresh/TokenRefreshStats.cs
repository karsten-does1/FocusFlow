using System;
using System.Collections.Generic;
using System.Linq;

namespace FocusFlow.Infrastructure.Services.TokenRefresh
{
    internal sealed class TokenRefreshStats
    {
        public List<AccountEntry> Refreshed { get; } = new();
        public List<AccountEntry> Skipped { get; } = new();
        public List<AccountEntry> Failed { get; } = new();

        public void LogSummary(Action<string> log)
        {
            log(
                $"Token refresh run finished: " +
                $"refreshed={Refreshed.Count} [{Format(Refreshed)}], " +
                $"skipped={Skipped.Count} [{Format(Skipped)}], " +
                $"failed={Failed.Count} [{Format(Failed)}]"
            );
        }

        private static string Format(IEnumerable<AccountEntry> accounts)
        {
            return string.Join(", ",
                accounts.Select(a => $"{a.Provider}:{a.Email}"));
        }

        internal sealed record AccountEntry(
            Guid AccountId,
            string Email,
            string Provider);
    }
}
