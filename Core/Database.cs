using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;

namespace SIDESA.Net.Core;

public static class Database
{
    private static readonly string DbDir = "database";
    private static readonly string DbPath = Path.Combine(DbDir, "surat.db");
    private static readonly string ConfigPath = "config.json";

    public static string GetConnectionString()
    {
        if (!Directory.Exists(DbDir))
        {
            Directory.CreateDirectory(DbDir);
        }
        return $"Data Source={DbPath}";
    }

    public static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        var builder = new StringBuilder();
        foreach (var b in bytes)
        {
            builder.Append(b.ToString("x2"));
        }
        return builder.ToString();
    }

    public static void InitDb()
    {
        using var connection = new SqliteConnection(GetConnectionString());
        connection.Open();

        // 1. Penduduk Table
        connection.Execute(@"
            CREATE TABLE IF NOT EXISTS penduduk (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                nik TEXT UNIQUE NOT NULL,
                kk TEXT NOT NULL,
                nama TEXT NOT NULL,
                tempat_lahir TEXT,
                tanggal_lahir TEXT,
                jenis_kelamin TEXT,
                agama TEXT,
                status_perkawinan TEXT,
                pekerjaan TEXT,
                alamat TEXT,
                rt TEXT,
                rw TEXT,
                desa TEXT,
                kecamatan TEXT,
                kabupaten TEXT,
                provinsi TEXT,
                kewarganegaraan TEXT DEFAULT 'WNI'
            )");

        // 2. Template Table
        connection.Execute(@"
            CREATE TABLE IF NOT EXISTS template (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                nama_template TEXT NOT NULL,
                jenis_surat TEXT NOT NULL,
                file_template TEXT NOT NULL UNIQUE,
                aktif INTEGER DEFAULT 1
            )");

        // 3. Riwayat Surat Table
        connection.Execute(@"
            CREATE TABLE IF NOT EXISTS riwayat_surat (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                nomor_surat TEXT NOT NULL,
                tanggal TEXT NOT NULL,
                nik TEXT NOT NULL,
                nama TEXT NOT NULL,
                jenis_surat TEXT NOT NULL,
                file_docx TEXT NOT NULL,
                file_pdf TEXT,
                petugas TEXT NOT NULL
            )");

        // 4. User Table
        connection.Execute(@"
            CREATE TABLE IF NOT EXISTS user (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                username TEXT UNIQUE NOT NULL,
                password_hash TEXT NOT NULL,
                role TEXT NOT NULL
            )");

        // Seed Users
        var userCount = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM user");
        if (userCount == 0)
        {
            connection.Execute(
                "INSERT INTO user (username, password_hash, role) VALUES (@Username, @PasswordHash, @Role)",
                new[]
                {
                    new { Username = "admin", PasswordHash = HashPassword("admin123"), Role = "Admin" },
                    new { Username = "petugas", PasswordHash = HashPassword("petugas123"), Role = "Petugas" }
                });
        }

        // Seed Templates
        var templateCount = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM template");
        if (templateCount == 0)
        {
            connection.Execute(
                "INSERT INTO template (nama_template, jenis_surat, file_template, aktif) VALUES (@Nama, @Jenis, @File, @Aktif)",
                new[]
                {
                    new { Nama = "Surat Keterangan Domisili", Jenis = "Domisili", File = "Domisili.docx", Aktif = 1 },
                    new { Nama = "Surat Keterangan Tidak Mampu", Jenis = "SKTM", File = "SKTM.docx", Aktif = 1 },
                    new { Nama = "Surat Keterangan Usaha", Jenis = "Usaha", File = "Usaha.docx", Aktif = 1 },
                    new { Nama = "Surat Keterangan Belum Menikah", Jenis = "Belum Menikah", File = "BelumMenikah.docx", Aktif = 1 }
                });
        }
    }

    // --- Penduduk CRUD ---
    public class Penduduk
    {
        public int Id { get; set; }
        public string Nik { get; set; } = "";
        public string Kk { get; set; } = "";
        public string Nama { get; set; } = "";
        public string Tempat_Lahir { get; set; } = "";
        public string Tanggal_Lahir { get; set; } = "";
        public string Jenis_Kelamin { get; set; } = "";
        public string Agama { get; set; } = "";
        public string Status_Perkawinan { get; set; } = "";
        public string Pekerjaan { get; set; } = "";
        public string Alamat { get; set; } = "";
        public string Rt { get; set; } = "";
        public string Rw { get; set; } = "";
        public string Desa { get; set; } = "";
        public string Kecamatan { get; set; } = "";
        public string Kabupaten { get; set; } = "";
        public string Provinsi { get; set; } = "";
        public string Kewarganegaraan { get; set; } = "WNI";
    }

    public static IEnumerable<Penduduk> GetAllPenduduk(int limit = 100, int offset = 0)
    {
        using var connection = new SqliteConnection(GetConnectionString());
        return connection.Query<Penduduk>(
            "SELECT * FROM penduduk ORDER BY nama ASC LIMIT @Limit OFFSET @Offset",
            new { Limit = limit, Offset = offset });
    }

    public static (bool Success, string Message) AddPenduduk(Penduduk data)
    {
        using var connection = new SqliteConnection(GetConnectionString());
        try
        {
            connection.Execute(@"
                INSERT INTO penduduk (
                    nik, kk, nama, tempat_lahir, tanggal_lahir, jenis_kelamin, agama,
                    status_perkawinan, pekerjaan, alamat, rt, rw, desa, kecamatan,
                    kabupaten, provinsi, kewarganegaraan
                ) VALUES (
                    @Nik, @Kk, @Nama, @Tempat_Lahir, @Tanggal_Lahir, @Jenis_Kelamin, @Agama,
                    @Status_Perkawinan, @Pekerjaan, @Alamat, @Rt, @Rw, @Desa, @Kecamatan,
                    @Kabupaten, @Provinsi, @Kewarganegaraan
                )", data);
            return (true, "Data penduduk berhasil ditambahkan.");
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19) // Constraint Violation
        {
            return (false, $"NIK '{data.Nik}' sudah terdaftar dalam sistem.");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public static (bool Success, string Message) UpdatePenduduk(Penduduk data)
    {
        using var connection = new SqliteConnection(GetConnectionString());
        try
        {
            connection.Execute(@"
                UPDATE penduduk SET
                    nik = @Nik, kk = @Kk, nama = @Nama, tempat_lahir = @Tempat_Lahir,
                    tanggal_lahir = @Tanggal_Lahir, jenis_kelamin = @Jenis_Kelamin, agama = @Agama,
                    status_perkawinan = @Status_Perkawinan, pekerjaan = @Pekerjaan, alamat = @Alamat,
                    rt = @Rt, rw = @Rw, desa = @Desa, kecamatan = @Kecamatan,
                    kabupaten = @Kabupaten, provinsi = @Provinsi, kewarganegaraan = @Kewarganegaraan
                WHERE id = @Id
                ", data);
            return (true, "Data penduduk berhasil diperbarui.");
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            return (false, $"NIK '{data.Nik}' sudah digunakan oleh penduduk lain.");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public static void DeletePenduduk(int id)
    {
        using var connection = new SqliteConnection(GetConnectionString());
        connection.Execute("DELETE FROM penduduk WHERE id = @Id", new { Id = id });
    }

    public static IEnumerable<Penduduk> SearchPenduduk(string query, int limit = 100)
    {
        using var connection = new SqliteConnection(GetConnectionString());
        var searchStr = $"%{query}%";
        return connection.Query<Penduduk>(@"
            SELECT * FROM penduduk
            WHERE nik LIKE @Query OR kk LIKE @Query OR nama LIKE @Query
            ORDER BY nama ASC LIMIT @Limit
            ", new { Query = searchStr, Limit = limit });
    }

    // --- Template CRUD ---
    public class Template
    {
        public int Id { get; set; }
        public string Nama_Template { get; set; } = "";
        public string Jenis_Surat { get; set; } = "";
        public string File_Template { get; set; } = "";
        public int Aktif { get; set; } = 1;
    }

    public static IEnumerable<Template> GetAllTemplates()
    {
        using var connection = new SqliteConnection(GetConnectionString());
        return connection.Query<Template>("SELECT * FROM template ORDER BY nama_template ASC");
    }

    public static IEnumerable<Template> GetActiveTemplates()
    {
        using var connection = new SqliteConnection(GetConnectionString());
        return connection.Query<Template>("SELECT * FROM template WHERE aktif = 1 ORDER BY nama_template ASC");
    }

    public static (bool Success, string Message) AddTemplate(Template template)
    {
        using var connection = new SqliteConnection(GetConnectionString());
        try
        {
            connection.Execute(
                "INSERT INTO template (nama_template, jenis_surat, file_template, aktif) VALUES (@Nama_Template, @Jenis_Surat, @File_Template, @Aktif)",
                template);
            return (true, "Template berhasil ditambahkan.");
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            return (false, $"File template '{template.File_Template}' sudah ada.");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public static void DeleteTemplate(int id)
    {
        using var connection = new SqliteConnection(GetConnectionString());
        connection.Execute("DELETE FROM template WHERE id = @Id", new { Id = id });
    }

    public static void ToggleTemplateStatus(int id, int status)
    {
        using var connection = new SqliteConnection(GetConnectionString());
        connection.Execute("UPDATE template SET aktif = @Status WHERE id = @Id", new { Id = id, Status = status });
    }

    // --- Riwayat Surat CRUD ---
    public class RiwayatSurat
    {
        public int Id { get; set; }
        public string Nomor_Surat { get; set; } = "";
        public string Tanggal { get; set; } = "";
        public string Nik { get; set; } = "";
        public string Nama { get; set; } = "";
        public string Jenis_Surat { get; set; } = "";
        public string File_Docx { get; set; } = "";
        public string File_Pdf { get; set; } = "";
        public string Petugas { get; set; } = "";
    }

    public static void AddRiwayat(RiwayatSurat riwayat)
    {
        using var connection = new SqliteConnection(GetConnectionString());
        connection.Execute(@"
            INSERT INTO riwayat_surat (nomor_surat, tanggal, nik, nama, jenis_surat, file_docx, file_pdf, petugas)
            VALUES (@Nomor_Surat, @Tanggal, @Nik, @Nama, @Jenis_Surat, @File_Docx, @File_Pdf, @Petugas)
            ", riwayat);
    }

    public static IEnumerable<RiwayatSurat> GetAllRiwayat()
    {
        using var connection = new SqliteConnection(GetConnectionString());
        return connection.Query<RiwayatSurat>("SELECT * FROM riwayat_surat ORDER BY id DESC");
    }

    // --- User Auth ---
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = "";
        public string Role { get; set; } = "";
    }

    public static User? AuthenticateUser(string username, string password)
    {
        using var connection = new SqliteConnection(GetConnectionString());
        return connection.QueryFirstOrDefault<User>(
            "SELECT id, username, role FROM user WHERE username = @Username AND password_hash = @PasswordHash",
            new { Username = username, PasswordHash = HashPassword(password) });
    }

    // --- Configuration Helper ---
    public static Dictionary<string, JsonElement> LoadConfig()
    {
        if (!File.Exists(ConfigPath))
        {
            return new Dictionary<string, JsonElement>();
        }
        try
        {
            var json = File.ReadAllText(ConfigPath);
            return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json) ?? new Dictionary<string, JsonElement>();
        }
        catch
        {
            return new Dictionary<string, JsonElement>();
        }
    }
}
