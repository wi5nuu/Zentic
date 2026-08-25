# Panduan Setup Internal — Zentic Enterprise

Dokumentasi resmi untuk setup, konfigurasi, dan operasional aplikasi internal **Zentic — Expense Requisition System** (Sistem Pengajuan Pengeluaran PT Zentic).

---

## 1. Ringkasan Aplikasi

Zentic adalah sistem pengajuan pengeluaran (expense requisition) berbasis web yang digunakan divisi Finance PT Zentic. Sistem menangani siklus hidup pengajuan dari pembuatan draft, submit, persetujuan bertingkat, hingga pembayaran — dilengkapi deteksi fraud otomatis, manajemen vendor, audit trail immutable, dan 3-way matching (penawaran vendor).

---

## 2. Tech Stack

| Layer | Teknologi | Keterangan |
|---|---|---|
| Framework | **ASP.NET Core MVC 10** (.NET 10) | Pola MVC server-side (Razor Views) |
| Arsitektur | **Clean Architecture** | 5 lapis: Domain → Application → Infrastructure → Web → Tests |
| ORM | **Entity Framework Core 10** | SqlServer provider |
| Database | **SQL Server** (SQLEXPRESS / Express / LocalDB) | `localhost\SQLEXPRESS` |
| Autentikasi | **Cookie Authentication** (custom, bukan ASP.NET Identity) | Login sendiri + BCrypt |
| Otorisasi | **Policy-based** (`CanApproveRequisition`, `CanViewAuditLog`, `CanManageVendors`) | Role-based |
| Hashing Password | **BCrypt.Net-Next 4.0.3** | Work factor 11 |
| Enkripsi | **AES-256-CBC** | IV acak per data; untuk data sensitif vendor (nomor rekening) |
| Validasi | **FluentValidation 11.9** | Validasi di boundary aplikasi |
| UI | **Bootstrap 5.3.2** + **Bootstrap Icons** (CDN) + CSS custom | Dark mode, sidebar |
| Rate Limiting | Built-in ASP.NET (`System.Threading.RateLimiting`) | Proteksi brute-force login: 10 percobaan / 15 menit |
| Testing | **xUnit 2.9** + **Moq** + **FluentAssertions** + EF InMemory | 13 unit test |
| Format Solusi | `.slnx` (XML solution, .NET 10) | Wajib .NET 10 SDK |

> **Catatan arsitektur**: Tidak ada lapisan Service terpisah di `Zentic.Application/Services` — implementasi service berada di `Zentic.Infrastructure/Services`. Aplikasi tidak memakai ASP.NET Identity melainkan cookie auth custom. Enkripsi memakai **AES-256-CBC** (bukan GCM).

---

## 3. Struktur Proyek

```
ZenticEnterprise/
├── Zentic.slnx                          # Solution file (.NET 10)
├── HashTool/                            # (kosong — placeholder)
│
├── Zentic.Domain/                       # Lapisan terdalam — entitas & enum
│   ├── Entities/
│   │   ├── AppUser.cs, Department.cs, ExpenseRequisition.cs
│   │   ├── Vendor.cs, AuditLog.cs, ConflictOfInterestDeclaration.cs
│   │   ├── PriceHistory.cs, RequisitionApprovalHistory.cs
│   │   ├── RequisitionQuotation.cs, BaseEntity.cs
│   └── Enums/Enums.cs
│
├── Zentic.Application/                  # Use cases, DTO, kontrak (interface)
│   ├── DTOs/                            #   ExpenseDto.cs, CommonDto.cs
│   ├── Interfaces/                      #   IAuthService, IExpenseService, dsb.
│   ├── Validators/CreateExpenseValidator.cs
│   └── ViewModels/LoginViewModel.cs
│
├── Zentic.Infrastructure/               # Implementasi teknis
│   ├── Data/AppDbContext.cs             #   EF Core context + seed data
│   ├── Repositories/Repository.cs       #   Generic repository
│   ├── Services/                        #   AuthService, ExpenseService, dsb.
│   └── DependencyInjection.cs           #   Registrasi DI + EnsureCreatedAsync
│
├── Zentic.Web/                          # Lapisan presentasi (ASP.NET MVC)
│   ├── Controllers/                     #   Auth, Home, Expense, Vendor, dll.
│   ├── Views/                           #   Razor views per controller
│   ├── Properties/launchSettings.json
│   ├── wwwroot/css/site.css, js/site.js
│   ├── appsettings.json                 #   Koneksi DB + pengaturan
│   └── Program.cs                       #   Bootstrap aplikasi
│
└── Zentic.Tests/                        # Unit test (13 test)
    └── UnitTests/
        ├── FraudDetectionServiceTests.cs
        └── ExpenseServiceTests.cs
```

