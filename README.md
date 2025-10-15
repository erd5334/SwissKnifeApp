# 🔧 SwissKnifeApp - Çok Amaçlı Araç Kutusu

SwissKnifeApp, günlük işlerinizi kolaylaştırmak için tasarlanmış, modern ve kullanıcı dostu bir masaüstü uygulamasıdır. WPF (.NET 8) ile geliştirilmiş bu uygulama, çeşitli araçları tek bir çatı altında toplar.

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
   ```

2. **Bağımlılıkları Yükle**
   ```bash
   dotnet restore
   ```

3. **Projeyi Derle**
   ```bash
   dotnet build
   ```

4. **Uygulamayı Çalıştır**
   ```bash
   dotnet run
   ```

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
│   └── ClipboardItem.cs
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
│       └── ClipboardHistoryPage.xaml
├── Resources/                  # Kaynaklar
│   ├── Icons/
│   └── Themes/
└── Services/                   # Servisler

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
- [ ] Color picker
- [ ] Base conversion (2, 8, 10, 16)

## 🐛 Bilinen Sorunlar

- ImageSharp kütüphanesinde güvenlik uyarıları (WebP ile ilgili)
- SVG to PNG dönüşümünde bazı karmaşık SVG'ler sorun yaratabilir
- ICO dönüşümü maksimum 256x256 boyutlarla sınırlı

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

**Versiyon**: 1.0.0  
**Son Güncelleme**: 15 Ekim 2025

⭐ Beğendiyseniz yıldız vermeyi unutmayın!
