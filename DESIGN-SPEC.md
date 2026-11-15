# BoWeather Control Center — Spesifikasi Desain

Referensi visual dan perilaku: `control-center.html` (bisa dibuka langsung di browser). Spesifikasi ini ditulis netral framework, jadi agent bisa menerjemahkannya ke WPF, WinUI, Qt, Electron, atau lainnya.

## 1. Struktur (atas ke bawah)

Lebar panel 392 px, padding 18 px, jarak antarbagian 16 px, radius 22 px.

| # | Bagian | Isi | Tinggi |
|---|---|---|---|
| 0 | Chip tema aktif (di luar panel, di atasnya, rata tengah) | Ikon tema + nama tema + chevron | 40 |
| 1 | Header | Judul "BoWeather Control Center", tombol mode (bulan/matahari), tombol tutup (X) | 32 |
| 2 | Grid tile 3 kolom x 2 baris, gap 8 | Sakura, Salju, Daun Gugur, Hujan, Kunang-kunang, Jeda | 68 per tile |
| 3 | Dua slider | Kekuatan Angin, Kepadatan Partikel | 24 label + 6 + 26 track, jarak antar slider 14 |
| 4 | Kartu toggle (padding 4, radius 18) | Window Physics, Auto-Pause Game, Wallpaper 8 Waktu | 58 per baris |
| 5 | Tombol aksi grid 2 x 2, gap 8 | Terapkan Wallpaper, Pilih Gambar…, Bahasa: ID, Keluar | 44 |

## 2. Token warna

| Token | Dark | Light |
|---|---|---|
| panel | `rgba(28,29,32,.94)` | `rgba(255,255,255,.88)` |
| border | `rgba(255,255,255,.10)` | `rgba(0,0,0,.08)` |
| text | `#FFFFFF` | `#16181D` |
| sub (teks sekunder) | `rgba(255,255,255,.70)` | `rgba(22,24,29,.68)` |
| value (label nilai slider) | `#8CC2FF` | `#0A5BB8` |
| tile | `rgba(255,255,255,.08)` | `rgba(0,0,0,.05)` |
| card | `rgba(255,255,255,.06)` | `rgba(0,0,0,.04)` |
| iconbox | `rgba(255,255,255,.10)` | `rgba(0,0,0,.06)` |
| divider | `rgba(255,255,255,.08)` | `rgba(0,0,0,.08)` |
| track slider | `rgba(255,255,255,.16)` | `rgba(0,0,0,.13)` |
| switch mati | `rgba(255,255,255,.22)` | `rgba(0,0,0,.22)` |
| tombol ghost | `rgba(255,255,255,.10)` | `rgba(0,0,0,.06)` |
| tombol Keluar (bg / teks) | `rgba(255,99,88,.20)` / `#FFB9B2` | `rgba(220,53,40,.12)` / `#B3261E` |
| chip (bg / teks) | `rgba(28,29,32,.92)` / `#FFFFFF` | `rgba(255,255,255,.92)` / `#16181D` |
| accent (kedua mode) | `#0A6FD6` | `#0A6FD6` |
| on-accent | `#FFFFFF` | `#FFFFFF` |

Pilihan aksen lain (semua lolos kontras dengan teks putih): `#B83A7E`, `#A8570A`, `#1F7A4D`.

Panel memakai efek acrylic atau blur (`backdrop-filter: blur(24px)`). Di Windows 11, gunakan Mica atau Acrylic bawaan, dan tetap pakai warna `panel` di atas sebagai tint.

## 3. Tipografi

Font Inter. Fallback: Segoe UI Variable, lalu sans-serif sistem.

| Elemen | Ukuran / berat |
|---|---|
| Judul panel | 17 / 700 |
| Judul baris dan label slider | 14 / 600 |
| Subjudul baris | 12 / 400 |
| Label tile | 12 / 600 |
| Nilai slider | 13 / 600 |
| Tombol aksi | 13 / 600 |
| Chip | 14 / 600 |

Semua label harus `nowrap` dan tidak boleh terpotong. Tile selebar sekitar 113 px cukup untuk "Kunang-kunang" di 12 px.

## 4. Komponen dan state

**Tile tema (5) dan tile Jeda**
- Tata letak vertikal: ikon 26 px di atas, label di bawah, jarak 6.
- Hanya satu tema yang terpilih. Tile terpilih berlatar `accent` dengan teks `on-accent`.
- Tile Jeda berdiri sendiri (toggle): saat aktif, ikon berubah dari pause ke play, label berubah dari "Jeda" ke "Lanjut", dan latarnya `accent`.

**Slider**
- Track tinggi 8 px, bagian terisi berwarna `accent`, thumb putih bulat 22 px dengan bayangan.
- Label nilai di kanan: Kekuatan Angin → Tenang (<34), Sepoi-sepoi (34–66), Kencang (≥67). Kepadatan Partikel → Jarang, Sedang, Padat dengan ambang yang sama.
- Nilai 0–100.

**Baris toggle**
- Susunan: kotak ikon 36 px, lalu judul dan subjudul, lalu switch 44x26 di kanan.
- Switch aktif berwarna `accent`, knob putih 20 px geser dari kiri ke kanan.
- Tiga toggle: Window Physics, Auto-Pause Game, Wallpaper 8 Waktu.

**Tombol aksi**
- Terapkan Wallpaper (utama, berlatar `accent`): setelah diklik labelnya jadi "Diterapkan"; berubah lagi ke "Terapkan Wallpaper" saat tema diganti.
- Pilih Gambar… (ghost): membuka dialog pilih file.
- Bahasa: ID (ghost): klik untuk bergantian ID dan EN.
- Keluar (merah lembut).

**Mode gelap/terang**
- Tombol di header menampilkan ikon bulan saat mode gelap dan ikon matahari saat mode terang. Klik untuk berpindah.
- Disarankan mengikuti tema sistem (`prefers-color-scheme` atau pengaturan tema Windows) sebagai nilai awal, lalu simpan pilihan pengguna.

## 5. Aksesibilitas

- Semua kontrol berupa tombol sungguhan (`button`) dengan `aria-pressed` untuk yang bisa menyala/mati.
- Tombol berikon saja (mode, tutup) wajib punya `aria-label`.
- Fokus keyboard terlihat: outline 2 px (`#FFFFFF` di dark, `#0A6FD6` di light).
- Slider memakai input range asli sehingga bisa digeser dengan keyboard.

## 6. Hook ke aplikasi

Di `control-center.html`, objek `app` berisi fungsi kosong yang tinggal diisi dengan logika aplikasi sungguhan:

`setTheme`, `setPaused`, `setPhysics`, `setAutoPause`, `setAutoTime`, `setWind`, `setDensity`, `applyWallpaper`, `pickImage`, `setLanguage`, `quit`, `closePanel`.

Nilai awal yang dipakai di desain: tema Salju, semua toggle aktif, slider di 50, bahasa ID, mode gelap.

## 7. Catatan penerjemahan ke framework native

- Token warna di bagian 2 dijadikan satu kamus sumber daya per mode (misalnya `ResourceDictionary` Dark dan Light di WPF/WinUI) lalu diganti saat pengguna menekan tombol mode.
- Ikon di HTML berupa path SVG (viewBox 24, stroke 1.8, ujung bulat). Gunakan path yang sama sebagai `Path` atau `Geometry` di XAML, atau pakai Segoe Fluent Icons yang paling mirip.
- Jarak, radius, dan tinggi di atas dalam piksel logis (DIP).
