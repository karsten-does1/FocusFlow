using CommunityToolkit.Mvvm.ComponentModel;

namespace FocusFlow.App.ViewModels.Emails
{
    public partial class AiTaskSuggestionViewModel : ObservableObject
    {
        public AiTaskSuggestionViewModel(
            string title,
            string description,
            string priority,
            string? dueDate,
            string? dueText,
            double confidence,
            string? sourceQuote)
        {
            Title = title;
            Description = description;
            Priority = priority;
            DueDate = dueDate;
            DueText = dueText;
            Confidence = confidence;
            SourceQuote = sourceQuote;

            IsSelected = true; 
        }

        public string Title { get; }
        public string Description { get; }
        public string Priority { get; }
        public string? DueDate { get; }
        public string? DueText { get; }
        public double Confidence { get; }
        public string? SourceQuote { get; }

        [ObservableProperty]
        private bool _isSelected;

        public bool HasDue => !string.IsNullOrWhiteSpace(DueDate) || !string.IsNullOrWhiteSpace(DueText);
        public bool HasSource => !string.IsNullOrWhiteSpace(SourceQuote);

        public string DueDisplay
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(DueDate)) return DueDate!;
                if (!string.IsNullOrWhiteSpace(DueText)) return DueText!;
                return "";
            }
        }
    }
}
