using System.Collections.Generic;

namespace FocusFlow.Core.Application.Contracts.DTOs
{
    public sealed record EmailSyncResultDto(
        int Added,
        int Skipped,
        int Failed,
        IReadOnlyList<string> Errors);
}

