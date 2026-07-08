using CommunityToolkit.Mvvm.ComponentModel;

namespace SIDESA.Net.ViewModels;

public partial class TentangViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _appVersion = "1.0.0 (Offline Native .NET)";

    [ObservableProperty]
    private string _developerInfo = "Dikembangkan oleh: SIDESA Developer Team";

    [ObservableProperty]
    private string _description = "SI-DESAKU adalah aplikasi administrasi desa berbasis desktop offline yang memungkinkan perangkat desa mencetak surat secara instan dan aman tanpa bergantung pada koneksi internet.";
}
