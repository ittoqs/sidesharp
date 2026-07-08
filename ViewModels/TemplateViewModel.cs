using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SIDESA.Net.Core;

namespace SIDESA.Net.ViewModels;

public partial class TemplateViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<Database.Template> _templates = new();

    [ObservableProperty]
    private Database.Template? _selectedTemplate;

    public void LoadData()
    {
        Templates.Clear();
        var data = Database.GetAllTemplates();
        foreach (var t in data)
        {
            Templates.Add(t);
        }
    }

    [RelayCommand]
    private void ToggleStatus(Database.Template? template)
    {
        if (template != null)
        {
            int newStatus = template.Aktif == 1 ? 0 : 1;
            Database.ToggleTemplateStatus(template.Id, newStatus);
            LoadData();
        }
    }

    [RelayCommand]
    private void DeleteTemplate(Database.Template? template)
    {
        if (template != null)
        {
            Database.DeleteTemplate(template.Id);
            LoadData();
        }
    }
}
