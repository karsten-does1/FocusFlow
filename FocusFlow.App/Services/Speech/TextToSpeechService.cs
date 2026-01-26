using System;
using System.Speech.Synthesis;
using System.Threading;
using System.Threading.Tasks;
using FocusFlow.Core.Application.Contracts.Services;

namespace FocusFlow.App.Services.Speech
{
    public sealed class TextToSpeechService : ITextToSpeechService, IDisposable
    {
        private readonly SpeechSynthesizer _synthesizer;
        private readonly object _sync = new();

        private bool _disposed;
        private volatile bool _isSpeaking;

        public bool IsSpeaking => _isSpeaking;

        public TextToSpeechService()
        {
            _synthesizer = new SpeechSynthesizer();
            _synthesizer.SetOutputToDefaultAudioDevice();

            _synthesizer.SpeakStarted += OnSpeakStarted;
            _synthesizer.SpeakCompleted += OnSpeakCompleted;
        }

        public Task SpeakAsync(string text, CancellationToken ct = default)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(TextToSpeechService));

            var cleaned = (text ?? string.Empty).Trim();
            if (cleaned.Length == 0)
                return Task.CompletedTask;

            var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            EventHandler<SpeakCompletedEventArgs>? onThisUtteranceCompleted = null;
            onThisUtteranceCompleted = (sender, args) =>
            {
                _synthesizer.SpeakCompleted -= onThisUtteranceCompleted;

                if (args.Cancelled) completion.TrySetCanceled();
                else completion.TrySetResult();
            };

            lock (_sync)
            {
                _synthesizer.SpeakAsyncCancelAll();

                _synthesizer.SpeakCompleted += onThisUtteranceCompleted;

                _synthesizer.SpeakAsync(cleaned);
            }

            if (ct.CanBeCanceled)
            {
                ct.Register(Stop);
            }

            return completion.Task;
        }

        public void Stop()
        {
            if (_disposed) return;

            lock (_sync)
            {
                _synthesizer.SpeakAsyncCancelAll();
            }
        }

        private void OnSpeakStarted(object? sender, SpeakStartedEventArgs e)
        {
            _isSpeaking = true;
        }

        private void OnSpeakCompleted(object? sender, SpeakCompletedEventArgs e)
        {
            _isSpeaking = false;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            try { _synthesizer.SpeakAsyncCancelAll(); } catch { }

            _synthesizer.SpeakStarted -= OnSpeakStarted;
            _synthesizer.SpeakCompleted -= OnSpeakCompleted;

            _synthesizer.Dispose();
        }
    }
}
