# Bilnex.Pos

Bilnex POS, WPF ve MVVM ile gelistirilmis modern bir perakende satis uygulamasi prototipidir. Proje; hizli urun ekleme, barkod akisi, odeme alma, fis olusturma, askiya alma ve ayar yonetimi gibi temel kasa senaryolarini kapsar.

## Ozellikler

- WPF tabanli modern POS arayuzu
- MVVM mimarisi
- Barkod girisi ve barkoddan urun esleme
- Hizli urun butonlari ile sepete ekleme
- Sepet satirinda miktar artirma, azaltma ve silme
- Receipt tabanli fis yapisi
- Nakit ve kart odeme akisi
- Para ustu hesaplama
- Fis tamamlama ve yeni fis olusturma
- Askıya alma ve askidan geri cagirma
- Tamamlanan fis gecmisi
- Project Settings altindan hizli tutar ve odeme tipi yonetimi
- Kalici ayar dosyasi destegi
- Light / Dark tema destegi

## Teknolojiler

- .NET 10
- WPF
- C#
- MVVM

## Proje Yapisi

```text
Bilnex.Pos/
  Commands/
  Models/
  Services/
  States/
  Themes/
  ViewModels/
  Views/
```

## Baslangic

1. Repoyu klonlayin.
2. `Bilnex.Pos.sln` veya `Yunus.sln` dosyasini Visual Studio ile acin.
3. `Bilnex.Pos` projesini Startup Project yapin.
4. Debug veya Release modunda calistirin.

## Build

```powershell
dotnet build .\Bilnex.Pos\Bilnex.Pos.csproj
```

## Calisan Ana Akislar

### POS

- Barkod okut veya kod gir
- Hizli urun sec
- Sepet satirlarini duzenle
- Nakit veya kart odeme al
- Fisi tamamla

### Receipt

- Her satis icin otomatik `ReceiptNo`
- Fis icerisinde urun satirlari
- Toplam tutar, odeme tutari ve para ustu
- Tamamlanan fis gecmisi

### Ayarlar

- Hizli tutarlar
- Odeme tipleri
- Varsayilan odeme tipi
- Tema
- Fis yazici adi

## Ekranlar

- Dashboard
- POS Sales
- Customers
- Inventory
- Project Settings

## Notlar

- Proje su anda veritabani kullanmaz.
- Ayarlar kullanici bazli olarak `AppData` altinda saklanir.
- Barkod okuyucu akisi keyboard emulation mantigina uygun olacak sekilde hazirlanmistir.

## Gelistirme Yol Haritasi

- Gercek barkod reader entegrasyonu
- Iade sistemi
- Fis yazdirma entegrasyonu
- Musteri ve stok modullerinin derinlestirilmesi
- Raporlama