---

## 4. Prasyarat

| Kebutuhan | Versi | Keterangan |
|---|---|---|
| **.NET SDK** | 10.0+ | Wajib (mendukung format `.slnx`) |
| **SQL Server** | Express / LocalDB / Developer | Default: `localhost\SQLEXPRESS` |
| **IDE** | Visual Studio 2022+ / Rider / VS Code | Opsional |
| **sqlcmd** | ODBC Driver 18 | Opsional, untuk akses DB via CLI |

Cek instalasi:

```powershell
dotnet --list-sdks
```

Harus ada baris dengan `10.0.x`. Pastikan service SQL Server berjalan:

```powershell
Get-Service | Where-Object { $_.Name -match 'MSSQL' }
# Output yang diharapkan: MSSQL$SQLEXPRESS  Running
```

---

## 5. Menjalankan Aplikasi (Setup Lokal)

### 5.1. Konfigurasi koneksi database

Buka `Zentic.Web/appsettings.json` — sesuaikan `DefaultConnection` dengan instance SQL Server Anda:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=ZenticEnterprise;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

| Parameter | Nilai default | Keterangan |
|---|---|---|
| `Server` | `localhost\SQLEXPRESS` | Ganti sesuai instance Anda (mis. `(localdb)\MSSQLLocalDB`) |
| `Database` | `ZenticEnterprise` | Akan dibuat otomatis saat pertama kali aplikasi dijalankan |

> **Database dibuat otomatis.** Pada startup, `Program.cs` memanggil `DependencyInjection.MigrateAndSeedAsync()` yang menjalankan `EnsureCreatedAsync()` — membuat schema + data seed (5 departemen, 5 user demo, 2 vendor) jika database belum ada.
>
> ⚠️ **PENTING — Tidak ada EF Migrations.** Skema dibuat langsung dari model (`EnsureCreatedAsync`). Jika Anda mengubah entitas domain (menambah kolom/tabel), database harus di-drop dan dibuat ulang (data akan hilang) — atau Anda perlu inisialisasi migrations baru via `dotnet ef migrations add`.

### 5.2. Jalankan

```powershell
# Dari root repositori
dotnet restore
dotnet build Zentic.slnx
dotnet run --project Zentic.Web
```

URL aplikasi (lihat `Zentic.Web/Properties/launchSettings.json`):
- **https://localhost:7081** (HTTPS)
- http://localhost:5081 (HTTP)

Jika dijalankan tanpa IDE, HTTPS redirect membutuhkan dev-certificate:

```powershell
dotnet dev-certs https --trust
```

### 5.3. Akun Demo

Semua akun seed memakai password yang sama:

| Email | Password | Role | Hak Akses |
|---|---|---|---|
| `admin@company.internal` | `P@ssw0rd` | Admin | Semua menu + administrasi |
| `finance.manager@company.internal` | `P@ssw0rd` | Finance | Persetujuan, vendor, report, audit |
| `budi.santoso@company.internal` | `P@ssw0rd` | Staff | Buat & kelola pengajuan sendiri |
| `siti.rahayu@company.internal` | `P@ssw0rd` | Manager | Persetujuan (policy CanApproveRequisition) |
| `auditor@company.internal` | `P@ssw0rd` | Auditor | Audit trail saja |

> **🔴 WAJIB**: Ganti semua password demo sebelum dipakai produksi (menu User Management → Admin).

---

## 6. Konfigurasi (`appsettings.json`)

### 6.1. `AppSettings`

```json
"AppSettings": {
  "CompanyName": "PT Zentic",
  "Division": "Finance",
  "AppVersion": "2.0.0",
  "MultipleQuotationThreshold": 50000000,
  "L2ApprovalThreshold": 100000000
}
```

