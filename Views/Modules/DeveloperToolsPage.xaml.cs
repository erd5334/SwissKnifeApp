using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Bogus;
using CronExpressionDescriptor;
using Newtonsoft.Json;
using Formatting = Newtonsoft.Json.Formatting;

namespace SwissKnifeApp.Views.Modules
{
    public partial class DeveloperToolsPage : Page
    {
        private bool _isUpdating = false;

        public DeveloperToolsPage()
        {
            InitializeComponent();
            CmbLocale.SelectedIndex = 0; // Default to TR
            BtnGetNow_Click(null, null);
        }

        #region GUID Generator
        private void BtnGenerateGuid_Click(object sender, RoutedEventArgs e)
        {
            int count = (int)NumGuidCount.Value.GetValueOrDefault(10);
            string format = "D";
            if (CmbGuidFormat.SelectedItem is ComboBoxItem item)
            {
                var content = item.Content.ToString();
                if (content.Contains("(B)")) format = "B";
                else if (content.Contains("(P)")) format = "P";
                else if (content.Contains("(N)")) format = "N";
            }

            var guids = new StringBuilder();
            for (int i = 0; i < count; i++)
            {
                guids.AppendLine(Guid.NewGuid().ToString(format));
            }
            TxtGuidList.Text = guids.ToString();
        }

