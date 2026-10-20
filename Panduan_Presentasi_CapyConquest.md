# Panduan Presentasi ETS: Capy Conquest

Durasi sekitar **10 menit**, tanpa slide. Yang ditampilkan hanya game (jalankan `CapyConquest.exe` atau tekan Play di Unity) dan Unity Editor saat membahas kode.

Pembagian: **Farras** membuka (konsep, referensi, tur project), **Mumtaz** demo gameplay dan AI musuh, **Kayla** flocking, boss, Autopilot, dan penutup.

## Persiapan sebelum mulai

- [ ] Ekstrak `CapyConquest_Windows.zip`, jalankan `CapyConquest.exe` sekali untuk memastikan suara keluar. Game terbuka layar penuh; `Alt + Enter` untuk mode jendela.
- [ ] Buka juga project di Unity (untuk bagian kode), scene `Assets/Scenes/CapyConquest.unity`.
- [ ] Hafalkan tombol darurat. Tombol ini tidak tertulis di layar game:

| Tombol | Kapan dipakai |
|---|---|
| `F1` | God mode. Pakai jika pemain kewalahan supaya demo tidak berhenti di layar kalah. |
| `F2` | Kalahkan semua musuh di ruangan, jika waktu presentasi mepet. |
| `F3` | Naik level seketika, untuk memperlihatkan kartu skill kapan saja. |
| `F4` | Lompat ke ruangan berikutnya. Tekan beberapa kali untuk langsung ke boss. |
| `F10` | Autopilot. AI yang memainkan Capy, bisa dipakai saat menjelaskan sambil game tetap berjalan. |

Saran: pilih **MODE: EASY** di menu utama untuk demo, atau nyalakan `F1` sejak awal supaya fokus pada penjelasan AI. Matikan `F1` saat melawan boss jika ingin memperlihatkan layar menang yang "asli".

---

## Bagian 1: Farras (00:00 - 02:30)

### 1.1 Pembukaan (00:00 - 00:40)

**Tampilan layar:** Menu utama game. Kamera bergoyang pelan memperlihatkan Capy berendam di onsen dan semua musuh di belakangnya.

**Narasi:**
"Assalamualaikum. Kami Farras, Mumtaz, dan Kayla, mempresentasikan game ETS kami, **Capy Conquest**. Ceritanya sederhana: Golem King mencuri Golden Yuzu dari pemandian air panas milik Capy. Capy harus menaklukkan lima ruangan dungeon untuk merebutnya kembali. Yang di belakang itu adalah musuh-musuhnya: slime, skeleton, wizard, kelelawar mata, dan Golem King. Desain karakternya kami ambil dari gambar konsep kelompok."

### 1.2 Referensi game komersial (00:40 - 01:30)

**Tampilan layar:** Klik **HOW TO PLAY**.

**Narasi:**
"Kami memakai game mobile **Archero** dari Habby sebagai referensi utama. Archero sangat laku, dan Habby juga pembuat Capybara Go!, game capybara yang sedang populer. Dari Archero kami ambil tiga hal. Pertama, ruangan demi ruangan: pintu terkunci sampai semua musuh kalah. Kedua, Capy menyerang otomatis hanya saat diam, jadi pemain selalu memilih antara menyerang atau menghindar. Ketiga, setiap naik level kita memilih satu dari tiga skill acak, sehingga tiap permainan berbeda. Kami juga menambahkan dash yang membuat kebal sesaat seperti di Hades, dan kristal XP yang tertarik ke pemain seperti di Vampire Survivors.

Di kolom kanan terlihat bagian terpenting untuk mata kuliah ini: setiap musuh memakai teknik AI yang berbeda. Slime memakai steering, skeleton memakai finite state machine dan A*, wizard dan boss memakai behavior tree, kelelawar memakai flocking."

### 1.3 Tur project di Unity (01:30 - 02:30)

**Tampilan layar:** Unity Editor. Tunjukkan panel Project: folder `Assets/Scripts` (AI, Enemies, Player, Level, UI). Lalu Hierarchy scene `CapyConquest`: `Level` berisi `Room0_CapyCamp` sampai `Room5`, `Player`, `GameManager`, `Main Camera`. Klik `Room2`, tunjukkan komponen `RoomController` (daftar Waves dan pengali HP).

