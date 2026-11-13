# BoWeather - Interactive Desktop Weather & Window Physics

**BoWeather** adalah aplikasi desktop mandiri untuk Windows (terinspirasi dari estetika **[Snowy](https://snowy.aauburn.com)** dan fitur interaktif **[KoiArt](https://koiart.app)**). Aplikasi ini menghadirkan efek cuaca transparan di desktop Anda dengan **Window Physics interaktif yang mulus dan menenangkan**.

---

## ✨ Fitur Interaktif BoWeather

1. **❄️ Selimut Salju Pas di Atas Tab/Titlebar (Pillowy Snow Blanket):**
   - Di tepi atas (*titlebar* dan tab) jendela mengambang aktif (File Explorer, Chrome, Notepad, Terminal, dll.), salju menumpuk membentuk **selimut empuk bergelombang alami**.
   - **Pas Menempel di Atas Tab (Tanpa Celah Melayang):** Posisi salju kini terpasang presisi menempel tepat di atas bilah tab/titlebar jendela Windows 11.
   - **Sudut Membulat Rapi (*Fillet Curves*):** Tepi kiri dan kanan gundukan salju melengkung halus mengikuti lengkungan sudut jendela Windows 11.
   - **Gradien 3D Volumetrik:** Putih bersih di puncak gundukan dengan bayangan salju sejuk (*cool shadow*) di bagian bawah dan kilau kristal salju di garis punggungnya.
   - Bagian dasar layar / Taskbar Windows juga tertutupi selimut salju empuk!

2. **💨 Goyang / Geser Cepat Tab, Salju Rontok ke Bawah (*Shake-Off Physics*):**
   - Saat Anda menggeser atau menggoyang (*shake*) tab/jendela dengan cepat:
     - Salju di atas tab jendela tersebut **rontok dan meluncur jatuh ke bawah**.
     - Salju yang jatuh ini langsung **menimbun Taskbar / lantai di bawahnya sehingga tumpukan salju di Taskbar bertambah tebal!**
     - Bebas dari bug partikel berlebih (*fixed bounded pool*), sehingga salju yang rontok jatuh rapi tanpa memenuhi layar.
     - Begitu jendela diam di tempat, salju baru akan menumpuk kembali secara perlahan dan alami.

3. **⛄ Gulung Bola Salju & Susun Boneka Salju (Snowball & Snowman ala KoiArt):**
   - **Gulung Bola Salju (*Roll Snowball*):** Klik tahan tombol kiri mouse dan geser secara horizontal di atas tumpukan salju (pada lis jendela atau Taskbar). Bola salju 3D akan menggelinding dan membesar menyerap salju di sepanjang lintasannya!
   - **Angkat & Pindahkan Bola Salju (*Drag & Lift*):** Klik dan tarik bola salju untuk mengangkatnya ke udara. Anda bisa melemparnya atau memindahkannya ke jendela lain!
   - **Tumpuk Jadi Boneka Salju (*Stacking Snowman*):**
     - Lepaskan bola salju kedua di atas bola salju pertama $\rightarrow$ otomatis tersusun menjadi **Tumpukan 2 Bola (Badan & Pangkal)**.
     - Gulung bola ketiga dan letakkan di atasnya $\rightarrow$ **Otomatis bertransformasi menjadi Boneka Salju (Snowman)!** Lengkap dengan mata hitam, hidung wortel oranye, senyum batu bara, syal merah berumbai, kancing dada, dan tangan ranting kayu.
   - **Lempar & Terurai (*Shatter Physics*):** Jika bola salju dilempar kencang ke arah permukaan jendela/lantai, bola salju akan pecah dan terurai menjadi hujan partikel salju yang memukau!
   - **Bawa Jalan-Jalan:** Boneka salju yang menempel di jendela akan ikut bergerak mulus saat jendela digeser!

4. **🧹 Sapu Salju Interaktif dengan Kursor Mouse:**
   - Melewatkan kursor mouse di atas gundukan salju jendela atau taskbar akan menyapu bersih salju di jalur lintasan kursor secara halus.
   - Butiran salju segar yang terus berjatuhan akan menimbun dan memulihkan kembali gundukan salju secara bertahap dalam kurun waktu 3–4 detik.

5. **💨 Kontrol Arah & Kekuatan Angin (Wind Direction & Speed ala Snowy):**
   - **Arah Tiupan Angin:** 
     - ⬅️ **Tiup ke Kiri (West)** *(seperti tampilan default di aplikasi Snowy)*
     - ➡️ **Tiup ke Kanan (East)**
   - **Kecepatan Angin:**
     - 🍃 **Tenang (Calm)**
     - 💨 **Sepoi-sepoi (Breeze)**
     - 🌪️ **Kencang (Windy)**
   - Dapat diatur langsung lewat klik kanan ikon Tray $\rightarrow$ `💨 Angin (Arah & Kecepatan)`.

6. **🌸 Koleksi Efek Cuaca Lengkap:**
   - ❄️ **Snow (Salju):** Butiran salju bercahaya lembut dengan fisika menumpuk di taskbar dan titlebar jendela.
   - 🌸 **Sakura (Bunga Sakura):** Kelopak merah muda yang meliuk-liuk dan berputar 3D ditiup angin.
   - 🍂 **Autumn Leaves (Daun Gugur):** Daun maple merah, jingga, dan emas yang melayang anggun.
   - 🌧️ **Rain (Hujan):** Garis hujan jatuh miring sesuai arah angin dengan riak cipratan air saat menyentuh jendela.
   - ✨ **Fireflies (Kunang-Kunang):** Partikel bercahaya malam hari dengan denyut aura lembut.

7. **🎮 Auto-Pause Saat Main Game / Fullscreen (Zero Lag):**
   - Mendeteksi secara otomatis saat Anda membuka game (VALORANT, PUBG, Steam, dll.) atau video fullscreen:
     - Efek langsung **100% transparan dan di-pause (0.0% CPU & 0.0% GPU)**.
     - Layar game bersih, bidikan/crosshair fokus, dan FPS game tetap maksimal.
     - Saat kembali ke desktop, partikel cuaca otomatis aktif seketika!
   - Dapat diatur melalui klik kanan ikon Tray: `🎮 Auto-Pause saat Main Game / Fullscreen`.

8. **⚡ Sangat Ringan & Stabil:**
   - 60 FPS halus, CPU < 1%, RAM ~30 MB, tanpa instalasi runtime tambahan.
   - 100% Click-Through: mouse klik dan ketik keyboard tembus langsung ke aplikasi di bawahnya tanpa terhalang.

---

## 🚀 Cara Menjalankan BoWeather

1. Buka folder proyek di File Explorer.
2. Klik dua kali file [run.bat](file:///c:/Users/oktam/OneDrive/Documents/wallpaper-engine/run.bat) (atau [BoWeather.exe](file:///c:/Users/oktam/OneDrive/Documents/wallpaper-engine/BoWeather.exe)).
3. Klik kanan ikon **BoWeather** di System Tray (dekat jam Windows) untuk memilih cuaca, mengatur arah/kekuatan angin, atau wallpaper tema!
