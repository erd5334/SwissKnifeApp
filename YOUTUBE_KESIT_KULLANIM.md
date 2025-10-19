# YouTube Kesit İndirici - Kullanım Kılavuzu

## 📋 TXT Dosya Formatı

TXT dosyanızda her satıra bir aralık yazın. Format:

```
BAŞLANGIÇ BİTİŞ
```

### ✅ Doğru Örnekler:

```txt
00:00 02:30
05:10 07:45
01:15:30 01:20:00
```

- **mm:ss** formatı: Dakika:Saniye (örn: `05:30` = 5 dakika 30 saniye)
- **hh:mm:ss** formatı: Saat:Dakika:Saniye (örn: `01:15:30` = 1 saat 15 dakika 30 saniye)
- Boşluk veya tab ile ayırın
- Boş satırlar göz ardı edilir

### ❌ Yanlış Örnekler:

```txt
2:30 - 5:00        ❌ Tire kullanmayın
00:00-02:30        ❌ Boşluk olmalı
150-180            ❌ Sadece saniye yazılamaz
```

---

## 🔧 Gerekli Araçlar

Bu modül çalışmak için **yt-dlp** ve **ffmpeg** gerektirir.

### Yöntem 1: Portable Kullanım (ÖNERİLEN)

1. **yt-dlp.exe** ve **ffmpeg.exe** dosyalarını indirin
2. Uygulamanın yanında `Tools` klasörü oluşturun
3. Her iki exe'yi bu klasöre kopyalayın

```
📁 Türk Çakısı.exe
📁 Tools/
   └─ yt-dlp.exe
   └─ ffmpeg.exe
```

**İndirme Linkleri:**
- yt-dlp: https://github.com/yt-dlp/yt-dlp/releases/latest
- ffmpeg: https://github.com/BtbN/FFmpeg-Builds/releases (ffmpeg-master-latest-win64-gpl.zip)

### Yöntem 2: Sistem Geneli Kurulum

```powershell
winget install yt-dlp
winget install ffmpeg
```

Veya `C:\Tools\` klasörüne koyup PATH'e ekleyin.

---

## 🚀 Kullanım

1. **YouTube URL'si**: Video linkini yapıştırın
2. **TXT Dosyası**: Aralıkları içeren txt dosyasını seçin
3. **Çıkış Klasörü**: Videoların kaydedileceği yeri seçin
4. **İndirmeyi Başlat** butonuna tıklayın
5. İşlemi durdurmak için **Durdur** butonunu kullanın

---

## 📊 Çıktı

Kesitler şu isimle kaydedilir:
- `kesit_01.mp4`
- `kesit_02.mp4`
- `kesit_03.mp4`
- ...

**Video Kalitesi**: Otomatik olarak 1080p veya mevcut en yüksek kalite seçilir.

---

## ⚠️ Sorun Giderme

### "yt-dlp bulunamadı" Hatası
- Araçların doğru konumda olduğundan emin olun
- PATH'e eklendiyse VS Code'u yeniden başlatın

### İlk Saniyeler Boş Geliyor
- ✅ DÜZELTİLDİ: Artık tam olarak belirttiğiniz saniyede kesiyor

### Yanlış Zaman Aralığı İndiriliyor
- ✅ DÜZELTİLDİ: Format sorunu çözüldü (mm:ss → saniye)
- TXT dosyanızın formatını kontrol edin

---

## 💡 İpuçları

- Videoları test etmek için kısa aralıklar kullanın (örn: `00:00 00:10`)
- İnternet hızınıza bağlı olarak indirme süresi değişir
- Çok sayıda kesit indirirken bilgisayarın uyku moduna geçmediğinden emin olun