**Narasi:**
"Ini project-nya di Unity 6 dengan URP. Semua model kami bangun dari bentuk dasar Unity, dan tekstur, ikon, suara, serta musiknya dibuat dengan kode, jadi tidak ada aset pihak ketiga. Level terdiri dari kemah dan lima ruangan. Setiap ruangan punya `RoomController` yang mengatur gelombang musuh. Contohnya Ruang 2 punya tiga gelombang, dan HP musuhnya 1,6 kali lipat supaya kesulitan naik bertahap. Semua musuh turunan dari `EnemyBase` yang mengurus darah, knockback, dan efek. Tiap jenis musuh hanya mengisi fungsi `Think()` dengan teknik AI-nya masing-masing. Sekarang Mumtaz akan memperlihatkan gameplay-nya."

---

## Bagian 2: Mumtaz (02:30 - 06:00)

### 2.1 Kontrol di Capy Camp (02:30 - 03:00)

**Tampilan layar:** Dari menu klik **PLAY**. Di Capy Camp, gerakkan Capy dengan WASD, tekan `Space` untuk dash.

**Narasi:**
"Kita mulai di Capy Camp yang aman. WASD untuk bergerak, Space untuk dash. Di kiri atas ada darah dan XP, di kanan atas waktu dan jumlah musuh yang dikalahkan. Panah hijau menunjuk ke pintu utara."

### 2.2 Slime dan steering (03:00 - 04:00)

**Tampilan layar:** Masuk Ruang 1. Begitu slime muncul, tekan `V` untuk AI Vision. Diam sebentar agar Capy menyerang.

**Narasi:**
"Begitu masuk, pintu di belakang tertutup dan slime muncul dari portal ungu. Saya tekan V untuk AI Vision. Di atas setiap slime tertulis tekniknya, STEERING. Garis hijau adalah gaya **Seek** menuju Capy, garis oranye **Separation** supaya slime tidak bertumpuk, dan garis biru **Wander** agar geraknya tidak kaku. Setiap kali akan melompat, ketiga gaya ini dijumlahkan dengan bobot untuk menentukan arah lompatan. Slime besar membelah jadi dua saat kalah. Perhatikan juga: saat saya diam, Capy menebas otomatis. Begitu saya bergerak, serangannya berhenti."

### 2.3 Skeleton: FSM dan A* (04:00 - 05:00)

**Tampilan layar:** Ruang 2 dengan AI Vision tetap aktif. Arahkan perhatian ke label skeleton dan garis biru muda di sekitar pilar.

**Narasi:**
"Skeleton memakai **finite state machine**. Lihat labelnya berganti: PATROL, lalu CHASE saat melihat Capy, lalu WINDUP dengan tanda merah di lantai, ATTACK menerjang, dan RECOVER. Saat Capy tidak terlihat langsung karena terhalang pilar, labelnya menjadi CHASE (A* path) dan muncul garis biru muda. Itu jalur **A\*** dari grid ruangan. Tanda X merah adalah sel yang terhalang. Skeleton pemanah juga memakai FSM. Sebelum menembak ia membidik dengan garis laser merah, jadi pemain punya waktu menghindar. Setelah ruangan bersih muncul peti harta yang memberi satu skill gratis."

### 2.4 Wizard: Behavior Tree (05:00 - 06:00)

**Tampilan layar:** Ruang 3. Biarkan label wizard terlihat bergantian. Lalu di Unity buka `Scripts/Enemies/WizardAI.cs` bagian `OnSpawned()` yang berisi susunan pohon.

**Narasi:**
"Wizard memakai **behavior tree** dengan framework dari Praktikum 6. Akarnya Selector yang mencoba cabang dari atas. Kalau sedang merapal, ia menunggu. Kalau Capy terlalu dekat, ia TELEPORT menjauh. Kalau pengikutnya kurang, ia SUMMON slime. Kalau Capy terlihat, ia CAST SPELL. Selain itu ia REPOSITION menjaga jarak. Ada satu temuan: `CooldownDecorator` dari modul mengembalikan Running selama cooldown, sehingga Selector berhenti di cabang itu dan cabang lain tidak pernah dicoba. Karena itu kami membuat `CooldownGate` yang mengembalikan Failure, sehingga Selector lanjut ke cabang berikutnya. Kayla akan melanjutkan ke kelelawar dan boss."

---

## Bagian 3: Kayla (06:00 - 10:00)

### 3.1 Kelelawar: Flocking (06:00 - 07:00)

**Tampilan layar:** Ruang 4 dengan AI Vision. Tunjukkan garis ungu antar kelelawar.

