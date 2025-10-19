# Advanced Installer için Kurulum Kılavuzu
# YouTube Kesit İndirici Modülü - Araç Kurulumu

## Seçenek 1: Portable Kurulum (ÖNERİLEN)

### Advanced Installer Ayarları:

1. **Files and Folders** bölümüne git
   - Application Folder altında `Tools` klasörü oluştur
   - Veya kurulum sırasında otomatik indirmek için Custom Action kullan

2. **Custom Actions** → **Install** altına ekle:
   ```
   Tip: PowerShell Script
   Script: DownloadTools.ps1
   Execution Time: Immediate
   When: After InstallFiles
   Execution Account: Current User (Admin değil!)
   Script Arguments: -TargetDir "[APPDIR]\Tools"
   ```

3. **Build** et

### Sonuç:
```
C:\Program Files\SwissKnifeApp\
├─ Türk Çakısı.exe
└─ Tools\
   ├─ yt-dlp.exe     (~10 MB)
   └─ ffmpeg.exe     (~120 MB)
```

**Toplam ek boyut:** ~130 MB (kurulum paketine dahil edilmezse, ilk çalışmada indirilir)

---

## Seçenek 2: C:\Tools + PATH (Sistem Geneli)

### Advanced Installer Ayarları:

1. **Prerequisites** kullan (winget varsa):
   ```
   Search: winget
   Command: winget install yt-dlp -h --accept-source-agreements --accept-package-agreements
   Command: winget install ffmpeg -h --accept-source-agreements --accept-package-agreements
   ```

2. **Manuel İndirme + PATH** için:
   - Custom Action: DownloadTools.ps1 (TargetDir: C:\Tools)
   - Environment Variables bölümünde PATH'e ekle:
     ```
     Variable: PATH
     Value: [~];C:\Tools
     Action: Append
     ```

**NOT:** Bu yöntem admin yetkisi gerektirir ve PATH değişikliği için yeniden başlatma gerekebilir.

---

## Seçenek 3: Hibrit (En Esnek)

1. **Kurulum sırasında kullanıcıya sor:**
   ```
   Dialog: "YouTube indirme araçları nasıl kurulsun?"
   [ ] Uygulama ile birlikte (portable, ~130 MB)
   [x] İlk kullanımda otomatik indir (önerilen)
   [ ] Kendim kuracağım
   ```

2. **İlk kullanımda otomatik indir:**
   - Uygulama ilk kez açıldığında `Tools` klasörünü kontrol et
   - Yoksa arka planda indir (splash screen / progress göster)
   - Bu servisi ekleyebilirim (şu an hata veriyor, indirme kodu ekleyebiliriz)

---

## 📦 Kurulum Paketine Gömme (Offline)

Eğer internet olmadan kurulum istiyorsan:

1. **yt-dlp.exe ve ffmpeg.exe'yi indir**
2. **Advanced Installer'da:**
   - Files and Folders → Application Folder → Tools klasörü oluştur
   - İndirdiğin dosyaları sürükle-bırak
   - Paket boyutu: +130 MB

3. **Avantajları:**
   - ✅ Offline kurulum
   - ✅ Belirli versiyon garantisi
   - ❌ Paket boyutu büyür
   - ❌ Güncelleme için yeni kurulum gerekir

---

## 🎯 HANGİSİNİ ÖNERİRİM?

**EN İYİ ÇÖZÜM: Seçenek 1 + İlk Açılışta Otomatik İndirme**

### Nasıl Çalışır:
1. Setup kurulur (araçsız, küçük paket)
2. Kullanıcı ilk kez YouTube modülünü açar
3. Araçlar yoksa:
   - "Gerekli araçlar indiriliyor... (yt-dlp: 10 MB, ffmpeg: 120 MB)"
   - Progress bar gösterilir
   - İndirme bitince otomatik devam eder
4. Sonraki kullanımlarda direkt çalışır

### Bu Çözümün Avantajları:
- ✅ Küçük setup boyutu
- ✅ Admin yetkisi gerekmez
- ✅ İnternet bağlantısı sadece ilk kullanımda gerekir
- ✅ Otomatik güncelleme yapılabilir (sonra ekleyebiliriz)
- ✅ Kullanıcı hiçbir şey yapmak zorunda değil

---

## 📝 YAPILACAKLAR (İstersen Eklerim)

- [ ] İlk açılışta otomatik indirme servisi
- [ ] İndirme progress UI (splash screen)
- [ ] "Araçları Kontrol Et" butonu (Ayarlar menüsü?)
- [ ] Araç güncelleme kontrolü
- [ ] Offline kurulum seçeneği (setup'a göm)

Hangisini tercih edersin? Ben **Seçenek 1 + İlk Açılış** öneririm ve gerekli kodu hemen ekleyebilirim.
