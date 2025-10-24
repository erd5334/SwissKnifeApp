# 🔧 Türk Çakısı - Özellikler ve Modüller

Bu doküman, Türk Çakısı uygulamasındaki tüm modüllerin detaylı özelliklerini içerir.

## 📋 İçindekiler
1. [Metin İşlemleri](#-metin-işlemleri)
2. [Şifre ve Güvenlik Araçları](#-şifre-ve-güvenlik-araçları)
3. [QR Kod & Barkod Araçları](#-qr-kod--barkod-araçları)
4. [Görsel Dönüştürücü](#-görsel-dönüştürücü)
5. [PDF İşlemleri](#-pdf-işlemleri)
6. [Birim Dönüştürücü](#-birim-dönüştürücü)
7. [JSON & XML Formatter](#-json--xml-formatter)
8. [Para Yazıya Çevirme](#-para-yazıya-çevirme)
9. [İnternet Hız Testi](#-i̇nternet-hız-testi)
10. [Pano Geçmişi](#-pano-geçmişi)
11. [Resim Kolaj](#-resim-kolaj)
12. [Vergi Hesaplayıcı](#-vergi-hesaplayıcı)
13. [YouTube Klip İndirici](#-youtube-klip-i̇ndirici)
14. [Ses Araçları](#-ses-araçları)
15. [Video Araçları](#-video-araçları)
16. [Veri Analizi](#-veri-analizi)
17. [Dosya Yöneticisi](#-dosya-yöneticisi)

---

## 📝 Metin İşlemleri

### Temel Dönüşümler
- **Büyük Harf**: Tüm metni büyük harfe çevir (ÖRNEK METİN)
- **Küçük Harf**: Tüm metni küçük harfe çevir (örnek metin)
- **Başlık Harf**: Her kelimenin ilk harfini büyük yap (Örnek Metin)
- **Ters Çevir**: Metni tersten yaz (niteM kelpmÖ)
- **Cümle Başları**: Her cümlenin ilk harfini büyük yap

### Kodlama ve Şifreleme
- **URL Encode**: URL'de kullanılmak üzere özel karakterleri kodla
- **URL Decode**: Kodlanmış URL'leri orijinal haline çevir
- **Base64 Encode**: Metni Base64 formatına dönüştür
- **Base64 Decode**: Base64 kodunu çöz
- **HTML Encode**: HTML özel karakterlerini kodla (&lt;, &gt;, &amp;)
- **HTML Decode**: HTML kodlarını çöz

### Metin Analizi
- **Kelime Sayısı**: Metindeki toplam kelime sayısını say
- **Karakter Sayısı**: Toplam karakter sayısı (boşluklu/boşluksuz)
- **Satır Sayısı**: Toplam satır sayısı
- **Benzersiz Kelime**: Tekrar etmeyen kelime sayısı

### Satır İşlemleri
- **Alfabetik Sıralama**: Satırları A-Z sırala
- **Ters Alfabetik Sıralama**: Satırları Z-A sırala
- **Tekrar Eden Satırları Kaldır**: Duplicate satırları temizle
- **Satır Numaralandırma**: Her satırın başına numara ekle (1., 2., 3.)
- **Boş Satırları Kaldır**: Boş satırları temizle

### Boşluk İşlemleri
- **Fazla Boşlukları Kaldır**: Çift boşlukları tek boşluğa çevir
- **Baş/Son Boşlukları Kaldır**: Satır başı ve sonu boşluklarını temizle
- **Tüm Boşlukları Kaldır**: Metindeki tüm boşlukları sil

---

## 🔐 Şifre ve Güvenlik Araçları

### Şifre Üretici
- **Uzunluk Ayarı**: 4-128 karakter arası şifre uzunluğu seçimi
- **Karakter Türleri**:
  - Büyük harfler (A-Z)
  - Küçük harfler (a-z)
  - Sayılar (0-9)
  - Özel karakterler (!@#$%^&*()_+-=[]{}|;:,.<>?)
- **Okunabilir Şifre**: Karıştırılabilecek karakterleri (0/O, 1/l/I) hariç tut
- **Toplu Üretim**: Aynı anda birden fazla şifre üret
- **Güvenlik Seviyesi**: Şifre gücünü görsel olarak göster

### Hash Üretici
- **MD5**: 128-bit hash değeri
- **SHA1**: 160-bit hash değeri
- **SHA256**: 256-bit hash değeri
- **SHA512**: 512-bit hash değeri
- **Büyük/Küçük Harf**: Hash çıktısını büyük veya küçük harf olarak al

### AES Şifreleme
- **Şifreleme**: Metni AES-256 algoritması ile şifrele
- **Şifre Çözme**: Şifrelenmiş metni orijinal haline çevir
- **Anahtar Koruması**: Güçlü şifre anahtarı kullanımı
- **Base64 Çıktı**: Şifrelenmiş veri Base64 formatında

---

## 📊 QR Kod & Barkod Araçları

### QR Kod Oluşturma
- **Metin QR**: Düz metin için QR kod
- **URL QR**: Web adresleri için QR kod
- **WiFi QR**: WiFi ağ bilgileri (SSID, Şifre, Güvenlik)
- **E-posta QR**: E-posta adresi, konu, mesaj
- **SMS QR**: Telefon numarası ve mesaj
- **vCard QR**: Kişi bilgileri (Ad, Telefon, E-posta, Adres)

### QR Kod Ayarları
- **Boyut**: 100x100'den 1000x1000 piksel arası
- **Hata Düzeltme**: Low, Medium, Quartile, High
- **Format**: PNG
- **Renk**: Siyah-beyaz veya özel renkler

### Barkod Oluşturma
- **CODE_128**: Alfanümerik barkod (en yaygın)
- **EAN_13**: 13 haneli ürün barkodu
- **UPC_A**: 12 haneli ürün barkodu
- **Boyut Ayarı**: Genişlik ve yükseklik ayarı
- **Format**: PNG

### QR/Barkod Okuma
- **Görsel Yükleme**: PNG, JPG, BMP dosyalarından okuma
- **Otomatik Algılama**: QR kod ve barkodları otomatik tanı
- **Sonuç Gösterimi**: Okunan veriyi göster ve kopyala

---

## 🖼️ Görsel Dönüştürücü

### Format Dönüşümü
- **Desteklenen Formatlar**:
  - JPG/JPEG: Fotoğraflar için optimize
  - PNG: Şeffaflık desteği
  - BMP: Windows bitmap
  - GIF: Animasyonlu görsel (ilk kare)
  - WEBP: Modern web formatı
  - ICO: Windows icon dosyası
  - SVG: Vektörel grafik (PNG'den SVG'ye ve SVG'den PNG'ye)
- **Toplu Dönüşüm**: Birden fazla dosyayı aynı anda dönüştür
- **Format Algılama**: Otomatik kaynak format tespiti

### Yeniden Boyutlandırma
- **Özel Boyut**: İstenilen genişlik ve yükseklik (px)
- **Yüzde Oranı**: %10-%200 arası oransal boyutlandırma
- **En-Boy Oranı**: Oranı koru veya serbest boyutlandır
- **Kalite Seçimi**: Hızlı veya yüksek kalite

### Filtreler
- **Gri Tonlama**: Renkleri griye çevir
- **Renk Tersine Çevirme**: Negatif efekt
- **Siyah/Beyaz**: Tam kontrast
- **Sepia**: Nostaljik kahverengi ton
- **Bulanıklaştırma**: Blur efekti

### Ayarlamalar
- **Parlaklık**: -100 ile +100 arası
- **Kontrast**: -100 ile +100 arası
- **Doygunluk**: Renk yoğunluğu ayarı
- **Kalite**: JPG için 0-100 arası sıkıştırma

### Özellikler
- **Sürükle-Bırak**: Dosya ve klasörleri direkt sürükle
- **Canlı Önizleme**: Değişiklikleri anında gör
- **Toplu İşlem**: Birden fazla dosyayı işle
- **Progress Bar**: İşlem ilerlemesini takip et

---

## 📄 PDF İşlemleri

### PDF Birleştirme
- **Çoklu Dosya**: İstediğiniz kadar PDF dosyası ekle
- **Sıralama**: Dosyaların sırasını değiştir
- **Önizleme**: Birleştirme öncesi kontrol
- **Tek Dosya Çıktı**: Tüm PDF'leri tek dosyada birleştir

### PDF Bölme
- **Sayfa Aralığı**: Başlangıç ve bitiş sayfası seç
- **Tekli Sayfa**: Sadece bir sayfayı çıkar
- **Çoklu Bölüm**: Birden fazla aralık belirle
- **Önizleme**: Bölme öncesi sayfa kontrolü

### Görüntüden PDF
- **Desteklenen Formatlar**: JPG, PNG, BMP, GIF, TIFF
- **Çoklu Görsel**: Birden fazla görseli tek PDF'te topla
- **Sayfa Boyutu**: A4, Letter, Custom
- **Sıralama**: Görsellerin PDF'teki sırası
- **Kalite**: Görsel sıkıştırma ayarı

### Özellikler
- **Drag & Drop**: Dosyaları sürükle bırak
- **İlerleme Takibi**: İşlem durumunu görüntüle
- **Hata Yönetimi**: Sorunlu dosyalar için uyarı
- **Hızlı İşlem**: Optimize edilmiş PDF işleme

---

## 🔢 Birim Dönüştürücü

### Uzunluk Birimleri
- **Milimetre (mm)**: 1mm = 0.001m
- **Santimetre (cm)**: 1cm = 0.01m
- **Metre (m)**: Temel birim
- **Kilometre (km)**: 1km = 1000m
- **İnç (in)**: 1in = 2.54cm
- **Fit (ft)**: 1ft = 30.48cm
- **Yarda (yd)**: 1yd = 91.44cm
- **Mil (mi)**: 1mi = 1.609km

### Ağırlık Birimleri
- **Miligram (mg)**: 1mg = 0.001g
- **Gram (g)**: Temel birim
- **Kilogram (kg)**: 1kg = 1000g
- **Ton**: 1ton = 1000kg
- **Ons (oz)**: 1oz = 28.35g
- **Pound (lb)**: 1lb = 453.59g

### Sıcaklık Birimleri
- **Celsius (°C)**: Su donma noktası 0°C
- **Fahrenheit (°F)**: Su donma noktası 32°F
- **Kelvin (K)**: Mutlak sıfır 0K
- **Dönüşüm Formülleri**: Otomatik hesaplama

### Hız Birimleri
- **Metre/Saniye (m/s)**: SI birimi
- **Kilometre/Saat (km/h)**: Yaygın kullanım
- **Mil/Saat (mph)**: İngiliz sistemi
- **Knot**: Denizcilik birimi

### Alan Birimleri
- **Metrekare (m²)**: Temel alan birimi
- **Kilometrekare (km²)**: 1km² = 1,000,000m²
- **Hektar (ha)**: 1ha = 10,000m²
- **Dönüm**: Türkiye'de kullanılan (1 dönüm = 1000m²)
- **Akre**: 1acre = 4047m²

### Özellikler
- **Çift Yönlü**: Herhangi iki birim arası dönüşüm
- **Gerçek Zamanlı**: Anında hesaplama
- **Hassasiyet**: 6 ondalık basamak
- **Temizleme**: Tek tıkla sıfırla

---

## 🌐 JSON & XML Formatter

### JSON İşlemleri
- **Format (Beautify)**: JSON'u okunabilir hale getir
- **Minify**: JSON'u sıkıştır (boşlukları kaldır)
- **Söz Dizimi Kontrolü**: Geçersiz JSON'u tespit et
- **Hata Gösterimi**: Satır numarası ile hata bildirimi
- **Renklendirme**: Syntax highlighting (anahtar, değer, string)
- **Girinti Ayarı**: 2 veya 4 boşluk

### XML İşlemleri
- **Format (Beautify)**: XML'i okunabilir hale getir
- **Minify**: XML'i sıkıştır
- **Söz Dizimi Kontrolü**: Geçersiz XML'i tespit et
- **Hata Gösterimi**: Detaylı hata mesajları
- **Renklendirme**: Tag, attribute, value vurgulama
- **Girinti Ayarı**: Özelleştirilebilir girinti

### JSON ↔ XML Dönüşümü
- **JSON'dan XML'e**: Otomatik dönüşüm
- **XML'den JSON'a**: Otomatik dönüşüm
- **Yapı Koruması**: Element ve attribute eşleştirme
- **Hata Yönetimi**: Uyumsuz yapılar için uyarı

### Editör Özellikleri
- **Satır Numaraları**: Kod satırlarını takip et
- **Kod Katlama**: Blokları daralt/genişlet
- **Arama/Değiştir**: Metin içinde ara ve değiştir
- **Kopyala/Yapıştır**: Hızlı işlem
- **Temizle**: Tek tıkla editörü sıfırla

---

## 💸 Para Yazıya Çevirme

### Desteklenen Para Birimleri
- **Türk Lirası (TRY)**: Lira, Kuruş
- **Amerikan Doları (USD)**: Dolar, Sent
- **Euro (EUR)**: Euro, Sent
- **İngiliz Sterlini (GBP)**: Pound, Peni
- **Rus Rublesi (RUB)**: Ruble, Kopek

### Dil Seçenekleri
- **Türkçe**: Tam Türkçe yazım
  - Örnek: "Bin Yüz Yirmi Üç Türk Lirası Kırk Beş Kuruş"
- **İngilizce**: Tam İngilizce yazım
  - Örnek: "One Thousand One Hundred Twenty-Three Turkish Lira and Forty-Five Kurus"

### Format Seçenekleri
- **Harf Boyutu**:
  - BÜYÜK HARF: TÜM METİN BÜYÜK
  - küçük harf: tüm metin küçük
  - İlk Harf Büyük: Her Kelimenin İlk Harfi Büyük
- **Boşluk Modu**:
  - Boşluklu: Normal yazım
  - Boşluksuz: Tüm boşlukları kaldır
- **Ayraç Belirleme**: Kelimeler arası özel ayraç (-_+#)
- **İlk Harf**: Sadece ilk harfi büyük yap

### Özellikler
- **Gerçek Zamanlı**: Sayı girerken anında çevir
- **Kopyala**: Sonucu tek tıkla kopyala
- **Ondalık Destek**: Kuruş/sent desteği
- **Büyük Sayılar**: Milyarlara kadar destek
- **HBMoneyToWords**: Profesyonel kütüphane kullanımı

---

## 🌐 İnternet Hız Testi

### Hız Ölçümleri
- **İndirme Hızı (Download)**:
  - Gerçek zamanlı ölçüm (Mbps)
  - Minimum, maksimum, ortalama değerler
  - Canlı grafik gösterimi
- **Yükleme Hızı (Upload)**:
  - Gerçek zamanlı ölçüm (Mbps)
  - Minimum, maksimum, ortalama değerler
  - Canlı grafik gösterimi
- **Ping (Gecikme)**:
  - Sunucu yanıt süresi (ms)
  - Ortalama ping değeri
  - Bağlantı kalitesi göstergesi

### Grafik Özellikleri
- **Canlı Grafik**: Hız değişimlerini anlık izle
- **Zaman Ekseni**: Saniye bazında ölçüm
- **Çift Eksen**: İndirme ve yükleme ayrı çizgiler
- **Renkli Gösterim**: Kolay ayırt edilebilir
- **Zoom**: Grafik üzerinde yakınlaştırma

### Test Özellikleri
- **Sunucu Seçimi**: Farklı sunuculara test
- **Çoklu Test**: Art arda testler yap
- **Geçmiş**: Önceki test sonuçlarını görüntüle
- **İptal**: Testi istediğin zaman durdur
- **Paylaş**: Sonuçları kopyala

---

## 📋 Pano Geçmişi

### Otomatik Kayıt
- **Metin Kayıt**: Kopyalanan tüm metinler
- **Görsel Kayıt**: Ekran görüntüleri ve resimler
- **Format Algılama**: Otomatik içerik tipi tespiti
- **Zaman Damgası**: Her kayıt için tarih/saat
- **Sıralama**: En yeni en üstte

### Önizleme
- **Metin Önizleme**: İlk 100 karakter
- **Görsel Önizleme**: Küçük thumbnail
- **Tip Göstergesi**: İkon ile içerik tipi
- **Tarih Bilgisi**: Ne zaman kopyalandı

### İşlemler
- **Yeniden Kullan**: Tek tıkla panoya kopyala
- **Sil**: İstenmeyen kayıtları sil
- **Tümünü Temizle**: Tüm geçmişi sil
- **Arama**: Metin içinde ara
- **Filtre**: Tip bazında filtrele (Metin/Görsel)

### Ayarlar
- **Otomatik Başlat**: Uygulama açılışında aktif
- **Maksimum Kayıt**: Kaç kayıt saklanacak
- **Bildirim**: Yeni kayıt bildirimi
- **Gizlilik**: Hassas verileri kaydetme

---

## 🖼️ Resim Kolaj

### Kolaj Oluşturma
- **Fotoğraf Sayısı**: 1-20 arası fotoğraf
- **Otomatik Grid**: Fotoğraf sayısına göre düzen
  - 2 fotoğraf: 1x2
  - 3-4 fotoğraf: 2x2
  - 5-9 fotoğraf: 3x3
  - 10-16 fotoğraf: 4x4
  - 17-20 fotoğraf: 5x4
- **Sürükle-Bırak**: Fotoğrafları direkt ekle
- **Sıralama**: Fotoğraf sırasını değiştir

### Kenarlık Ayarları
- **Kalınlık**: 0-50 piksel arası
- **Renk Seçimi**: ColorPicker ile renk seç
- **Stil**: Düz, yuvarlak köşe
- **Gölge**: İsteğe bağlı gölge efekti

### Arka Plan
- **Renk**: Tek renk arka plan (ColorPicker)
- **Şeffaflık**: Alpha kanal desteği
- **Köşe Yuvarlatma**: 0-50px arası
- **Fotoğraf Arası Boşluk**: 0-50px arası

### Metin Ekleme
- **Metin İçeriği**: Kolaj üzerine yazı
- **Pozisyon Seçimi**:
  - Yatay: Sol, Orta, Sağ
  - Dikey: Üst, Orta, Alt
- **Yazı Rengi**: ColorPicker ile seç
- **Font Boyutu**: 12-72pt arası
- **Yazı Tipi**: Arial, Times, Courier vb.

### Önizleme ve Kayıt
- **Canlı Önizleme**: Değişiklikleri anında gör
- **Format Seçimi**: PNG veya JPG
- **Kalite**: JPG için sıkıştırma oranı
- **Boyut**: Çıktı boyutu ayarı
- **Kaydet**: İstenilen konuma kaydet

---

## 💰 Vergi Hesaplayıcı

### 1. Gelir Vergisi
- **Dilim Hesaplama**: 5 dilimli artan oranlı vergi
- **Ücret/Ücret Dışı**: Farklı oran seçenekleri
- **2024-2025 Oranları**: Güncel dilim ve oranlar
- **Detaylı Rapor**: 
  - Her dilim için ayrı hesaplama
  - Matrah, vergi, net tutar
  - Toplam vergi yükü

### 2. KDV Hesaplama
- **KDV Dahil/Hariç**: İki yönlü hesaplama
- **5 Farklı Oran**: %1, %8, %10, %18, %20
- **Otomatik Hesaplama**: Gerçek zamanlı sonuç
- **Detaylı Gösterim**: KDV tutarı ve net tutar

### 3. Kurumlar Vergisi
- **Standart Oran**: %25
- **Finans Sektörü**: %30
- **Matrah Hesabı**: Karlılık üzerinden
- **Ödenecek Vergi**: Net vergi tutarı

### 4. Kira Gelir Vergisi
- **Yıllık/Aylık**: Her iki hesaplama
- **İstisna Tutarı**: 2024 için 14,000 TL
- **Artan Oranlı**: Dilim bazlı hesaplama
- **Net Gelir**: Vergi sonrası kazanç

### 5. Damga Vergisi
- **Belge Değeri**: İşlem tutarı girişi
- **Sabit Oran**: ‰0.948 (binde 0.948)
- **Hesaplama**: Otomatik vergi tutarı
- **Kullanım Alanları**: Sözleşme, protokol, dekont

### 6. MTV (Motorlu Taşıtlar Vergisi)
- **Araç Tipi**: Otomobil, Motosiklet
- **Motor Hacmi**: cc bazlı hesaplama
- **Yaş Katsayısı**: Araç yaşına göre indirim
- **Taksit**: 2 taksit seçeneği
- **Özel Oranlar**: 
  - 1-1300cc, 1301-1600cc, 1601-1800cc, 1801-2000cc, 2001-2500cc, 2501-3000cc, 3001-3500cc, 3501-4000cc, 4001+cc
  - Motosiklet: 100-250cc, 251-650cc, 651-1200cc, 1201+cc

### 7. KDV Tevkifatı
- **9 Hizmet Kategorisi**:
  - Makine, Teçhizat, Demirbaş (%50, %90)
  - Hurda ve Atık Teslimi (%90)
  - Bakır, Çinko, Alüminyum, Kurşun (%90)
  - İstisnadan Vazgeçenler (%50, %90)
  - Bakım Onarım (%50)
  - Tekstil ve Konfeksiyon (%50)
  - Makine Halı (%50)
  - İşlenmiş veya İşlenmemiş Et (%50)
  - Pamuk, Tiftik, Yün ve Yapağı (%50)
- **Hesaplama**: Tevkifat oranı ve tutarı
- **Net KDV**: Ödenecek net vergi

### 8. Değer Artış Kazancı
- **Gayrimenkul**: Arsa, bina değer artışı
- **Menkul Değer**: Hisse senedi kazancı
- **İstisna**: %50 istisna uygulaması
- **Hesaplama**: Alış-satış farkı üzerinden

### 9. Değerli Konut Vergisi
- **Lüks Konut**: 12.5M TL üzeri konutlar
- **Artan Oranlı**: 4 dilim (%0.3-1%)
- **Değer Aralıkları**: 
  - 12.5M-25M TL: %0.3
  - 25M-50M TL: %0.6
  - 50M-100M TL: %0.9
  - 100M+ TL: %1.0
- **Yıllık Vergi**: Tapu değeri üzerinden

### 10. Emlak Vergisi
- **Bina Vergisi**: İkamet, işyeri ayrımı
- **Arazi Vergisi**: Tarım, imarlı ayrımı
- **Oranlar**: 
  - Bina (İkamet): %0.2
  - Bina (İşyeri): %0.4
  - Arazi (Tarım): %0.1
  - Arazi (İmar): %0.6
- **Taksit**: 2 eşit taksit

### 11. ÖTV (Akaryakıt)
- **Akaryakıt Türleri**: 
  - Benzin (95, 97 oktan)
  - Motorin
  - LPG
  - Fuel Oil
- **Güncel Oranlar**: 2024-2025 ÖTV oranları
- **Hesaplama**: Litre/tutar bazlı
- **Web Scraping**: Otomatik oran güncelleme

### 12. Veraset ve İntikal Vergisi
- **Miras Türü**: Vasiyetname, vasiyet dışı
- **Mirasçı Tipi**: 
  - Eş ve çocuklar (%1-10)
  - Diğer mirasçılar (%10-30)
- **Dilim Bazlı**: 4 dilim artan oranlı
- **İstisna**: Yasal istisna tutarları

### 13. Vergi Gecikme Faizi
- **Gün Hesabı**: Gecikme gün sayısı
- **Aylık Faiz**: TCMB oranları
- **Gecikmeli Ödeme**: Otomatik faiz hesabı
- **Toplam Borç**: Anapara + faiz

### Genel Özellikler
- **JSON Cache**: Vergi oranları yerel cache
- **Otomatik Güncelleme**: Web scraping ile oran güncellemesi
- **Validasyon**: Girdi kontrolü ve hata mesajları
- **Detaylı Rapor**: Dilim bazlı hesaplama gösterimi
- **Temizleme**: Tek tıkla formu sıfırla
- **Kopyalama**: Sonuçları kopyala

---

## 🎥 YouTube Klip İndirici

### İndirme Modları
- **TXT Dosyası**: Toplu kesit listesi
- **Manuel Giriş**: Tek tek aralık girişi
- **Tam Video**: Tüm videoyu indir

### Video Ayarları
- **Kalite**: En iyi kalite (1080p'ye kadar)
- **Format**: MP4 (otomatik birleştirme)
- **Codec**: H.264 (evrensel uyumluluk)
- **Ses**: AAC ses codec'i

### 🎵 Ses İndirme (YENİ!)
- **7 Format Desteği**:
  - MP3: 320kbps (evrensel)
  - WAV: Kayıpsız (yüksek kalite)
  - FLAC: Kayıpsız sıkıştırılmış
  - M4A: AAC container (Apple)
  - OGG: Vorbis codec (açık kaynak)
  - OPUS: Modern düşük bitrate
  - AAC: M4A ile aynı, farklı container
- **En İyi Kalite**: --audio-quality 0
- **Otomatik Dönüşüm**: Video'dan ses çıkarma

### 🍪 Cookie Desteği (YENİ!)
- **Yaş Sınırlı İçerik**: 18+ videolar
- **Premium İçerik**: YouTube Premium videoları
- **Özel Videolar**: Gizlilik ayarlı videolar
- **Tarayıcı Cookie**: Giriş bilgilerinizi kullan
- **Format**: Netscape cookie.txt formatı

### 🤖 Anti-Bot Koruması (YENİ!)
- **Android Client**: `player_client=android,web`
- **User-Agent**: Android app simülasyonu
- **403 Bypass**: YouTube bot algılamayı aş
- **Stabilite**: Daha güvenilir indirme

### 📚 Cookie Yardım Penceresi (YENİ!)
- **Adım Adım Rehber**: Cookie nasıl alınır
- **Tarayıcı Eklentileri**:
  - Chrome/Edge: "Get cookies.txt LOCALLY"
  - Firefox: "cookies.txt"
- **Direkt Linkler**: Chrome Web Store ve Firefox Add-ons
- **Güvenlik Uyarıları**: Cookie güvenliği
- **Kullanım Talimatları**: Programa nasıl eklenir

### Kesit Özellikleri
- **Zaman Formatı**: HH:MM:SS veya saniye
- **Çoklu Kesit**: Tek seferde birden fazla
- **Temiz Kesme**: Keyframe tabanlı kesme
- **İsim Belirleme**: Her kesit için özel isim

### TXT Format
```
Video URL
Başlangıç-Bitiş İsim
00:01:30-00:03:45 Giriş Sahnesi
120-225 İkinci Kesit
```

### Araçlar
- **yt-dlp**: YouTube indirici
- **ffmpeg**: Video/ses işleme
- **Otomatik Kurulum**: "Araçları Kur" butonu
- **Portable**: Tools klasöründen çalış

### İlerleme Takibi
- **Toplam İlerleme**: Tüm kısımlar
- **Parça İlerleme**: Mevcut kesit
- **Log Gösterimi**: Detaylı işlem bilgisi
- **İptal**: İstediğiniz zaman durdur

---

## 🎵 Ses Araçları

### Dönüştürme
- **7 Format**:
  - MP3: 320kbps (evrensel uyumluluk)
  - AAC: 256kbps (Apple uyumlu)
  - WAV: Kayıpsız PCM (profesyonel)
  - FLAC: Kayıpsız sıkıştırma (audiophile)
  - OPUS: 192kbps (modern, düşük boyut)
  - M4A: AAC container (iTunes)
  - OGG: Vorbis codec (açık kaynak)
- **Toplu Dönüşüm**: Birden fazla dosya
- **Kalite Seçimi**: Highest, High, Medium, Low, Lossless

### Kalite Presetleri
- **Highest**: MP3 320kbps, AAC 256kbps, OPUS 192kbps
- **High**: MP3 256kbps, AAC 192kbps, OPUS 128kbps
- **Medium**: MP3 192kbps, AAC 128kbps, OPUS 96kbps
- **Low**: MP3 128kbps, AAC 96kbps, OPUS 64kbps
- **Lossless**: WAV/FLAC kayıpsız (sadece bu formatlar için)

### Kesme (Trim)
- **Başlangıç Zamanı**: HH:MM:SS formatı
- **Bitiş Zamanı**: HH:MM:SS formatı
- **Önizleme**: Ses süresini görüntüle
- **Hassas Kesim**: Milisaniye hassasiyeti

### Ses Normalizasyonu
- **Loudnorm Filtresi**: EBU R128 standardı
- **Otomatik Seviye**: Tüm dosyalarda aynı ses
- **Dinamik Koruma**: Kaliteyi koru
- **İsteğe Bağlı**: Açık/kapalı seçeneği

### Özellikler
- **ffmpeg**: Profesyonel ses işleme
- **ffprobe**: Otomatik süre tespiti
- **İlerleme**: Gerçek zamanlı işlem durumu
- **Portable**: Tools klasöründen çalış
- **Batch İşlem**: Klasör dönüşümü

---

## 🎬 Video Araçları

### 3 Mod (YENİ!)
1. **Dönüştür**: Video format değiştirme
2. **Ses Çıkar**: Videodan ses dosyası (YENİ!)
3. **Trim/Kırp**: Video kesme ve kırpma

### 🎵 Ses Çıkarma (YENİ!)
- **7 Ses Formatı**: MP3, AAC, WAV, FLAC, OPUS, M4A, OGG
- **Codec Bazlı Bitrate**:
  - MP3: libmp3lame (96k-320k)
  - AAC: aac (96k-256k)
  - WAV: pcm_s16le (kayıpsız)
  - FLAC: flac (sıkıştırma 0-8)
  - OPUS: libopus (64k-192k)
  - M4A: aac (96k-256k)
  - OGG: libvorbis (96k-256k)
- **Kalite Presetleri**: 
  - Highest: En yüksek bitrate
  - High: Yüksek kalite
  - Medium: Dengeli
  - Low: Düşük boyut
  - Lossless: WAV/FLAC için kayıpsız

### Video Dönüştürme
- **7 Format**: MP4, MKV, WEBM, MOV, TS, AVI, FLV
- **3 Codec**: 
  - H.264 (x264): Evrensel uyumluluk
  - H.265 (x265/HEVC): Yüksek sıkıştırma
  - VP9: WebM için modern codec
- **6 Çözünürlük**: Original, 2160p (4K), 1440p, 1080p, 720p, 480p

### Kalite Ayarları
- **CRF Tabanlı**:
  - Highest: CRF 18 (en iyi kalite)
  - High: CRF 20
  - Medium: CRF 23 (dengeli)
  - Low: CRF 28
  - Lossless: CRF 0 (kayıpsız)
- **Preset**: Fast encoding

### Trim (Kesme)
- **Başlangıç/Bitiş**: HH:MM:SS formatı
- **Süre Tespiti**: ffprobe ile otomatik
- **Hassas Kesim**: Keyframe tabanlı
- **Copy Codec**: Kayıpsız kesme

### Crop (Kırpma)
- **Boyut**: Genişlik x Yükseklik
- **Pozisyon**: X ve Y koordinatları
- **Önizleme**: Manuel hesaplama
- **Merkez**: Otomatik merkezleme

### 🎨 Dinamik UI (YENİ!)
- **Mod Bazlı**: Seçilen moda göre UI değişir
- **Video Modu**: Format, codec, çözünürlük göster
- **Ses Modu**: Sadece ses formatları, codec/çözünürlük gizle
- **Otomatik Güncelleme**: ComboBox'lar dinamik doldurulur

### Özellikler
- **Toplu İşlem**: Birden fazla video
- **İlerleme Takibi**: Gerçek zamanlı durum
- **Uyumluluk Kontrolü**: Format-codec eşleştirme
- **Pixel Format**: yuv420p (evrensel)
- **MP4 Optimizasyonu**: -movflags +faststart
- **HEVC Tag**: -tag:v hvc1 (Apple uyumluluğu)

---

## 📊 Veri Analizi

### Veri Yükleme
- **CSV Dosyası**: Virgülle ayrılmış değerler
- **Excel Dosyası**: .xlsx formatı
- **Sürükle-Bırak**: Dosyayı direkt sürükle
- **Otomatik Algılama**: Format tanıma

### Temel İstatistikler
- **Satır/Sütun Sayısı**: Veri boyutu
- **Veri Tipleri**: Her sütun için tip
- **Eksik Veriler**: Null değer tespiti
- **Benzersiz Değerler**: Unique count

### Sayısal Analiz
- **Ortalama**: Mean hesaplama
- **Medyan**: Ortanca değer
- **Mod**: En sık tekrar eden
- **Standart Sapma**: Dağılım ölçüsü
- **Min/Max**: En küçük/büyük değer
- **Çeyrekler**: Q1, Q2, Q3

### Görselleştirme
- **Çizgi Grafik**: Zaman serisi
- **Sütun Grafik**: Kategori karşılaştırma
- **Pasta Grafik**: Oran gösterimi
- **Dağılım Grafik**: Scatter plot
- **Kutu Grafik**: Box plot

### Filtreleme
- **Sütun Seçimi**: İstenen sütunları seç
- **Değer Filtreleme**: Koşullu filtreleme
- **Sıralama**: Artan/azalan
- **Gruplama**: Kategorik gruplama

### Export
- **CSV**: Filtrelenmiş veri
- **Excel**: Formatlı çıktı
- **Grafik**: PNG olarak kaydet
- **Rapor**: PDF rapor oluştur

---

## 📁 Dosya Yöneticisi

### Şifreleme
- **AES-256**: Güçlü şifreleme
- **Şifre Koruması**: Kullanıcı tanımlı anahtar
- **Dosya/Klasör**: Her ikisini de şifrele
- **Şifreli Uzantı**: .encrypted

### Şifre Çözme
- **Otomatik Algılama**: .encrypted dosyaları
- **Şifre Girişi**: Güvenli şifre dialog
- **Orijinal Format**: Dosyayı eski haline getir
- **Toplu Çözme**: Birden fazla dosya

### Sıkıştırma
- **ZIP Format**: Standart sıkıştırma
- **Seviye Seçimi**: Store, Fastest, Optimal, Maximum
- **Toplu Sıkıştırma**: Çoklu dosya/klasör
- **Şifre Korumalı ZIP**: Opsiyonel şifreleme

### Açma (Extract)
- **ZIP, RAR, 7Z**: Çoklu format desteği
- **Hedef Klasör**: Nereye açılacak
- **Yapı Koruma**: Klasör hiyerarşisi
- **Üzerine Yazma**: Kontrollü extract

### Özellikler
- **Sürükle-Bırak**: Kolay dosya ekleme
- **İlerleme**: İşlem takibi
- **Hata Yönetimi**: Detaylı hata mesajları
- **Güvenlik**: Şifre koruma seçenekleri

---

## 🎯 Genel Özellikler

### Kullanıcı Arayüzü
- **Modern Tasarım**: WPF MahApps.Metro
- **Koyu/Açık Tema**: Tema değiştirme
- **Responsive**: Pencere boyutuna uyum
- **İkonlar**: Material Design Icons

### Performans
- **Hızlı İşlem**: Optimize edilmiş kod
- **Çoklu Thread**: Arka plan işlemleri
- **Bellek Yönetimi**: Etkili kaynak kullanımı
- **Async İşlemler**: UI donmaması

### Kullanım Kolaylığı
- **Sürükle-Bırak**: Çoğu modülde destek
- **Kopyala/Yapıştır**: Hızlı erişim
- **Kısayollar**: Klavye kısayolları
- **Tooltip**: Yardımcı ipuçları

### Güvenilirlik
- **Hata Yönetimi**: Try-catch blokları
- **Validasyon**: Girdi kontrolü
- **Log**: Detaylı işlem kayıtları
- **Geri Alma**: İptal özellikleri

---

**Son Güncelleme**: Ekim 2025  
**Versiyon**: 1.0  
**Geliştirici**: Türk Çakısı Ekibi