**Narasi:**
"Kelelawar mata memakai **flocking** atau boids. Setiap kelelawar hanya melihat tetangga dalam radius tertentu, ditunjukkan garis ungu ini. Ada tiga aturan: Separation agar tidak bertabrakan, Alignment agar searah dengan kawanan, dan Cohesion agar tetap berkumpul. Kami tambahkan target yang berputar mengelilingi Capy, jadi kawanan seolah mengepung. Sesekali satu kelelawar bergetar lalu menukik. Tukikannya bisa dihindari dengan dash. Setelah ruangan ini muncul onsen yang memulihkan darah penuh."

### 3.2 Golem King: Behavior Tree dua fase (07:00 - 08:30)

**Tampilan layar:** Masuk Ruang 5. Intro sinematik, kartu judul GOLEM KING. Lawan boss sampai fase marah.

**Narasi:**
"Ini boss-nya, Golem King. Saat kita masuk ada intro sinematik dan musik berganti. Golem juga memakai **behavior tree**, tapi lebih besar. Kalau Capy dekat ia menghantam tanah. Kalau jauh dan terlihat ia charge. Kalau agak jauh ia melempar batu yang titik jatuhnya ditandai lingkaran merah. Charge yang menabrak tembok membuatnya pusing dan menerima damage satu setengah kali, jadi pemain bisa memancingnya ke dinding. Saat HP-nya di bawah 50 persen, cabang Enrage aktif: ia lebih cepat, hantamannya menimbulkan shockwave yang harus ditembus dengan dash, dan muncul hujan batu. Setelah kalah ia menjatuhkan Golden Yuzu. Kita ambil, dan muncul layar kemenangan dengan waktu dan rank."

### 3.3 Autopilot: Utility AI (08:30 - 09:30)

**Tampilan layar:** Tekan `Esc`, pilih **RESTART** (atau pakai game yang masih berjalan di ruangan mana pun). Tekan `F10`, lalu `V`.

**Narasi:**
"Satu fitur tambahan: Autopilot dengan tombol F10. Di sini AI yang memainkan Capy, memakai **utility AI** atau context steering. Setiap frame ada 16 arah gerak plus pilihan diam yang diberi skor. Skornya berkurang oleh bahaya, yaitu musuh dekat, proyektil yang mendekat, area merah, dan shockwave, dan bertambah oleh tujuan seperti pintu, peti, atau Golden Yuzu. Garis hijau adalah arah yang aman, merah yang berbahaya, dan garis paling tebal adalah pilihannya. Fitur ini juga kami pakai untuk menguji keseimbangan game secara otomatis. Dalam lima uji, Autopilot selalu menang dengan waktu sekitar 3 sampai 4 menit, dan dari hasil itu kami menaikkan HP musuh per ruangan serta HP boss."

### 3.4 Penutup (09:30 - 10:00)

**Tampilan layar:** Kembali ke menu utama.

**Narasi:**
"Kesimpulannya, Capy Conquest menerapkan lima teknik AI dari kuliah dalam satu game: steering pada slime, FSM dan A* pada skeleton, behavior tree pada wizard dan boss, flocking pada kelelawar, dan utility AI pada Autopilot. Semuanya bisa dilihat langsung lewat AI Vision. Terima kasih. Wassalamualaikum."

---

## Kemungkinan pertanyaan dosen

| Pertanyaan | Jawaban singkat |
|---|---|
| Kenapa memakai A\* buatan sendiri, bukan NavMesh? | Supaya proses pathfinding terlihat dan bisa dijelaskan: grid dibuat dari collider berlayer Obstacle, A\* 8 arah dengan heuristik octile, lalu jalur dihaluskan dengan string pulling. Kodenya di `NavGrid.cs`. |
| Apa beda FSM skeleton dan BT wizard? | FSM: satu state aktif dan transisi ditulis eksplisit di tiap state. BT: setiap frame pohon dievaluasi dari akar, prioritas ditentukan urutan cabang, jadi mudah menambah perilaku baru tanpa mengubah transisi lain. |
| Bagaimana flocking dibatasi agar tidak keluar ruangan? | Ada gaya Bounds yang mendorong kelelawar kembali ke tengah jika melewati batas, ditambah posisinya di-clamp ke dalam ruangan. |
| Bagaimana keseimbangan kesulitan diuji? | Dengan Autopilot dan skrip uji otomatis yang mencatat durasi tiap ruangan dan damage. HP dan damage musuh dinaikkan per ruangan dari hasil uji itu. Angkanya ada di README. |
| Asetnya dari mana? | Semua model dari primitif Unity, tekstur dan ikon digambar dengan kode (signed distance function dan noise), suara dan musik disintesis dengan kode. Hanya dua font dari Google Fonts (lisensi OFL). |
