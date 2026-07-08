using System.IO;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SIDESA.Net.Core;

namespace SIDESA.Net.ViewModels;

public partial class PengaturanViewModel : ViewModelBase
{
    [ObservableProperty] private string _desa = "";
    [ObservableProperty] private string _kecamatan = "";
    [ObservableProperty] private string _kabupaten = "";
    [ObservableProperty] private string _provinsi = "";
    [ObservableProperty] private string _alamat = "";
    [ObservableProperty] private string _telepon = "";

    [ObservableProperty] private string _namaKades = "";
    [ObservableProperty] private string _nipKades = "";
    [ObservableProperty] private string _jabatanKades = "";

    public PengaturanViewModel()
    {
        LoadConfig();
    }

    public void LoadConfig()
    {
        var config = Database.LoadConfig();
        
        if (config.TryGetValue("instansi", out var instansi) && instansi.ValueKind == JsonValueKind.Object)
        {
            Desa = GetProp(instansi, "desa", "Kampokku Jaya");
            Kecamatan = GetProp(instansi, "kecamatan", "Pakkampong");
            Kabupaten = GetProp(instansi, "kabupaten", "Limpo");
            Provinsi = GetProp(instansi, "provinsi", "Limpo Toddang");
            Alamat = GetProp(instansi, "alamat", "Jl. Raya Kampong No. 01");
            Telepon = GetProp(instansi, "telepon", "(0123) 456789");
        }

        if (config.TryGetValue("penandatangan", out var kades) && kades.ValueKind == JsonValueKind.Object)
        {
            NamaKades = GetProp(kades, "nama", "H. Baco Tang, S.IP");
            NipKades = GetProp(kades, "nip", "");
            JabatanKades = GetProp(kades, "jabatan", "Kepala Desa");
        }
    }

    private string GetProp(JsonElement el, string key, string def)
    {
        return el.TryGetProperty(key, out var p) ? (p.GetString() ?? def) : def;
    }

    [RelayCommand]
    private void SaveConfig()
    {
        var dict = new System.Collections.Generic.Dictionary<string, object>
        {
            ["instansi"] = new
            {
                desa = Desa,
                kecamatan = Kecamatan,
                kabupaten = Kabupaten,
                provinsi = Provinsi,
                alamat = Alamat,
                telepon = Telepon
            },
            ["penandatangan"] = new
            {
                nama = NamaKades,
                nip = NipKades,
                jabatan = JabatanKades
            }
        };

        var json = JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText("config.json", json);
    }
}
