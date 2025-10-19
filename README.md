# 🔧 Türk Çakısı - Çok Amaçlı Araç Kutusu

Türk Çakısı, günlük işlerinizi kolaylaştırmak için tasarlanmış, modern ve kullanıcı dostu bir masaüstü uygulamasıdır. WPF (.NET 8) ile geliştirilmiş bu uygulama, çeşitli araçları tek bir çatı altında toplar.

## 📋 İçindekiler
- [Özellikler](#-özellikler)
- [Kurulum](#-kurulum)
- [Kullanılan Teknolojiler](#-kullanılan-teknolojiler)
- [Modüller](#-modüller)
- [Ekran Görüntüleri](#-ekran-görüntüleri)
- [Geliştirme](#-geliştirme)
- [Katkıda Bulunma](#-katkıda-bulunma)
- [Lisans](#-lisans)

## 🎯 Özellikler

### 📝 Metin İşlemleri
- **Büyük/Küçük Harf Dönüştürme**: Metinleri büyük, küçük veya başlık harfine dönüştürün
- **Tersten Yazma**: Metni ters çevirin
- **URL Encode/Decode**: URL encoding/decoding işlemleri
- **Base64 Encode/Decode**: Base64 dönüşümleri
- **Kelime & Karakter Sayımı**: Metin istatistikleri
- **Satır Sıralama**: Satırları alfabetik veya ters alfabetik sırala
- **Tekrarlayan Satırları Kaldır**: Duplicate satırları temizle
- **Boşlukları Temizle**: Fazla boşlukları kaldır
- **Satır Numaralandırma**: Satırlara numara ekle

### 🔐 Şifre ve Güvenlik Araçları
- **Şifre Üretici**: Güçlü ve rastgele şifreler oluşturun
  - Özelleştirilebilir uzunluk (4-128 karakter)
  - Büyük/küçük harf, sayı, özel karakter seçenekleri
  - Okunabilir şifre modu
  - Birden fazla şifre üretme
- **Hash Üretici**: MD5, SHA1, SHA256, SHA512 hash değerleri
- **AES Şifreleme/Çözme**: Metinleri AES algoritması ile şifrele/çöz

### 📊 QR Kod & Barkod Araçları
- **QR Kod Oluşturma**: Metin, URL, WiFi, E-posta, SMS, vCard için QR kodları
  - Özelleştirilebilir boyut ve hata düzeltme seviyesi
  - PNG formatında kaydetme
- **Barkod Oluşturma**: CODE_128, EAN_13, UPC_A formatları
- **QR/Barkod Okuma**: Görüntülerden QR kod ve barkod okuma

### 🖼️ Görsel Dönüştürücü
- **Format Dönüşümü**: JPG, PNG, BMP, GIF, WEBP, ICO, SVG formatları arası dönüşüm
  - **ICO Format**: Herhangi bir görseli Windows icon dosyasına çevir
  - **SVG Desteği**: SVG'den PNG'ye ve PNG'den SVG'ye dönüşüm
- **Toplu İşlem**: Birden fazla görseli aynı anda dönüştür
- **Yeniden Boyutlandırma**: Özel boyutlar veya yüzde ile
- **Filtreler**: Gri tonlama, renk tersine çevirme, siyah/beyaz
- **Ayarlar**: Doygunluk ve parlaklık kontrolü
- **Kalite Ayarı**: JPG kalite seçimi (0-100)
- **Sürükle-Bırak**: Dosya ve klasörleri direkt olarak sürükleyip bırakın
- **Canlı Önizleme**: Değişiklikleri anında görün

### 📄 PDF İşlemleri
- **PDF Birleştirme**: Birden fazla PDF'i tek dosyada birleştir
- **PDF Bölme**: PDF'leri sayfa aralığına göre böl
- **Görüntüden PDF**: JPG, PNG gibi görüntüleri PDF'e dönüştür

### 🔢 Birim Dönüştürücü
- **Uzunluk**: Metre, kilometre, santimetre, milimetre
- **Ağırlık**: Gram, kilogram, ton, miligram
- **Sıcaklık**: Celsius, Fahrenheit, Kelvin
- **Hız**: km/h, m/s, mph
- **Alan**: m², km², hektar

### 🌐 JSON & XML Formatter
- **JSON Formatter**: JSON formatını düzenle ve güzelleştir
- **XML Formatter**: XML formatını düzenle ve güzelleştir
- **JSON ↔ XML**: JSON ve XML formatları arası dönüşüm
- **Söz Dizimi Vurgulama**: Kodları renkli görüntüle (AvalonEdit)
- **Hata Gösterimi**: Geçersiz JSON/XML için hata mesajları

### 💸 Para Yazıya Çevirme
- **Çoklu Dil Desteği**: Türkçe ve İngilizce
- **Para Birimleri**: TRY, USD, EUR, GBP, RUB
- **Format Seçenekleri**:
  - Büyük harf / Küçük harf / İlk harf büyük
  - Boşluksuz yazım
  - Özel ayraç belirleme
  - İlk harfi büyük yazma
- **HBMoneyToWords Kütüphanesi**: Profesyonel para-metin dönüşümü

Not: Bu modülün iş mantığı `Services/MoneyToTextService.cs` dosyasına taşınmıştır. UI katmanı sadece kullanıcı etkileşimini yönetir.

### 🌐 İnternet Hız Testi
- **İndirme Hızı**: Gerçek zamanlı indirme hızı ölçümü (Mbps)
- **Yükleme Hızı**: Gerçek zamanlı yükleme hızı ölçümü (Mbps)
- **Ping Testi**: Sunucu gecikme süresi ölçümü (ms)
- **Grafik Gösterimi**: Hız değişimlerini canlı grafik ile takip et
- **Detaylı Sonuçlar**: Ortalama, maksimum ve minimum hız değerleri

### 📋 Pano Geçmişi
- **Otomatik Kayıt**: Panoya kopyalanan her şeyi kaydet
- **Metin ve Görsel**: Hem metin hem de görsel kopyaları sakla
- **Arama**: Geçmiş pano kayıtlarında arama yap
- **Yeniden Kullanım**: Eski pano kayıtlarını tek tıkla kullan
- **Temizleme**: Tüm geçmişi tek seferde temizle

### 🖼️ Resim Kolaj
- **Çoklu Fotoğraf**: 1-20 arası fotoğrafla kolaj oluştur
- **Şablon Seçimi**: Otomatik grid düzeni (2x2, 3x3, 4x4, 5x4)
- **Özelleştirme**: 
  - Kenarlık kalınlığı ve rengi
  - Arka plan rengi (ColorPicker ile)
  - Köşe yuvarlatma (0-50px)
  - Fotoğraf arası boşluk
- **Metin Ekleme**: 
  - Kolaj üzerine yazı ekleme
  - Pozisyon seçimi (üst/orta/alt, sol/orta/sağ)
  - Yazı rengi (ColorPicker ile)
  - Font boyutu ayarı
- **Önizleme & Kaydetme**: Canlı önizleme ve PNG/JPG formatında kayıt
- **Sürükle-Bırak**: Fotoğrafları direkt olarak sürükleyip bırakın

### 💰 Vergi Hesaplayıcı
Türkiye vergi sistemine özel 13 farklı vergi türü hesaplama:
- **📊 Gelir Vergisi**: 5 dilimli artan oranlı hesaplama (ücret/ücret dışı ayrımı)
- **🧾 KDV Hesaplama**: KDV dahil/hariç fiyat hesaplama (5 farklı oran)
- **🏢 Kurumlar Vergisi**: Kurumlar vergisi hesaplama (finans sektörü ayrımı)
- **🏠 Kira Gelir Vergisi**: Kira geliri vergisi hesaplama (istisna ile)
- **📄 Damga Vergisi**: Belge ve sözleşme damga vergisi (‰0.948)
- **🚗 MTV (Motorlu Taşıtlar Vergisi)**: Otomobil ve motosiklet MTV hesaplama
- **✂️ KDV Tevkifatı**: 9 hizmet kategorisi için KDV tevkifat hesaplama
- **📈 Değer Artış Kazancı**: Gayrimenkul ve menkul değer artış kazancı (%50 istisna)
- **🏰 Değerli Konut Vergisi**: 12.5M TL üzeri konutlar için lüks konut vergisi
- **🏘️ Emlak Vergisi**: Bina ve arazi emlak vergisi hesaplama
- **⛽ ÖTV (Özel Tüketim Vergisi)**: Akaryakıt ÖTV hesaplama
- **🎁 Veraset ve İntikal Vergisi**: Miras vergisi hesaplama (mirasçı türüne göre)
- **⏱️ Vergi Gecikme Faizi**: Geciken vergi borçları için faiz hesaplama

**Özellikler**:
- 2024-2025 vergi oranları (JSON cache)
- Dilim bazlı detaylı hesaplama
- Matrah, vergi ve net tutar gösterimi
- Web scraping ile otomatik oran güncelleme (HtmlAgilityPack)
- Kullanıcı dostu arayüz ve validasyon

## 🚀 Kurulum

### Gereksinimler
- Windows 10/11
- .NET 8.0 Runtime veya SDK
- En az 4GB RAM
- 100MB boş disk alanı

### Adımlar
1. **Proje Dosyalarını İndir**
   ```bash
   git clone https://github.com/kullaniciadi/SwissKnifeApp.git
   cd SwissKnifeApp
   ...existing code...

2. **Bağımlılıkları Yükle**
   Görsel dönüştürme modülünde SVG ve ICO desteği eklendi, dosya kaydetme hatası giderildi.
   Birim dönüştürücü modülüne zaman/tarih araçları ve yaş hesaplama fonksiyonları eklendi.
   Metin işlemleri modülüne yeni araçlar (kelime/simge sayacı, base64 şifreleme, lorem ipsum üretici) eklendi.
   JSON tabanlı ayar sistemi ile son girilen veriler hatırlanıyor.
   Proje GitHub’a yüklendi, katkı rehberi eklendi.
   ```bash
   dotnet restore
   Projeye katkı için lütfen CONTRIBUTING.md dosyasını inceleyin ve pull request gönderin.
   ```

3. **Projeyi Derle**
   ```bash
   dotnet build
   ```

4. **Uygulamayı Çalıştır**
   ```bash
   dotnet run
   ```

### Harici Araçlar (YouTube/Ses/Video modülleri)
- Uygulama, `ffmpeg`, `ffprobe` ve `yt-dlp` araçlarını otomatik keşfeder.
- Portable kullanım için, uygulama klasöründe `Tools/` dizini oluşturup `yt-dlp.exe` ve `ffmpeg.exe` dosyalarını kopyalayabilirsiniz.
- Alternatif olarak `C:\\Tools` veya PATH üzerinde bulunmaları yeterlidir.
- “Araçları Kur” butonu ile otomatik indirme yapılabilir.
- Ayrıntılı kurulum ve dağıtım notları: `SETUP_KILAVUZU.md`

### Yayınlama (Release)
```bash
dotnet publish -c Release -r win-x64 --self-contained
```

## 🛠️ Kullanılan Teknolojiler

### Framework & Dil
- **.NET 8.0**: Modern ve performanslı framework
- **WPF (Windows Presentation Foundation)**: Zengin masaüstü UI
- **C# 12**: Güçlü ve modern programlama dili

### UI Kütüphaneleri
- **MahApps.Metro** (2.4.11): Modern ve şık UI tasarımı
- **MahApps.Metro.IconPacks** (6.1.0): Zengin ikon koleksiyonu
  - FontAwesome
  - Material Design
  - Modern Icons
- **LucideIcons** (1.0.0.5): Minimalist ve şık ikonlar
- **AvalonEdit** (6.3.1.120): Söz dizimi vurgulama editörü

### Görsel İşleme
- **SixLabors.ImageSharp** (3.1.4): Güçlü görsel işleme kütüphanesi
- **SixLabors.ImageSharp.Drawing** (2.x): Kolaj üzerinde metin/çizim işlemleri
- **Svg** (3.4.7): SVG dosya desteği

### PDF İşlemleri
- **iTextSharp** (5.5.13.4): PDF oluşturma ve düzenleme
- **PdfiumViewer** (2.13.0): PDF görüntüleme

### QR & Barkod
- **QRCoder** (1.6.0): QR kod oluşturma
- **ZXing.Net** (0.16.9): Barkod okuma ve oluşturma

### Diğer Kütüphaneler
- **Newtonsoft.Json** (13.0.3): JSON işlemleri
- **HBMoneyToWords** (1.0.0): Para-metin dönüşümü
- **WindowsAPICodePack** (8.0.5): Windows özel özellikleri
- **CommunityToolkit.Mvvm** (8.4.0): MVVM pattern desteği
- **HtmlAgilityPack** (1.12.4): Web scraping ve HTML parsing

### Harici Araçlar
- **FFmpeg / FFprobe**: Ses/video dönüştürme, kırpma, kırpma (crop), süre ölçümü
- **yt-dlp**: YouTube videolarını indirme ve zaman aralığına göre kesit alma

Not: Bu araçlar portable olarak uygulama dizininde `Tools/` klasöründe veya sistem PATH üzerinde bulunabilir. Uygulama içerisindeki “Araçları Kur” butonu ile otomatik indirilebilir.

## 📦 Modüller

### 1. TextOperationsPage
**Dosya**: `Views/Modules/TextOperationsPage.xaml`
- Metin işleme araçları
- 12+ farklı metin dönüşüm özelliği
- Gerçek zamanlı işleme

### 2. PasswordToolsPage
**Dosya**: `Views/Modules/PasswordToolsPage.xaml`
- Şifre üretici
- Hash hesaplama
- AES şifreleme/çözme

### 3. QrBarcodeToolsPage
**Dosya**: `Views/Modules/QrBarcodeToolsPage.xaml`
- QR kod oluşturma (6 farklı tip)
- Barkod oluşturma (3 format)
- QR/Barkod okuma

### 4. ImageConverterPage
**Dosya**: `Views/Modules/ImageConverterPage.xaml`
- 7 format desteği (JPG, PNG, BMP, GIF, WEBP, ICO, SVG)
- Toplu dönüşüm
- Gelişmiş filtreleme
- Sürükle-bırak özelliği

### 5. PdfOperationsPage
**Dosya**: `Views/Modules/PdfOperationsPage.xaml`
- PDF birleştirme
- PDF bölme
- Görüntüden PDF

### 6. UnitConverterPage
**Dosya**: `Views/Modules/UnitConverterPage.xaml`
- 5 farklı birim kategorisi
- Çift yönlü dönüşüm
- Gerçek zamanlı hesaplama

### 7. JsonXmlFormatterPage
**Dosya**: `Views/Modules/JsonXmlFormatterPage.xaml`
- JSON/XML formatçı
- Söz dizimi vurgulama
- Format dönüşümü

### 8. MoneyToTextPage
**Dosya**: `Views/Modules/MoneyToTextPage.xaml`
- 2 dil desteği
- 5 para birimi
- Özelleştirilebilir format

### 9. SpeedTestPage
**Dosya**: `Views/Modules/SpeedTestPage.xaml`
- İndirme/Yükleme hızı
- Ping testi
- Grafik gösterimi

### 10. ClipboardHistoryPage
**Dosya**: `Views/Modules/ClipboardHistoryPage.xaml`
- Otomatik pano kaydı
- Arama özelliği
- Metin ve görsel desteği

### 11. ColorPickerPage
**Dosya**: `Views/Modules/ColorPickerPage.xaml`
- Renk seçici modülü (Paint tarzı eyedropper)
- Tüm ekrandan renk seçme (global eyedropper)
- HEX, RGB, HSL kodları ve 30+ programlama dili için kod blokları
- Kopyalanabilir renk kodları
- Çoklu monitör desteği

### 12. EyedropperOverlayWindow
**Dosya**: `Views/Modules/EyedropperOverlayWindow.xaml`
- Global ekran renk seçici (Win32 interop)
- Şeffaf overlay ile ekranın herhangi bir yerinden renk alma
- Seçilen rengi ColorPickerPage'e aktarma

### 13. TextSummarizerPage
**Dosya**: `Views/Modules/TextSummarizerPage.xaml`
- Metin özetleme (oransal özet)
- Anahtar kelime bulma
- Önemli cümle çıkarma
- Türkçe/İngilizce dil desteği
- Kopyalanabilir özet ve anahtar kelimeler

### 14. FileManagerPage
**Dosya**: `Views/Modules/FileManagerPage.xaml`
- Dosya karşılaştırma (fark analizi)
- Dosya şifreleme/çözme
- Toplu dosya yeniden adlandırma
- Gelişmiş filtre ve arama seçenekleri

### 15. PhotoCollagePage
**Dosya**: `Views/Modules/PhotoCollagePage.xaml`
- 1-20 arası fotoğrafla kolaj oluşturma
- Otomatik şablon seçimi
- Kenarlık, boşluk ve köşe ayarları
- ColorPicker ile renk seçimi
- Metin ekleme ve pozisyonlandırma
- PNG/JPG formatında kaydetme

### 16. TaxCalculatorPage
**Dosya**: `Views/Modules/TaxCalculatorPage.xaml`
**Servisler**: 
- `Services/TaxCalculationService.cs` - 20+ vergi hesaplama metodu
- `Services/TaxRatesScraperService.cs` - Web scraping servisi
- `Models/TaxRateModels.cs` - 17 vergi veri modeli
- `Data/tax-rates.json` - 2024-2025 vergi oranları cache

13 farklı vergi türü hesaplama:
- Gelir Vergisi (5 dilim)
- KDV (5 oran)
- Kurumlar Vergisi
- Kira Gelir Vergisi
- Damga Vergisi
- MTV (Motorlu Taşıtlar)
- KDV Tevkifatı (9 kategori)
- Değer Artış Kazancı
- Değerli Konut Vergisi
- Emlak Vergisi
- ÖTV (Akaryakıt)
- Veraset ve İntikal Vergisi
- Vergi Gecikme Faizi

### 17. YouTubeClipDownloaderPage
**Dosya**: `Views/Modules/YouTubeClipDownloaderPage.xaml`
**Servis**: `Services/YoutubeTxtClipDownloaderService.cs`
- TXT veya Manuel aralık girişi ile YouTube videosundan kesit indirme
- `--download-sections` ile tam saniye bazlı kesitler (örn. `*150-180`)
- `--force-keyframes-at-cuts` ile temiz kesme
- 1080p’ye kadar en iyi kalite otomatik seçim ve MP4 birleştirme
- Toplam/parça ilerleme çubukları ve ayrıntılı log
- Taşınabilir araç keşfi: `[Uygulama]/Tools`, `C:\\Tools`, veya PATH
- “Araçları Kur” ile otomatik yt-dlp/ffmpeg indirimi
   - Ayrıntılı kullanım ve TXT formatı: `YOUTUBE_KESIT_KULLANIM.md`
   - Modül genel kılavuzu: `YOUTUBE_KULLANIM.md`

### 18. AudioToolsPage
**Dosya**: `Views/Modules/AudioToolsPage.xaml`
**Servis**: `Services/AudioToolsService.cs`
- Toplu dönüştürme ve Trim (kesme)
- Formatlar: MP3, AAC (m4a), WAV, FLAC, OPUS
- Kalite ön ayarları: Highest/High/Medium/Low/Lossless
- Opsiyonel ses normalizasyonu: `loudnorm`
- İlerleme takibi (ffmpeg zamanından) ve loglama
- ffprobe ile süre tespiti, portable araç keşfi (`Tools/`, `C:\\Tools`, PATH)

### 19. VideoToolsPage
**Dosya**: `Views/Modules/VideoToolsPage.xaml`
**Servis**: `Services/VideoToolsService.cs`
- Dönüştürme ve Trim/Kırp (crop)
- Formatlar: MP4, MKV, WEBM, MOV, TS, AVI, FLV
- Codecler: H.264, H.265 (HEVC), VP9, (uygun durumlarda) Copy
- CRF tabanlı kalite: Highest/High/Medium/Low/Lossless
- Çözünürlük ön ayarları: Original, 2160p, 1440p, 1080p, 720p, 480p
- Kapsayıcı/codec uyumluluğu, `yuv420p`, MP4 için `-movflags +faststart` ve HEVC için `-tag:v hvc1`
- ffprobe ile süre tespiti, ilerleme takibi, portable araç keşfi

## 🎨 Tasarım Özellikleri

### Renkler
- **Primary**: `#1E88E5` (Mavi)
- **Accent**: `#FF6F00` (Turuncu)
- **Background**: `#F5F5F5` (Açık Gri)
- **Text**: `#2C3E50` (Koyu Gri)

### Tipografi
- **Başlıklar**: 18-22pt, Bold
- **Gövde Metni**: 14-16pt, Regular
- **Butonlar**: 14-16pt, Medium

### UI Elemanları
- Modern, düz tasarım
- Yumuşak köşeler (BorderRadius: 4-8px)
- Gölge efektleri
- Hover animasyonları
- İkon odaklı tasarım

## 📸 Ekran Görüntüleri

*Ekran görüntüleri eklenecek*

## 🔧 Geliştirme

### Proje Yapısı
```
SwissKnifeApp/
├── App.xaml                    # Uygulama yapılandırması
├── MainWindow.xaml             # Ana pencere ve navigasyon
├── Models/                     # Veri modelleri
│   ├── ClipboardItem.cs
│   └── TaxRateModels.cs       # Vergi hesaplama modelleri (17 class)
├── ViewModels/                 # MVVM view modelleri
│   └── MainViewModel.cs
├── Views/
│   └── Modules/               # Modül sayfaları
│       ├── TextOperationsPage.xaml
│       ├── PasswordToolsPage.xaml
│       ├── QrBarcodeToolsPage.xaml
│       ├── ImageConverterPage.xaml
│       ├── PdfOperationsPage.xaml
│       ├── UnitConverterPage.xaml
│       ├── JsonXmlFormatterPage.xaml
│       ├── MoneyToTextPage.xaml
│       ├── SpeedTestPage.xaml
│       ├── ClipboardHistoryPage.xaml
│       ├── PhotoCollagePage.xaml      # Resim kolaj oluşturucu
│       └── TaxCalculatorPage.xaml     # Vergi hesaplayıcı (13 tab)
├── Resources/                  # Kaynaklar
│   ├── Icons/
│   └── Themes/
├── Services/                   # Servisler (iş mantığı katmanı)
│   ├── ClipboardHistoryService.cs    # Pano geçmişi
│   ├── ColorPickerService.cs         # Renk seçici yardımcıları
│   ├── DataAnalysisService.cs        # CSV/Excel/JSON analiz
│   ├── FileManagerService.cs         # Şifreleme, diff, yeniden adlandırma
│   ├── ImageConverterService.cs      # Görsel dönüşüm ve filtreler
│   ├── JsonXmlFormatterService.cs    # JSON/XML formatlama ve dönüşüm
│   ├── MoneyToTextService.cs         # Para -> yazı dönüşümü (HBMoneyToWords)
│   ├── TaxCalculationService.cs      # Vergi hesaplama servisi
│   └── TaxRatesScraperService.cs     # Web scraping servisi
└── Data/                       # Veri dosyaları
    └── tax-rates.json          # Vergi oranları cache (2024-2025)

```

### Yeni Modül Ekleme

1. **XAML Sayfası Oluştur**
   ```xaml
   <Page x:Class="SwissKnifeApp.Views.Modules.YeniModulPage"
         xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
         xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
       <!-- UI tasarımı -->
   </Page>
   ```

2. **Code-Behind Ekle**
   ```csharp
   public partial class YeniModulPage : Page
   {
       public YeniModulPage()
       {
           InitializeComponent();
       }
   }
   ```

3. **MainWindow'a Ekle**
   ```csharp
   case "YeniModul":
       MainFrame.Navigate(new YeniModulPage());
       break;
   ```

4. **Menü Butonu Ekle**
   ```xaml
   <Button Tag="YeniModul" Click="MenuButton_Click">
       <StackPanel>
           <iconPacks:PackIconMaterial Kind="Icon" />
           <TextBlock Text="Yeni Modül"/>
       </StackPanel>
   </Button>
   ```

### Debug
```bash
dotnet run --configuration Debug
```

### Test
```bash
dotnet test
```

## 🤝 Katkıda Bulunma

1. Fork yapın
2. Feature branch oluşturun (`git checkout -b feature/YeniOzellik`)
3. Değişikliklerinizi commit edin (`git commit -m 'Yeni özellik eklendi'`)
4. Branch'inizi push edin (`git push origin feature/YeniOzellik`)
5. Pull Request oluşturun

### Kod Standartları
- C# Coding Conventions'a uyun
- XAML için tutarlı indentation kullanın
- Türkçe yorum ve değişken isimleri tercih edin
- Her modül için ayrı dosya kullanın

## 📝 Yapılacaklar

- [ ] Tema değiştirme (Light/Dark mode)
- [ ] Çoklu dil desteği (İngilizce)
- [ ] Ayarlar sayfası
- [ ] Favori modüller
- [ ] Dışa/İçe aktarma (Settings export/import)
- [ ] Otomatik güncelleme
- [ ] Markdown önizleme
- [ ] Regex test aracı
- [ ] Base conversion (2, 8, 10, 16)
- [x] Resim kolaj oluşturucu (ColorPicker, metin ekleme)
- [x] Vergi hesaplayıcı (13 vergi türü, web scraping)
- [ ] Resim kolaj - Fotoğraf sürükle-bırak ile sıralama

## 🐛 Bilinen Sorunlar

- ImageSharp kütüphanesinde güvenlik uyarıları (WebP ile ilgili)
- SVG to PNG dönüşümünde bazı karmaşık SVG'ler sorun yaratabilir
- ICO dönüşümü maksimum 256x256 boyutlarla sınırlı
- Resim kolaj - Köşe yuvarlatma Border'larda tam uygulanmıyor
- Vergi hesaplayıcı - Web scraping HTML parsing iyileştirilebilir (şu an fallback değerler kullanılıyor)

## 📄 Lisans

Bu proje [MIT Lisansı](LICENSE) altında lisanslanmıştır.

## 👨‍💻 Geliştirici

**SwissKnifeApp** - Çok amaçlı araç kutusu uygulaması

## 📞 İletişim

- GitHub: [Proje Sayfası](https://github.com/kullaniciadi/SwissKnifeApp)
- E-posta: kullanici@example.com

## 🙏 Teşekkürler

- MahApps.Metro ekibine modern UI için
- SixLabors ekibine ImageSharp için
- Tüm açık kaynak kütüphane geliştiricilerine

---

**Not**: Bu uygulama aktif geliştirme aşamasındadır. Yeni özellikler ve iyileştirmeler düzenli olarak eklenmektedir.

**Versiyon**: 2.0.0  
**Son Güncelleme**: 17 Ekim 2025

## 🎉 Son Güncelleme (v2.0.0 - 17 Ekim 2025)

### Yeni Özellikler
- ✨ **Resim Kolaj Oluşturucu** eklendi (1-20 fotoğraf, özelleştirilebilir şablonlar)
- ✨ **Vergi Hesaplayıcı** eklendi (13 farklı vergi türü, web scraping desteği)
- 🎨 ColorPicker entegrasyonu (kolaj ve vergi modüllerinde)
- 📊 JSON tabanlı vergi oranları cache sistemi
- 🌐 HtmlAgilityPack ile web scraping altyapısı

### İyileştirmeler
- 🖼️ PDF servisleri genişletildi
- 📝 Geliştirilmiş dosya yönetimi
- 🎯 Kullanıcı arayüzü iyileştirmeleri
- 📚 README detaylandırıldı

### Refaktör (Hizmet Katmanına Taşıma)
- MoneyToTextPage modülündeki tüm iş mantığı `MoneyToTextService`'e taşındı.
- JsonXmlFormatterPage, ClipboardHistoryPage, ColorPickerPage, DataAnalysisPage modülleri servis katmanını kullanacak şekilde güncellendi.
- UI event handler'ları sadeleştirildi; servisler test edilebilir hale getirildi.

### Teknik Detaylar
- 17 yeni model class (TaxRateModels.cs)
- 2 yeni servis (TaxCalculationService, TaxRatesScraperService)
- 2 yeni sayfa (PhotoCollagePage, TaxCalculatorPage)
- 350+ satır JSON vergi verisi
- 1000+ satır XAML ve C# kodu eklendi

⭐ Beğendiyseniz yıldız vermeyi unutmayın!
