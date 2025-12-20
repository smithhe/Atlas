namespace Atlas.UI.ViewModels;

public abstract class PageViewModel : ViewModelBase
{
    protected PageViewModel(AiPanelViewModel ai)
    {
        Ai = ai;
    }

    public AiPanelViewModel Ai { get; }
}


