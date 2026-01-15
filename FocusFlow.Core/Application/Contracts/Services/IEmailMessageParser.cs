using System.Text.Json;
using FocusFlow.Core.Domain.Entities;

namespace FocusFlow.Core.Application.Contracts.Services
{
    public interface IEmailMessageParser<TMessage>
    {
        Email? ParseMessage(TMessage messageData, string messageId, Guid emailAccountId);
    }
}



