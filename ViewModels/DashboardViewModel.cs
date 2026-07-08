using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using SIDESA.Net.Core;

namespace SIDESA.Net.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _totalPenduduk = "0";

    [ObservableProperty]
    private string _suratHariIni = "0";

    [ObservableProperty]
    private string _suratBulanIni = "0";

    [ObservableProperty]
    private string _templateAktif = "0";

    public ObservableCollection<Database.RiwayatSurat> RecentLetters { get; } = new();

    public void RefreshData()
    {
        // For now, these are dummy counts since we don't have the exact queries mapped yet,
        // but let's query the DB where possible.
        try
        {
            using var conn = new Microsoft.Data.Sqlite.SqliteConnection(Database.GetConnectionString());
            TotalPenduduk = Dapper.SqlMapper.ExecuteScalar<int>(conn, "SELECT COUNT(*) FROM penduduk").ToString();
            
            var today = System.DateTime.Today.ToString("yyyy-MM-dd");
            var month = System.DateTime.Today.ToString("yyyy-MM-");
            
            SuratHariIni = Dapper.SqlMapper.ExecuteScalar<int>(conn, "SELECT COUNT(*) FROM riwayat_surat WHERE tanggal LIKE @Date", new { Date = today + "%" }).ToString();
            SuratBulanIni = Dapper.SqlMapper.ExecuteScalar<int>(conn, "SELECT COUNT(*) FROM riwayat_surat WHERE tanggal LIKE @Date", new { Date = month + "%" }).ToString();
            TemplateAktif = Database.GetActiveTemplates().Count().ToString();

            RecentLetters.Clear();
            var letters = Database.GetAllRiwayat().Take(10);
            foreach (var l in letters)
            {
                RecentLetters.Add(l);
            }
        }
        catch
        {
            // Ignore errors if DB is locked or not found yet
        }
    }
}
