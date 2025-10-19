# 🎬 YouTube Kesit İndirici - Kullanıcı Kılavuzu

## ✨ Yeni Özellikler

### 🔧 Otomatik Araç Kurulumu

Artık yt-dlp ve ffmpeg araçlarını **manuel kurmaya gerek yok**!

**İlk Kullanımda:**
1. YouTube modülünü ilk kez açtığınızda otomatik kontrol edilir
2. Araçlar eksikse şu mesajı görürsünüz:
   ```
   YouTube kesit indirici için gerekli araçlar eksik: yt-dlp ve ffmpeg
   
   İndirme boyutu: ~130 MB
   Konum: [UygulamaDizini]\Tools
   
   Şimdi indirmek ister misiniz?
   ```
3. **"Evet"** derseniz: Otomatik indirilir (progress bar ile)
4. **"Hayır"** derseniz: Manuel "Araçları Kur" butonu ile sonra kurabilirsiniz

### 📦 Araçları Kur Butonu

Herhangi bir zamanda **"Araçları Kur"** butonuna tıklayarak:
- yt-dlp ve ffmpeg'i otomatik indirebilirsiniz
- İndirme ilerlemesini log'da takip edebilirsiniz
- Başarılı olunca bildirim alırsınız

---

## 🚀 Kullanım Modları

### Mod 1: Manuel Aralık Girişi (Yeni!)

1. **"Manuel"** seçeneğini işaretleyin
2. **Başlangıç** ve **Bitiş** girişlerine zaman yazın:
   - Format: `mm:ss` veya `hh:mm:ss`
   - Örnek: `00:00` ve `02:30`
3. **"Ekle"** butonuna tıklayın
4. İstediğiniz kadar aralık ekleyin
5. Seçili aralığı silmek için **"Sil"** kullanın

### Mod 2: TXT Dosyasından

1. **"TXT"** seçeneğini işaretleyin
2. **"Seç"** ile TXT dosyasını seçin
3. TXT formatı:
   ```
   00:00 02:30
   05:10 07:45
   01:15:30 01:20:00
   ```

---

## 🎯 Adım Adım Kullanım

1. **YouTube URL'si** girin
2. **Aralıkları** girin (Manuel veya TXT)
3. **Çıkış Klasörü** seçin
4. **"İndirmeyi Başlat"** tıklayın
5. İlerlemeyi izleyin:
   - **Toplam İlerleme**: Tüm kesitler için
   - **Parça İlerleme**: Şu anki kesit için
   - **Log**: Detaylı bilgiler

---

## 📂 Dosyalar Nereye Kaydediliyor?

### İndirilen Videolar:
Seçtiğiniz çıkış klasöründe:
```
kesit_01.mp4
kesit_02.mp4
kesit_03.mp4
...
```

### Araçlar (yt-dlp & ffmpeg):
```
[Uygulama Dizini]\Tools\
├─ yt-dlp.exe    (~10 MB)
└─ ffmpeg.exe    (~120 MB)
```

---

## ⚠️ Sorun Giderme

### "Araçlar bulunamadı" Hatası
1. **"Araçları Kur"** butonuna tıklayın
2. İnternet bağlantınızı kontrol edin
3. Firewall/Antivirus'ün indirmeyi engellemediğinden emin olun

### "There are no chapters matching the regex"
✅ **Düzeltildi!** Artık bu hatayı almamalısınız.
- Zaman aralıkları saniye cinsinden gönderiliyor
- `*` prefix otomatik ekleniyor

### İlk 10 Saniye Boş Geliyor
✅ **Düzeltildi!**
- `--force-keyframes-at-cuts` eklendi
- Tam saniye formatı kullanılıyor

### İndirme Çok Yavaş
- İnternet hızınıza bağlı
- YouTube sunucusuna bağlı
- 1080p yerine 720p denemek için serviste format değiştirebilirsiniz

---

## 🎨 Video Kalitesi

**Varsayılan:** 1080p veya mevcut en yüksek kalite
**Format:** MP4 (video + ses birleştirilmiş)

---

## 💡 İpuçları

✅ **Manuel mod daha hızlı:** TXT dosyası oluşturmadan direkt giriş
✅ **Çoklu aralık:** İstediğiniz kadar ekleyin
✅ **Offline çalışma:** Araçlar bir kez kurulduktan sonra internet gerektirmez (sadece video indirme için gerekir)
✅ **Temiz kaldırma:** Uygulamayı sildiğinizde `Tools` klasörü de silinir

---

## 🔄 Güncelleme

Araçları güncellemek için:
1. `[UygulamaDizini]\Tools` klasörünü silin
2. **"Araçları Kur"** butonuna tıklayın
3. En son versiyonlar indirilir

---

## 📞 Destek

Sorun yaşarsanız:
1. Log çıktısını kontrol edin
2. `Tools` klasörünün varlığını kontrol edin
3. İnternet bağlantınızı test edin

---

**Keyifli kullanımlar!** 🎉