| Setting | Default | Fungsi |
|---|---|---|
| `MultipleQuotationThreshold` | `50000000` (Rp 50 jt) | Pengajuan **di atas** nilai ini WAJIB punya **minimal 2 quotation vendor** sebelum bisa di-approve (3-way matching). Dibaca oleh `ExpenseService` |
| `L2ApprovalThreshold` | `100000000` (Rp 100 jt) | Disiapkan untuk approval tier L2 (belum di-enforce di kode) |

### 6.2. Enkripsi data sensitif (`Encryption`)

`Zentic.Web/appsettings.Development.json`:

```json
"Encryption": { "Key": "AAAA..." }
```

- Key berisi **base64 kunci AES-256** (32 byte).
- Nomor rekening vendor dienkripsi dengan **AES-256-CBC, IV acak per data** (16 byte pertama ciphertext adalah IV).
- Nilai di `appsettings.Development.json` adalah **placeholder lemah untuk development saja**.

**🔴 WAJIB untuk produksi** — jangan commit key nyata ke git. Gunakan salah satu:

```powershell
# Opsi 1: user-secrets (rekomendasi untuk dev)
dotnet user-secrets init --project Zentic.Web
dotnet user-secrets set "Encryption:Key" "<base64-32-byte>" --project Zentic.Web

# Opsi 2: environment variable
$env:Encryption__Key = "<base64-32-byte>"

# Generate key baru (PowerShell):
[Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
```

> ⚠️ Mengganti key akan membuat data terenkripsi lama (nomor rekening vendor) tidak bisa didekripsi. Ganti key sebelum ada data riil.

---

## 7. Alur Kerja Aplikasi

### 7.1. Siklus Requisition

```
Draft → Submitted → Approved → Paid
            │ (flagged fraud)        │
            └→ UnderReview →─────────┘
Rejected (dari Submitted/UnderReview)   Cancelled (dari selain Approved/Paid)
```

| Status | Siapa | Aksi |
|---|---|---|
| Draft | Owner | Submit, Cancel |
| Submitted | Finance/Admin/Manager | Approve, Reject |
| UnderReview | Finance/Admin/Manager | Approve, Reject (khusus pengajuan ter-flag fraud) |
| Approved | Finance/Admin | Mark as Paid |
| Paid | — | Terminal (tidak bisa diubah) |

### 7.2. Aturan Bisnis yang Diterapkan

1. **Anti self-approval**: approver tidak bisa menyetujui pengajuannya sendiri.
2. **3-Way Matching**: pengajuan dengan vendor dan nilai ≥ `MultipleQuotationThreshold` harus punya ≥ 2 quotation vendor saat approve (`ExpenseService.ApproveAsync`).
3. **Price Benchmarking**: saat approve, sistem mencatat `PriceHistory` otomatis — data ini menjadi benchmark deteksi fraud untuk pengajuan berikutnya.
4. **Deteksi Fraud** (saat create): deviasi harga > 20% dari benchmark, pola strukturisasi (≥ 5 pengajuan nilai besar ke vendor sama dalam 30 hari). Pengajuan ter-flag otomatis masuk status **UnderReview** saat di-submit.
5. **Conflict of Interest**: deklarasi wajib tersimpan dengan SHA-256 signature saat vendor dipilih.
6. **Audit Trail**: semua aksi (create/approve/reject/paid/export/view) tercatat immutable di `AuditLogs` dengan IP + timestamp.

### 7.3. Hak Akses per Halaman

| Halaman | Admin | Finance | Manager | Staff | Auditor |
|---|:---:|:---:|:---:|:---:|:---:|
| Dashboard, Expense | ✔ | ✔ | ✔ | ✔ (data sendiri) | ✔ |
| Approve/Reject | ✔ | ✔ | ✔ | — | — |
| Mark as Paid | ✔ | ✔ | — | — | — |
| Vendor Management | ✔ | ✔ | — | — | — |
| User Management | ✔ | — | — | — | — |
| Departments | ✔ | — | — | — | — |
| Reports + Export CSV | ✔ | ✔ | — | — | — |
| Audit Trail | ✔ | ✔ | — | — | ✔ |
| Export | ✔ | ✔ | — | — | — |

