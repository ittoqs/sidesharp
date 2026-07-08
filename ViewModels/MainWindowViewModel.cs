using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SIDESA.Net.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isLoggedIn = false;

    public LoginViewModel LoginVm { get; } = new();

    [ObservableProperty]
    private string _currentRouteName = "Dashboard Utama";

    [ObservableProperty]
    private string _userSessionInfo = "";

    [ObservableProperty]
    private ViewModelBase _currentPage;

    // ViewModels for pages
    private readonly DashboardViewModel _dashboardVm = new();
    private readonly PendudukViewModel _pendudukVm = new();
    private readonly SuratViewModel _suratVm = new();
    private readonly TemplateViewModel _templateVm = new();
    private readonly RiwayatViewModel _riwayatVm = new();
    private readonly BackupViewModel _backupVm = new();
    private readonly TentangViewModel _tentangVm = new();
    private readonly PengaturanViewModel _pengaturanVm = new();

    public MainWindowViewModel()
    {
        _currentPage = _dashboardVm;
        LoginVm.LoginSuccess += OnLoginSuccess;
    }

    private void OnLoginSuccess(Core.Database.User user)
    {
        IsLoggedIn = true;
        UserSessionInfo = $"Petugas: {user.Username} (Role: {user.Role})";
        Navigate("Dashboard");
    }

    [RelayCommand]
    private void Navigate(string route)
    {
        switch (route)
        {
            case "Dashboard":
                CurrentPage = _dashboardVm;
                CurrentRouteName = "Dashboard Utama";
                _dashboardVm.RefreshData();
                break;
            case "Penduduk":
                CurrentPage = _pendudukVm;
                CurrentRouteName = "Kelola Data Penduduk";
                _pendudukVm.RefreshList();
                break;
            case "Template":
                CurrentPage = _templateVm;
                CurrentRouteName = "Manajemen Template Surat";
                _templateVm.LoadData();
                break;
            case "BuatSurat":
                CurrentPage = _suratVm;
                CurrentRouteName = "Layanan Operasional Buat Surat";
                _suratVm.LoadData();
                break;
            case "Riwayat":
                CurrentPage = _riwayatVm;
                CurrentRouteName = "Riwayat Pembuatan Surat";
                _riwayatVm.LoadData();
                break;
            case "Backup":
                CurrentPage = _backupVm;
                CurrentRouteName = "Backup & Restore Data";
                _backupVm.StatusMessage = "";
                break;
            case "Tentang":
                CurrentPage = _tentangVm;
                CurrentRouteName = "Tentang Aplikasi";
                break;
            case "Pengaturan":
                CurrentPage = _pengaturanVm;
                CurrentRouteName = "Pengaturan Profil Desa & Sistem";
                _pengaturanVm.LoadConfig();
                break;
            // TODO: Add other routes here later
            default:
                break;
        }
    }
}
