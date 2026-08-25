# 🤖 AGENT INSTRUCTIONS — Post Bug Fix Verification Protocol

> **CATATAN UNTUK AI AGENT:** File ini WAJIB dibaca dan diikuti SETIAP KALI kamu selesai melakukan perubahan kode (bug fix, refactor, atau penambahan fitur), SEBELUM kamu menyatakan pekerjaan selesai kepada user. Jangan skip langkah manapun kecuali langkah tersebut memang tidak relevan dengan perubahan yang dilakukan — dan jika di-skip, WAJIB jelaskan alasannya secara eksplisit di ringkasan akhir.

---

## 🎯 Tujuan

Mencegah pola umum yang sering terjadi pada AI coding agent:
- Bug "ketutup" tapi memunculkan bug baru di tempat lain
- Fix hanya menyelesaikan gejala, bukan akar masalah
- Perubahan tidak diuji ulang secara menyeluruh
- Kode jadi tidak konsisten dengan gaya/arsitektur project
- Side effect tidak terdeteksi karena agent hanya fokus di file yang diedit

---

## 📋 CHECKLIST WAJIB SETELAH BUG FIX

### 1. Root Cause Verification (Verifikasi Akar Masalah)
- [ ] Jelaskan ulang **akar penyebab bug** dengan kalimat sendiri (bukan hanya gejalanya).
- [ ] Pastikan fix menyelesaikan **penyebab**, bukan sekadar menutup gejala (misalnya jangan hanya menambahkan `try/catch` tanpa menangani kondisi yang menyebabkan error).
- [ ] Jika ada lebih dari satu kemungkinan penyebab, sebutkan mana yang dipilih dan kenapa.

### 2. Scope Impact Analysis (Analisis Dampak Perubahan)
- [ ] Cari semua **pemanggil (callers)** dari fungsi/method/class yang diubah — gunakan pencarian referensi di seluruh codebase, jangan hanya file yang sedang dibuka.
- [ ] Cari semua **dependency** yang mungkin terpengaruh (import/export, shared state, config global, database schema, API contract).
- [ ] Jika perubahan menyentuh **interface publik** (API endpoint, function signature, tipe data, event), cek apakah ada konsumen lain yang akan patah (breaking change).
- [ ] Cek apakah perubahan memengaruhi **file lain yang belum disentuh** (misalnya perubahan tipe data di satu file bisa merusak type-checking di file lain).

### 3. Regression Check (Cek Regresi)
- [ ] Jalankan **seluruh test suite** yang ada di project (unit test, integration test, e2e test) — bukan hanya test untuk bagian yang diperbaiki.
- [ ] Jika tidak ada test otomatis untuk area yang diubah, **buat test baru** yang mereplikasi bug tersebut (test harus GAGAL sebelum fix, dan LULUS setelah fix).
- [ ] Pastikan tidak ada test yang sebelumnya lulus jadi gagal (no new failing tests).
- [ ] Jika project tidak punya test runner otomatis, lakukan **manual trace/simulasi** step-by-step dari input sampai output untuk skenario utama + skenario edge case.

### 4. Edge Case & Negative Testing
- [ ] Uji dengan input kosong/null/undefined.
- [ ] Uji dengan input di batas ekstrem (angka sangat besar/kecil, string sangat panjang, array kosong).
- [ ] Uji dengan input tidak valid/salah format (harus gagal dengan aman, bukan crash).
- [ ] Uji kondisi race condition / concurrency jika kode bersifat async atau multi-thread.
- [ ] Uji ulang skenario yang **awalnya memicu bug** persis seperti laporan user, pastikan benar-benar hilang.

### 5. Code Consistency Check (Konsistensi Kode)
- [ ] Pastikan gaya penulisan (naming convention, indentation, struktur folder) **konsisten** dengan kode sekitarnya, bukan menambahkan pola baru sendiri.
- [ ] Cek apakah ada **duplikasi logika** yang sebaiknya di-refactor jadi satu fungsi/helper.
- [ ] Pastikan tidak ada kode yang di-comment-out/dead code yang tertinggal dari proses debugging.
- [ ] Hapus semua `console.log`, `print()`, atau debug statement sementara yang ditambahkan selama proses fix.