        private void BtnCopyGuids_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(TxtGuidList.Text))
            {
                Clipboard.SetText(TxtGuidList.Text);
            }
        }
        #endregion

        #region Timestamp Converter
        private void BtnGetNow_Click(object? sender, RoutedEventArgs? e)
        {
            _isUpdating = true;
            var now = DateTimeOffset.Now;
            TxtUnixInput.Text = now.ToUnixTimeSeconds().ToString();
            TxtIsoInput.Text = now.ToString("yyyy-MM-dd HH:mm:ss");
            _isUpdating = false;
        }

        private void TxtUnixInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdating) return;
            _isUpdating = true;
            try
            {
                if (long.TryParse(TxtUnixInput.Text, out long unix))
                {
                    // Seconds or Milliseconds detect
                    DateTimeOffset dto;
                    if (unix > 9999999999) // Likely milliseconds
                        dto = DateTimeOffset.FromUnixTimeMilliseconds(unix);
                    else
                        dto = DateTimeOffset.FromUnixTimeSeconds(unix);

                    TxtIsoInput.Text = dto.LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss");
                }
            }
            catch { }
            _isUpdating = false;
        }

        private void TxtIsoInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdating) return;
            _isUpdating = true;
            try
            {
                if (DateTimeOffset.TryParse(TxtIsoInput.Text, out var dto))
                {
                    TxtUnixInput.Text = dto.ToUnixTimeSeconds().ToString();
                }
            }
            catch { }
            _isUpdating = false;
        }
        #endregion

        #region JWT Decoder
        private void TxtJwtInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            string token = TxtJwtInput.Text.Trim();
            if (string.IsNullOrEmpty(token)) return;

            try
            {
                var handler = new JwtSecurityTokenHandler();
                if (handler.CanReadToken(token))
                {
                    var jwt = handler.ReadJwtToken(token);
                    
                    // Simple formatting with Newtonsoft
                    TxtJwtHeader.Text = JsonConvert.SerializeObject(jwt.Header, Formatting.Indented);
                    
                    var payload = jwt.Payload;
                    TxtJwtPayload.Text = JsonConvert.SerializeObject(payload, Formatting.Indented);
                }
                else
                {
                    TxtJwtHeader.Text = "Geçersiz Token";
                    TxtJwtPayload.Text = "";
                }
            }
            catch (Exception ex)
            {
                TxtJwtHeader.Text = "Hata: " + ex.Message;
                TxtJwtPayload.Text = "";
            }
        }
        #endregion

        #region Cron Builder
        private void TxtCronExpression_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                string expression = TxtCronExpression.Text.Trim();
                if (string.IsNullOrEmpty(expression)) return;

                if (TxtCronDescription == null) return;
                var options = new Options { Locale = "tr" };
                string description = ExpressionDescriptor.GetDescription(expression, options);
                TxtCronDescription.Text = description;
            }
            catch (Exception ex)
            {
                TxtCronDescription.Text = "Hatalı İfade: " + ex.Message;
            }
        }

        private void BtnCronHelp_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Örnekler:\n" +
                "*/5 * * * *  -> Her 5 dakikada bir\n" +
                "0 0 * * *    -> Her gün gece yarısı\n" +
                "0 9-17 * * 1-5 -> Hafta içi mesai saatleri başı\n" +
                "0 0 1 * *    -> Her ayın birinde", "Cron Yardımı");
        }
        #endregion

        #region Fake Data
        private void BtnGenerateFake_Click(object sender, RoutedEventArgs e)
        {
            string locale = (CmbLocale.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "tr";
            int count = (int)NumFakeCount.Value.GetValueOrDefault(10);
            int type = CmbFakeDataType.SelectedIndex;
            int startYear = (int)NumBirthStart.Value.GetValueOrDefault(1980);
            int endYear = (int)NumBirthEnd.Value.GetValueOrDefault(2005);

            var faker = new Faker(locale);
            var results = new StringBuilder();

            bool isTr = locale == "tr";

            for (int i = 0; i < count; i++)
            {
                switch (type)
                {
                    case 0: // Person (Consistent name-email, Gender, Birthdate)
                        var gender = faker.PickRandom<Bogus.DataSets.Name.Gender>();
                        var firstName = faker.Name.FirstName(gender);
                        var lastName = faker.Name.LastName();
                        var fullName = $"{firstName} {lastName}";
                        // Create email based on generated name for consistency
                        var email = faker.Internet.Email(firstName, lastName).ToLower();
                        var birthDate = faker.Date.Between(new DateTime(startYear, 1, 1), new DateTime(endYear, 12, 31));
                        
                        string genderStr = isTr ? (gender == Bogus.DataSets.Name.Gender.Male ? "Erkek" : "Kadın") : gender.ToString();
                        
                        string phoneNumber = faker.Phone.PhoneNumber();
                        if (isTr)
                        {
                            var trPrefixes = new[] { 
                                "530", "531", "532", "533", "534", "535", "536", "537", "538", "539", // Turkcell
                                "540", "541", "542", "543", "544", "545", "546", "547", "548", "549", // Vodafone
                                "501", "505", "506", "507", "551", "552", "553", "554", "555", "559", // Türk Telekom
                                "212", "216", "312", "232", "224", "242" // Şehirler (İst, Ank, İzm, Bur, Ant)
                            };
                            phoneNumber = $"+90 ({faker.PickRandom(trPrefixes)}) {faker.Random.Number(100, 999)} {faker.Random.Number(10, 99)} {faker.Random.Number(10, 99)}";
                        }

                        results.AppendLine($"{fullName} | {genderStr} | {birthDate:dd.MM.yyyy} | {phoneNumber} | {email}");
                        break;

                    case 1: // Address (Force locale consistency)
                        var address = isTr 
                            ? $"{faker.Address.StreetAddress()}, {faker.Address.ZipCode()} {faker.Address.City()} / TÜRKİYE"
                            : $"{faker.Address.StreetAddress()}, {faker.Address.ZipCode()} {faker.Address.City()} / {faker.Address.Country()}";
                        results.AppendLine(address);
                        break;

                    case 2: // Company
                        results.AppendLine($"{faker.Company.CompanyName()} - {faker.Name.JobTitle()}");
                        break;

                    case 3: // Internet
                        results.AppendLine($"{faker.Internet.Url()} | {faker.Internet.UserName()} | {faker.Internet.UserAgent()}");
                        break;

                    case 4: // Finance
                        results.AppendLine($"{faker.Finance.Iban()} | {faker.Finance.CreditCardNumber()} | {faker.Finance.Amount(10, 5000)} {faker.Finance.Currency().Symbol}");
                        break;
                }
            }
            TxtFakeDataOutput.Text = results.ToString();
        }

        private void BtnCopyFake_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(TxtFakeDataOutput.Text))
            {
                Clipboard.SetText(TxtFakeDataOutput.Text);
            }
        }
        #endregion
    }
}
