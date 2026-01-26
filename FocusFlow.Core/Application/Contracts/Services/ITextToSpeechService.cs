using System.Threading;
using System.Threading.Tasks;

namespace FocusFlow.Core.Application.Contracts.Services
{
    public interface ITextToSpeechService
    {
        bool IsSpeaking { get; }
        Task SpeakAsync(string text, CancellationToken ct = default);
        void Stop();
    }
}