---

## 8. Skema Database

Database **ZenticEnterprise** — 9 tabel (dibuat otomatis):

| Tabel | Isi | Catatan |
|---|---|---|
| `Users` | User + role + password BCrypt | Soft delete, unique Email & EmployeeId |
| `Departments` | Divisi perusahaan | Soft delete |
| `Vendors` | Vendor + rekening bank terenkripsi | Soft delete, unique Code |
| `ExpenseRequisitions` | Pengajuan pengeluaran | Soft delete, unique RequisitionNumber, index Status/RequesterId/CreatedAt/IsFlagged |
| `RequisitionQuotations` | Penawaran vendor per pengajuan | 3-way matching |
| `PriceHistories` | Riwayat harga terbayar | Input fraud benchmarking |
| `RequisitionApprovalHistories` | Timeline persetujuan | Immutable |
| `AuditLogs` | Audit trail | Append-only (tanpa soft delete) |
| `ConflictOfInterestDeclarations` | Deklarasi COI + signature SHA-256 | |

Akses via sqlcmd:

```powershell
sqlcmd -S "localhost\SQLEXPRESS" -d "ZenticEnterprise" -C -Q "SELECT * FROM Users;" -W
```

> Flag `-C` diperlukan untuk ODBC 18 (trust server certificate).

---

## 9. Testing

```powershell
dotnet test Zentic.slnx
```

13 unit test:
- **FraudDetectionServiceTests** (7): benchmarking harga, pola strukturisasi, zero benchmark.
- **ExpenseServiceTests** (6): penegakan minimal 2 quotation, pencatatan price history saat approve, submit flagged → UnderReview, alur mark as paid.

Test memakai **EF InMemory** (database GUID acak per test) — tidak menyentuh SQL Server.

---

## 10. Troubleshooting

| Gejala | Penyebab | Solusi |
|---|---|---|
| `A network-related or instance-specific error...` | Service SQL Server mati / instance beda | Jalankan `Get-Service MSSQL*`; sesuaikan `Server=` di connection string |
| `Sqlcmd: ... certificate chain was issued by an authority that is not trusted` | ODBC 18 butuh trust cert | Tambahkan flag `-C` (trust server certificate) |
| Error login di halaman (anti-forgery) | Cookie `Secure` + akses via HTTP | Buka via **https://localhost:7081** |
| `The type or namespace 'ConfigurationBuilder'` | (hanya untuk pengembang test) | `dotnet restore` / pastikan package `Microsoft.Extensions.Configuration` |
| Halaman 403 saat Access Denied | Role tidak punya akses | Login dengan akun yang tepat |
| Halaman 500 / blank | `Home/Error.cshtml` menampilkan error umum | Jalankan `dotnet run` dengan `ASPNETCORE_ENVIRONMENT=Development` untuk detail error |
| Perubahan entitas tidak terlihat di DB | Tidak ada migrations (EnsureCreated) | Drop DB (`sqlcmd -Q "DROP DATABASE ZenticEnterprise"`) lalu restart aplikasi, ATAU inisialisasi migrations EF Core |
| Login berhasil tapi "Access Denied" | Policy role mismatch | Cek role di DB: `SELECT Email, Role FROM Users` |

---

## 11. Deployment (Catatan Singkat)

1. Set `ASPNETCORE_ENVIRONMENT=Production` (menonaktifkan Developer Exception Page, mengaktifkan HSTS).
2. Set `Encryption:Key` via environment variable / secret manager.
3. Ganti semua password demo.
4. Deploy via IIS / Kestrel + reverse proxy. Contoh publish:

```powershell
dotnet publish Zentic.Web -c Release -o ./publish
```

5. Gunakan instance SQL Server production (bukan Express).

---

## 12. Roadmap / Fitur yang Belum Diimplementasikan

Fitur berikut **belum ada di kode** (direncanakan):
- Email notifikasi (SMTP) untuk approve/reject
- Upload dokumen (invoice, PO, quotation file)
- Multi-currency
- SSO / Azure AD
- Docker support
- Enforce `L2ApprovalThreshold` (approval tier kedua untuk nilai besar)
