🚀 Türk Çakısı v2.0 - Eksiksiz Özellik Analizi
Özellikle medya, belge ve geliştirici araçları tarafında yaptığımız bu güncellemeler, programı basit bir yardımcı araçtan profesyonel bir İsviçre Çakısı'na dönüştürdü.

🎬 1. Medya ve Video İşleme Merkezi
Gelişmiş Video Araçları: Sadece dönüştürme değil; Video Stabilize Etme (deshake), GIF oluşturma, altyazı gömme ve ses-video senkron hatasını saniyeler bazında düzeltme özellikleri eklendi.
YouTube İndirici PRO: Oynatma listelerini (playlist) ve kanalları toplu indirme; yaş kısıtlamalı videolar için Chrome/Edge/Firefox cookie desteği ile oturum açılmış gibi indirme yeteneği.
Ses Laboratuvarı: Ses dosyalarından gürültü azaltma, tempo (BPM) tespiti ve müzik ritmine göre dinamik Spectrum Videosu oluşturma araçları entegre edildi.
🖼️ 2. Görsel Teknik Servis
AI Destekli Görüntü Araçları: Görselleri toplu halde WEBP/PNG formatına çevirirken, düşük çözünürlüklü fotoğrafları Lanczos algoritması (AI Upscale) ile netleştirme.
Akıllı Filigran: Metin veya logo bazlı filigranları saydamlık ve konum ayarıyla binlerce fotoğrafa tek tıkla uygulama.
Arka Plan Silici: Remove.bg API entegrasyonu ile fotoğrafların arka planını saniyeler içinde temizleme.
🎨 3. Gelişmiş Renk ve Tasarım Aracı
Renk Laboratuvarı: Tamamlayıcı, üçlü ve benzer renk kurallarına göre profesyonel palet üreticisi.
Erişilebilirlik Testi: Tasarımların WCAG (Web İçeriği Erişilebilirlik İlkeleri) uyumluluğunu kontrol eden kontrast ölçer.
Simülasyon: Renk körlüğü türlerine (Protanopia, Deuteranopia vb.) göre renklerin nasıl göründüğünü anlık simüle etme.
📄 4. Belge ve PDF Mühendisliği
PDF İsviçre Çakısı: PDF birleştirme/bölmenin ötesinde; PDF şifreleme, form doldurma, tablo verilerini ayıklama ve taranmış belgelerden OCR (Metin Tanıma) ile düzenlenebilir metin üretme.
Belge Analizi: Word (DOCX) belgelerini yüksek kaliteyle PDF'e dönüştürme ve metinlerin "okuma süresi/kelime yoğunluğu" gibi detaylı istatistiklerini raporlama.
Markdown Editor: Yazılımcılar için anlık HTML önizlemeli modern Markdown düzenleyici.
💻 5. Geliştirici (Developer) Ekosistemi
REST Client: API testleri için Postman benzeri; Header, Auth (Bearer/Basic), JSON Body desteği sunan tam teşekküllü istek istemcisi.
SQL Explorer: SQLite veritabanlarına bağlanıp tablo gezginliği yapma, SQL sorgularını formatlama ve sonuçları CSV olarak dışa aktarma.
Swiss Toolkit:
JWT Decoder: Token içeriklerini anlık çözme.
Fake Data Generator: Testler için Türkçe isim, adres, IBAN ve şirket verileri üretici.
Cron Builder: Karmaşık cron zamanlamalarını insan diline (örn: "Her Pazartesi saat 09:00'da") çeviren araç.

📝 Advanced Installer & Release Notes İçin Derlenmiş Özet
Versiyon 2.0.0 "The Ultimate Workspace"

[MULTIMEDIA] Video Stabilizasyon, YouTube Playlist İndirme, Ses Spectrum Analizi ve GIF Builder eklendi.
[IMAGE] AI Upscaling, Toplu Filigran ve API tabanlı Arka Plan Temizleme eklendi.
[DEV TOOLS] REST Client, SQL Query Browser, JWT Decoder ve Sahte Veri Üretici (Turkish Locale) eklendi.
[OFFICE] Gelişmiş PDF Editörü (Şifreleme/OCR/Tablo Ayıklama) ve Markdown Editörü eklendi.
[DESIGN] WCAG Kontrast Kontrolü, Renk Körlüğü Simülatörü ve CSS Gradient Üretici eklendi.
[UX] Global Dark Mode, Favori Modüller, Akıllı Arama ve Modüler Sidebar tasarımı uygulandı.
[COMPLIANCE] Tüm finansal motorlar 2026 mevzuatına ve tarih formatlarına güncellendi.
[THEME] Tema Kalıcılığı (Persistency) eklendi; uygulama Dark/Light seçimini settings.json ile hatırlar.
[MODERNIZATION] YouTube İndirici, Unit Converter, Video Araçları ve Metin İşlemleri modülleri Premium Tasarım Sistemi (January 2026 Refresh) ile tamamen yenilendi.

---

### 🎨 Ocak 2026 - Arayüz Modernizasyonu ve Premium Tasarım Detayları

Sabahtan bu yana yapılan çalışmalarla uygulamanın en çok kullanılan modülleri modern bir tasarım diline kavuşturuldu:

*   **Premium Tasarım Sistemi**: Tüm sayfalar MahApps.Metro tabanlı, yuvarlatılmış köşeler (Card-style), gölgeli paneller ve modern tipografi ile yenilendi.
*   **Modüler Renk Paletleri**: Her modül için işlevine uygun özel bir "Accent" rengi atandı:
    *   **YouTube İndirici**: YouTube Red (#F44336) teması.
    *   **Unit Converter**: Indigo (#3F51B5) teması.
    *   **Video Araçları**: Orange/Amber (#FF9800) teması.
    *   **Metin İşlemleri**: Purple (#9C27B0) teması.
    *   **Metin Özetleyici**: Teal (#009688) teması.
*   **Gelişmiş Dark Mode**: Sol menü (Sidebar) dahil olmak üzere tüm arayüz Dark Mode ile tam uyumlu hale getirildi. Renkler dinamik kaynaklardan (DynamicResource) beslendiği için tema geçişleri kusursuzdur.
*   **Tema Hafızası**: Kullanıcının Dark Mode tercihi artık kaydediliyor. Program kapatılıp açıldığında en son seçilen modda başlaması sağlandı (`settings.json`).
*   **Standart Kontrol Yapısı**: Tüm sayfalarda butonlar (`AccentButtonStyle`, `OutlineButtonStyle`), metin girişleri (`RoundedTextBox`) ve kombo kutular modernize edildi, tutarlı bir UX sağlandı.
