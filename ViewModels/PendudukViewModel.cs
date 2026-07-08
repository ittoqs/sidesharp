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
        RefreshList();
    }

    [RelayCommand]
    private void Search()
    {
        RefreshList();
    }

    public void RefreshList()
    {
        PendudukList.Clear();
        var data = string.IsNullOrWhiteSpace(SearchQuery) 
            ? Database.GetAllPenduduk() 
            : Database.SearchPenduduk(SearchQuery);
            
        foreach (var item in data)
        {
            PendudukList.Add(item);
        }
    }

    [RelayCommand]
    private void AddNew()
    {
        // TODO: Show Add Dialog
        // For demonstration we'll just add a dummy to DB to test DataGrid updating
        var dummy = new Database.Penduduk { Nik = System.DateTime.Now.Ticks.ToString().Substring(0,16), Nama = "Baru Tambah", Kk = "1234567890123456" };
        Database.AddPenduduk(dummy);
        RefreshList();
    }

    [RelayCommand]
    private void DeleteSelected()
    {
        if (SelectedPenduduk != null)
        {
            Database.DeletePenduduk(SelectedPenduduk.Id);
            RefreshList();
        }
    }
}