### 6. Security & Safety Review
- [ ] Cek apakah fix membuka celah keamanan baru (SQL injection, XSS, exposed secret/API key, path traversal, dll).
- [ ] Cek validasi input/output tetap terjaga (tidak dilonggarkan hanya demi "membuat fix bekerja").
- [ ] Jika fix menyentuh autentikasi/otorisasi/permission, double-check tidak ada bypass yang tidak disengaja.

### 7. Performance Sanity Check
- [ ] Pastikan fix tidak menambahkan operasi yang berat secara tidak perlu (misalnya loop bersarang, query database di dalam loop, N+1 query).
- [ ] Jika perubahan menyentuh kode yang sering dipanggil (hot path), pertimbangkan dampaknya ke performa.

### 8. Documentation & Traceability
- [ ] Update komentar/docstring jika logika berubah.
- [ ] Update dokumentasi eksternal (README, API docs, changelog) jika relevan.
- [ ] Tulis **commit message** yang jelas: apa bug-nya, apa root cause-nya, apa solusinya (format: `fix: <deskripsi singkat> — root cause: <...>`).

### 9. Build & Lint Verification
- [ ] Jalankan build/compile project, pastikan **tidak ada error maupun warning baru**.
- [ ] Jalankan linter/formatter project (eslint, prettier, black, dll) dan pastikan lulus.
- [ ] Jalankan type-checker jika project menggunakan bahasa/tooling bertipe statis (TypeScript, mypy, dll).

### 10. Final Self-Review (Double Check Terakhir)
- [ ] Baca ulang **seluruh diff** perubahan dari awal sampai akhir seolah-olah kamu adalah reviewer lain, bukan penulis kode ini.
- [ ] Tanyakan pada diri sendiri: *"Kalau saya user, apakah saya percaya bug ini benar-benar sudah selesai, atau ini baru asumsi?"*
- [ ] Pastikan tidak ada perubahan **di luar scope** yang tidak diminta (jangan mengubah bagian lain yang tidak berkaitan dengan bug ini kecuali memang diperlukan, dan jika terpaksa, sebutkan secara eksplisit).

---

## 📝 FORMAT LAPORAN WAJIB SETELAH SELESAI

Setelah menyelesaikan checklist di atas, AI agent **WAJIB** menyampaikan ringkasan ke user dengan format berikut (jangan hanya bilang "sudah diperbaiki"):

```
## Ringkasan Bug Fix

**Root cause:** <penjelasan akar masalah>

**Perubahan yang dilakukan:**
- <file 1>: <apa yang diubah dan kenapa>
- <file 2>: <apa yang diubah dan kenapa>

**Dampak/scope yang dicek:**
- <daftar file/fungsi lain yang ikut diperiksa>

**Testing yang dilakukan:**
- <test yang dijalankan/dibuat, hasilnya>
- <edge case yang diuji>

**Hal yang PERLU diperhatikan user (jika ada):**
- <risiko tersisa, asumsi yang diambil, atau bagian yang belum bisa divalidasi otomatis>

**Item checklist yang di-skip (jika ada):**
- <sebutkan alasan spesifik kenapa suatu langkah tidak relevan>
```

---

## ⚠️ ATURAN KERAS (HARD RULES)

1. **Jangan pernah menyatakan "bug sudah fix" tanpa menjalankan minimal langkah #2 (scope impact) dan #3 (regression check).**
2. **Jangan menambah dependency/library baru** untuk fix sederhana kecuali benar-benar tidak ada cara lain — dan jika terpaksa, jelaskan alasannya.
3. **Jangan mengubah interface publik / kontrak API** tanpa memberi tahu user secara eksplisit bahwa ini adalah breaking change.
4. **Jika tidak yakin bug sudah 100% teratasi**, katakan dengan jujur ke user bahwa ini masih perlu verifikasi manual — jangan mengklaim kepastian yang tidak ada.
5. **Selalu prioritaskan solusi minimal dan aman** dibanding solusi besar/agresif yang menyentuh banyak bagian kode sekaligus.

---

*File ini adalah standing instruction. Agent harus membaca ulang file ini setiap sesi baru dimulai jika belum ada di context, dan mengikutinya secara konsisten tanpa perlu diminta ulang oleh user.*