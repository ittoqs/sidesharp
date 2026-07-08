using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SIDESA.Net.Core;

namespace SIDESA.Net.ViewModels;

public partial class PendudukViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _searchQuery = "";

    public ObservableCollection<Database.Penduduk> PendudukList { get; } = new();

    [ObservableProperty]
    private Database.Penduduk? _selectedPenduduk;

    public PendudukViewModel()
    {
        // Fire and forget
        _ = RefreshListAsync();
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task SearchAsync()
    {
        await RefreshListAsync();
    }

    public void RefreshList()
    {
        _ = RefreshListAsync();
    }

    public async System.Threading.Tasks.Task RefreshListAsync()
    {
        var data = string.IsNullOrWhiteSpace(SearchQuery) 
            ? await Database.GetAllPendudukAsync()
            : await Database.SearchPendudukAsync(SearchQuery);
            
        // Assuming Avalonia handles ObservableCollection on UI Thread properly if modified entirely
        // Alternatively we can use Dispatcher, but doing clear & add works if dispatched
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            PendudukList.Clear();
            foreach (var item in data)
            {
                PendudukList.Add(item);
            }
        });
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task AddNewAsync()
    {
        // TODO: Show Add Dialog
        // For demonstration we'll just add a dummy to DB to test DataGrid updating
        var dummy = new Database.Penduduk { Nik = System.DateTime.Now.Ticks.ToString().Substring(0,16), Nama = "Baru Tambah", Kk = "1234567890123456" };

        // Execute synchronously in thread pool to prevent blocking if needed, but since it's just local sqlite, it's fast
        await System.Threading.Tasks.Task.Run(() => Database.AddPenduduk(dummy));

        await RefreshListAsync();
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task DeleteSelectedAsync()
    {
        if (SelectedPenduduk != null)
        {
            await System.Threading.Tasks.Task.Run(() => Database.DeletePenduduk(SelectedPenduduk.Id));
            await RefreshListAsync();
        }
    }
}
