using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SIDESA.Net.Core;

namespace SIDESA.Net.ViewModels;

public partial class SuratViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _nomorSurat = "470/1/DS/2026";

    [ObservableProperty]
    private Database.Penduduk? _selectedPenduduk;

    [ObservableProperty]
    private Database.Template? _selectedTemplate;

    public ObservableCollection<Database.Penduduk> PendudukList { get; } = new();
    public ObservableCollection<Database.Template> TemplateList { get; } = new();

    public SuratViewModel()
    {
        LoadData();
    }

    public void LoadData()
    {
        PendudukList.Clear();
        foreach (var p in Database.GetAllPenduduk())
        {
            PendudukList.Add(p);
        }

        TemplateList.Clear();
        foreach (var t in Database.GetActiveTemplates())
        {
            TemplateList.Add(t);
        }
    }

    [RelayCommand]
    private void BuatSurat()
    {
        if (SelectedPenduduk == null || SelectedTemplate == null)
            return;

        // Generate DOCX
        var (docxPath, errDocx) = DocumentGenerator.GenerateDocument(SelectedTemplate.File_Template, SelectedPenduduk, NomorSurat);
        if (docxPath != null)
        {
            // Convert to PDF
            var (pdfPath, errPdf) = DocumentGenerator.ConvertToPdf(docxPath);

            // Log to Riwayat
            Database.AddRiwayat(new Database.RiwayatSurat
            {
                Nomor_Surat = NomorSurat,
                Tanggal = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Nik = SelectedPenduduk.Nik,
                Nama = SelectedPenduduk.Nama,
                Jenis_Surat = SelectedTemplate.Jenis_Surat,
                File_Docx = docxPath,
                File_Pdf = pdfPath ?? "",
                Petugas = "Admin" // TODO: Get from active session
            });

            // Increment Counter placeholder
        }
    }
}
