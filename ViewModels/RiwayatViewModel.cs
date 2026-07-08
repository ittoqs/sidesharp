using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SIDESA.Net.Core;

namespace SIDESA.Net.ViewModels;

public partial class RiwayatViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<Database.RiwayatSurat> _riwayatList = new();

    [ObservableProperty]
    private Database.RiwayatSurat? _selectedRiwayat;

    [RelayCommand]
    public void LoadData()
    {
        RiwayatList.Clear();
        var data = Database.GetAllRiwayat();
        foreach (var r in data)
        {
            RiwayatList.Add(r);
        }
    }

    [RelayCommand]
    private void BukaFolderPdf()
    {
        var outputDir = Path.Combine(Directory.GetCurrentDirectory(), "output");
        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = outputDir,
                UseShellExecute = true,
                Verb = "open"
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error opening directory: " + ex.Message);
        }
    }

    [RelayCommand]
    private void BukaFilePdf(Database.RiwayatSurat? riwayat)
    {
        if (riwayat != null && !string.IsNullOrEmpty(riwayat.File_Pdf))
        {
            var pdfPath = Path.Combine(Directory.GetCurrentDirectory(), "output", riwayat.File_Pdf);
            if (File.Exists(pdfPath))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = pdfPath,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error opening PDF: " + ex.Message);
                }
            }
        }
    }
}
