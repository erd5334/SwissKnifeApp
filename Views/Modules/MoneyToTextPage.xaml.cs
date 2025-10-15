using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HBMoneyToWords;
using HBMoneyToWords.Extensions;
using HBMoneyToWords.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SwissKnifeApp.Views.Modules
{
    /// <summary>
    /// Interaction logic for MoneyToTextPage.xaml
    /// </summary>
    public partial class MoneyToTextPage : Page
    {
        public MoneyToTextPage()
        {
            InitializeComponent();
        }

        private void BtnConvert_Click(object sender, RoutedEventArgs e)
        {
            var txtAmountBox = FindName("txtAmount") as TextBox;
            var txtResultBlock = FindName("txtResult") as TextBlock;
            var cmbLanguage = FindName("cmbLanguage") as ComboBox;
            var cmbCasing = FindName("cmbCasing") as ComboBox;
            var chkNoSpaces = FindName("chkNoSpaces") as CheckBox;
            var chkFirstLetterUpper = FindName("chkFirstLetterUpper") as CheckBox;
            var txtSeparator = FindName("txtSeparator") as TextBox;
            var amountText = txtAmountBox?.Text?.Trim().Replace(',', '.') ?? "";
            if (!decimal.TryParse(amountText, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var amount))
            {
                if (txtResultBlock != null)
                    txtResultBlock.Text = "Geçerli bir tutar girin.";
                return;
            }

            string result = string.Empty;
            try
            {
                var lang = ((cmbLanguage?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Türkçe").ToLowerInvariant();
                int casing = cmbCasing?.SelectedIndex ?? 2;
                bool noSpaces = chkNoSpaces?.IsChecked == true;
                string separator = txtSeparator?.Text ?? " ";
                bool firstLetterUpper = chkFirstLetterUpper?.IsChecked == true;

                FormatOptions? format = null;
                if (!string.IsNullOrWhiteSpace(separator) || firstLetterUpper)
                {
                    format = new FormatOptions {
                        WordSeparator = !string.IsNullOrWhiteSpace(separator) ? separator : " ",
                        FirstLetterUppercase = firstLetterUpper
                    };
                }

                // Öncelik: Büyük harf, ilk harf büyük, boşluksuz
                if (lang.Contains("türk"))
                {
                    if (casing == 0) // Büyük harf
                        result = amount.ToWordsTurkish(options: FormatOptions.UpperCase);
                    else if (casing == 2) // İlk harf büyük
                        result = amount.ToWordsTurkish(options: FormatOptions.TitleCase);
                    else if (noSpaces)
                        result = amount.ToWordsTurkish(options: FormatOptions.NoSpaces);
                    else if (format != null)
                        result = amount.ToWordsTurkish(options: format);
                    else
                        result = amount.ToWordsTurkish(); // Küçük harf varsayılan
                }
                else
                {
                    if (casing == 0)
                        result = amount.ToWordsEnglish(options: FormatOptions.UpperCase);
                    else if (casing == 2)
                        result = amount.ToWordsEnglish(options: FormatOptions.TitleCase);
                    else if (noSpaces)
                        result = amount.ToWordsEnglish(options: FormatOptions.NoSpaces);
                    else if (format != null)
                        result = amount.ToWordsEnglish(options: format);
                    else
                        result = amount.ToWordsEnglish();
                }
            }
            catch (Exception ex)
            {
                result = $"Hata: {ex.Message}";
            }
            if (txtResultBlock != null)
                txtResultBlock.Text = result;
        }

        private void btnCopy_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText((FindName("txtResult") as TextBlock)?.Text ?? "");
        }
    }
}
